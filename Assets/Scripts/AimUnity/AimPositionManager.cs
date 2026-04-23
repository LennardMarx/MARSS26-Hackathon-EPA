#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using static AimPosition.AimPositionAPI;

namespace AimPosition
{
    /// <summary>
    /// Singleton manager for the AimPosition optical tracking system over Ethernet.
    ///
    /// Usage:
    ///   1. Add this component to a persistent GameObject (e.g. "TrackingManager").
    ///   2. Set Device IP in the Inspector (default: 192.168.31.10).
    ///   3. Place .aimtool files in StreamingAssets/AimTools/ (or override Tool Files Directory).
    ///   4. Add TrackedTool components to tracked GameObjects and set their Tool ID.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class AimPositionManager : MonoBehaviour
    {
        public static AimPositionManager Instance { get; private set; }

        [Header("Connection")]
        [Tooltip("IP address of the AimPosition device (default: 192.168.31.10)")]
        public string deviceIP = "192.168.31.10";

        [Header("Tool Files")]
        [Tooltip("Directory containing .aimtool files. Leave empty to use StreamingAssets/AimTools/")]
        public string toolFilesDirectory = "";

        [Header("Acquisition")]
        [Tooltip("Search tolerance in mm for tool point matching")]
        public float toolSearchOffset = 1.5f;
        [Tooltip("Enable optimized point-match algorithm")]
        public bool enableMatchOptimize = true;

        // ── Public state ──────────────────────────────────────────────────────

        public bool IsConnected { get; private set; }
        public T_AIMPOS_DATAPARA DeviceInfo { get; private set; }
        public T_AimPosStatusInfo LatestStatus { get; private set; }
        public int FrameCount { get; private set; }

        /// <summary>Fired on the main thread each time a new tracking frame arrives.</summary>
        public event Action<T_MarkerInfo, T_AimPosStatusInfo> OnFrameReceived;

        /// <summary>Fired on the main thread when connection state changes.</summary>
        public event Action<bool> OnConnectionStateChanged;

        // ── Internals ─────────────────────────────────────────────────────────

        private IntPtr _handle = IntPtr.Zero;
        private Thread _acquisitionThread;
        private volatile bool _running;

        // Tools registered by TrackedTool components: toolId → callback (main thread)
        private readonly Dictionary<string, Action<T_AimToolDataResultSingle>> _registeredTools
            = new Dictionary<string, Action<T_AimToolDataResultSingle>>();
        private readonly object _toolsLock = new object();

        private struct Frame
        {
            public T_MarkerInfo   markers;
            public T_AimPosStatusInfo status;
            public Dictionary<string, T_AimToolDataResultSingle> poses;
        }
        private readonly ConcurrentQueue<Frame> _frameQueue = new ConcurrentQueue<Frame>();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Connect();
        }

        void Update()
        {
            while (_frameQueue.TryDequeue(out var frame))
            {
                FrameCount++;
                LatestStatus = frame.status;
                OnFrameReceived?.Invoke(frame.markers, frame.status);

                foreach (var kv in frame.poses)
                {
                    Action<T_AimToolDataResultSingle> cb;
                    lock (_toolsLock)
                        _registeredTools.TryGetValue(kv.Key, out cb);
                    cb?.Invoke(kv.Value);
                }
            }
        }

        void OnDestroy()
        {
            Disconnect();
            if (Instance == this) Instance = null;
        }

        // ── Connection ────────────────────────────────────────────────────────

