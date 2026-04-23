#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

using UnityEngine;
using UnityEngine.Events;
using static AimPosition.AimPositionAPI;

namespace AimPosition
{
    /// <summary>
    /// Drives a Unity Transform from an AimPosition tool pose.
    ///
    /// Attach this to any GameObject that should follow a tracked tool.
    /// The GameObject's origin is placed at the tool origin (Tto).
    /// An optional TipMarker child is placed at the tool tip (tooltip).
    ///
    /// Coordinate conversion (AimPosition RH mm → Unity LH m):
    ///   position  = (x, y, -z) / 1000
    ///   rotation  = Quaternion(-qx, -qy, qz, qw)   [Qoxyz = {qx, qy, qz, qw}]
    /// </summary>
    public class TrackedTool : MonoBehaviour
    {
        [Header("Tool")]
        [Tooltip("Tool ID string matching the .aimtool filename (without extension)")]
        public string toolId = "";

        [Tooltip("Minimum marker points that must match (0 = use SDK default)")]
        public int minimumMatchPoints = 0;

        [Header("Quality gate")]
        [Tooltip("Hide this object when RMS error exceeds this value (mm). 0 = never hide.")]
        public float maxRmsError = 5f;

        [Header("References")]
        [Tooltip("Optional child GameObject to be placed at the tool tip in world space")]
        public GameObject tipMarker;

        [Header("Events")]
        public UnityEvent onTrackingAcquired;
        public UnityEvent onTrackingLost;

        // ── Public state ──────────────────────────────────────────────────────

        public bool IsTracked { get; private set; }
        public T_AimToolDataResultSingle LastPose { get; private set; }

        /// <summary>Tool tip position in Unity world space (meters).</summary>
        public Vector3 TipPosition { get; private set; }

        /// <summary>Mean point-distance error from last valid pose (mm).</summary>
        public float LastMeanError { get; private set; }

        /// <summary>RMS point-distance error from last valid pose (mm).</summary>
        public float LastRmsError { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        void OnEnable()
        {
            if (AimPositionManager.Instance != null)
                AimPositionManager.Instance.RegisterTool(toolId, OnPoseReceived);
        }

        void OnDisable()
        {
            if (AimPositionManager.Instance != null)
                AimPositionManager.Instance.UnregisterTool(toolId);

            if (IsTracked)
            {
                IsTracked = false;
                onTrackingLost?.Invoke();
            }
        }

        // ── Pose callback (main thread) ───────────────────────────────────────

        private void OnPoseReceived(T_AimToolDataResultSingle pose)
        {
            bool wasTracked = IsTracked;

            if (!pose.validflag || (maxRmsError > 0f && pose.Rms > maxRmsError))
            {
                IsTracked = false;
                if (wasTracked) onTrackingLost?.Invoke();
                return;
            }

            IsTracked = true;
            LastPose = pose;
            LastMeanError = pose.MeanError;
            LastRmsError  = pose.Rms;

            ApplyPose(pose);

            if (!wasTracked) onTrackingAcquired?.Invoke();
        }

        // ── Coordinate conversion ─────────────────────────────────────────────

        private void ApplyPose(T_AimToolDataResultSingle pose)
        {
            // Tool origin in system coords → Unity world position
            float[] t = pose.Tto;
            transform.position = RhToLh(t[0], t[1], t[2]);

            // Quaternion [Qx, Qy, Qz, Qo(=Qw)] — negate x,y for RH→LH
            float[] q = pose.Qoxyz;
            transform.rotation = new Quaternion(-q[0], -q[1], q[2], q[3]);

            // Tool tip in world space
            float[] tip = pose.tooltip;
            TipPosition = RhToLh(tip[0], tip[1], tip[2]);

            if (tipMarker != null)
                tipMarker.transform.position = TipPosition;
        }

        /// <summary>
        /// Converts an AimPosition right-handed mm coordinate to a Unity left-handed meter coordinate.
        /// Flips Z and scales by 1/1000.
        /// </summary>
        public static Vector3 RhToLh(float x, float y, float z)
            => new Vector3(x, y, -z) / 1000f;

        public static Vector3 RhToLh(double x, double y, double z)
            => new Vector3((float)x, (float)y, -(float)z) / 1000f;
    }
}

#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
