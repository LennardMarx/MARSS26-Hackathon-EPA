#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

using System.Collections.Generic;
using UnityEngine;
using static AimPosition.AimPositionAPI;

namespace AimPosition
{
    /// <summary>
    /// Visualizes raw AimPosition marker detections as spheres in the Unity scene.
    ///
    /// Spheres are pooled and reused each frame.  Color indicates tracking quality:
    ///   White  – normal marker
    ///   Yellow – close to field-of-view edge (eWarn_Common)
    ///   Red    – critically close to edge (eWarn_Critical)
    ///   Orange – phantom-point warning (possible ghost reflection)
    /// </summary>
    public class MarkerVisualizer : MonoBehaviour
    {
        [Header("Appearance")]
        public float markerRadius = 0.005f;   // meters
        public Color colorNormal   = Color.white;
        public Color colorEdge     = Color.yellow;
        public Color colorCritical = Color.red;
        public Color colorPhantom  = new Color(1f, 0.5f, 0f);  // orange

        [Header("Options")]
        [Tooltip("Hide markers with phantom-point warnings")]
        public bool hidePhantoms = false;
        [Tooltip("Parent transform for all sphere GameObjects (null = this transform)")]
        public Transform markerContainer;

        // ── Sphere pool ───────────────────────────────────────────────────────

        private readonly List<GameObject> _pool = new List<GameObject>();
        private Material _mat;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        void Awake()
        {
            _mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard"));
            _mat.enableInstancing = true;

            if (markerContainer == null) markerContainer = transform;
        }

        void OnEnable()
        {
            if (AimPositionManager.Instance != null)
                AimPositionManager.Instance.OnFrameReceived += OnFrameReceived;
        }

        void OnDisable()
        {
            if (AimPositionManager.Instance != null)
                AimPositionManager.Instance.OnFrameReceived -= OnFrameReceived;

            SetActiveCount(0);
        }

        void OnDestroy()
        {
            foreach (var go in _pool)
                if (go != null) Destroy(go);
            _pool.Clear();
            if (_mat != null) Destroy(_mat);
        }

        // ── Frame callback (main thread) ──────────────────────────────────────

        private void OnFrameReceived(T_MarkerInfo markers, T_AimPosStatusInfo _)
        {
            int n = markers.MarkerNumber;
            SetActiveCount(n);

            for (int i = 0; i < n; i++)
            {
                double mx = markers.MarkerCoordinate[i * 3 + 0];
                double my = markers.MarkerCoordinate[i * 3 + 1];
                double mz = markers.MarkerCoordinate[i * 3 + 2];

                bool isPhantom = markers.PhantomMarkerWarning != null
                                 && markers.PhantomMarkerWarning[i] > 0;

                if (hidePhantoms && isPhantom)
                {
                    _pool[i].SetActive(false);
                    continue;
                }

                _pool[i].SetActive(true);
                _pool[i].transform.position = TrackedTool.RhToLh(mx, my, mz);

                Color col;
                if (isPhantom)
                    col = colorPhantom;
                else if (markers.MarkWarn != null)
                    col = markers.MarkWarn[i] switch
                    {
                        E_MarkWarnType.eWarn_Critical => colorCritical,
                        E_MarkWarnType.eWarn_Common   => colorEdge,
                        _                             => colorNormal,
                    };
                else
                    col = colorNormal;

                SetSphereColor(_pool[i], col);
            }
        }

        // ── Pool management ───────────────────────────────────────────────────

        private void SetActiveCount(int count)
        {
            // Grow pool if needed
            while (_pool.Count < count)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Marker_{_pool.Count:D3}";
                go.transform.SetParent(markerContainer, false);
                go.transform.localScale = Vector3.one * markerRadius * 2f;

                // Remove collider — purely visual
                Destroy(go.GetComponent<Collider>());

                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = _mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                go.SetActive(false);
                _pool.Add(go);
            }

            // Deactivate surplus spheres
            for (int i = count; i < _pool.Count; i++)
                _pool[i].SetActive(false);
        }

        private static void SetSphereColor(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            // Use MaterialPropertyBlock to avoid creating per-instance materials
            var block = new MaterialPropertyBlock();
            mr.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);   // URP
            block.SetColor("_Color",     color);   // Built-in fallback
            mr.SetPropertyBlock(block);
        }
    }
}

#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