        public void Connect()
        {
            if (IsConnected) return;

            var ret = Aim_API_Initial(out _handle);
            if (ret != E_ReturnValue.AIMOOE_OK)
            {
                Debug.LogError($"[AimPosition] Aim_API_Initial failed: {ret}");
                return;
            }

            if (!TryParseIP(deviceIP, out var a, out var b, out var c, out var d))
            {
                Debug.LogError($"[AimPosition] Invalid IP: {deviceIP}");
                return;
            }
            Aim_SetEthernetConnectIP(_handle, a, b, c, d);

            ret = Aim_ConnectDevice(_handle, E_Interface.I_ETHERNET, out var devInfo);
            if (ret != E_ReturnValue.AIMOOE_OK)
            {
                Debug.LogError($"[AimPosition] ConnectDevice failed: {ret}. Check IP and cable.");
                return;
            }

            DeviceInfo = devInfo;
            IsConnected = true;
            Debug.Log($"[AimPosition] Connected to {devInfo.devtype} ({devInfo.hardwareinfo}) at {deviceIP}");

            string toolDir = string.IsNullOrEmpty(toolFilesDirectory)
                ? Path.Combine(Application.streamingAssetsPath, "AimTools")
                : toolFilesDirectory;
            Aim_SetToolInfoFilePath(_handle, toolDir);
            Aim_SetToolFindOffset(_handle, toolSearchOffset);
            Aim_SetToolFindPointMatchOptimizeEnable(_handle, enableMatchOptimize);

            Aim_SetAcquireData(_handle, E_Interface.I_ETHERNET, E_DataType.DT_INFO);

            _running = true;
            _acquisitionThread = new Thread(AcquisitionLoop)
            {
                IsBackground = true,
                Name = "AimPosition-Acquisition",
            };
            _acquisitionThread.Start();

            OnConnectionStateChanged?.Invoke(true);
        }

        public void Disconnect()
        {
            _running = false;
            _acquisitionThread?.Join(2000);
            _acquisitionThread = null;

            if (_handle != IntPtr.Zero)
            {
                Aim_SetAcquireData(_handle, E_Interface.I_ETHERNET, E_DataType.DT_NONE);
                Aim_API_Close(out _handle);
                IsConnected = false;
                Debug.Log("[AimPosition] Disconnected.");
                OnConnectionStateChanged?.Invoke(false);
            }
        }

        // ── Tool registration (called by TrackedTool) ─────────────────────────

        public void RegisterTool(string toolId, Action<T_AimToolDataResultSingle> callback)
        {
            if (string.IsNullOrEmpty(toolId)) return;
            lock (_toolsLock)
                _registeredTools[toolId] = callback;
        }

        public void UnregisterTool(string toolId)
        {
            if (string.IsNullOrEmpty(toolId)) return;
            lock (_toolsLock)
                _registeredTools.Remove(toolId);
        }

        // ── System commands ───────────────────────────────────────────────────

        public void SendCommand(E_SystemCommand command)
        {
            if (!IsConnected) return;
            Aim_SetSystemCommand(_handle, E_Interface.I_ETHERNET, command);
        }

        public void SetExposureTime(int expTime)
        {
            if (!IsConnected) return;
            Aim_SetDualExpTime(_handle, E_Interface.I_ETHERNET, expTime);
        }

        // ── Background acquisition thread ────────────────────────────────────

        private void AcquisitionLoop()
        {
            while (_running)
            {
                var ret = Aim_GetMarkerAndStatusFromHardware(
                    _handle, E_Interface.I_ETHERNET, out var markers, out var status);

                if (ret == E_ReturnValue.AIMOOE_NOT_CONNECT)
                {
                    Debug.LogError("[AimPosition] Device lost. Stopping acquisition.");
                    _running = false;
                    IsConnected = false;
                    continue;
                }

                if (ret != E_ReturnValue.AIMOOE_OK)
                    continue;

                // Run tool finding for all registered tools on this thread
                // (all API calls stay on one thread — no locking needed for the handle).
                string[] toolIds;
                lock (_toolsLock)
                {
                    toolIds = new string[_registeredTools.Count];
                    _registeredTools.Keys.CopyTo(toolIds, 0);
                }

                var poses = new Dictionary<string, T_AimToolDataResultSingle>(toolIds.Length);
                foreach (var id in toolIds)
                {
                    Aim_FindSingleToolInfo(_handle, markers, id, out var pose);
                    poses[id] = pose;
                }

                _frameQueue.Enqueue(new Frame { markers = markers, status = status, poses = poses });
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool TryParseIP(string ip, out byte a, out byte b, out byte c, out byte d)
        {
            a = b = c = d = 0;
            var parts = ip.Split('.');
            return parts.Length == 4
                && byte.TryParse(parts[0], out a)
                && byte.TryParse(parts[1], out b)
                && byte.TryParse(parts[2], out c)
                && byte.TryParse(parts[3], out d);
        }
    }
}

#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
