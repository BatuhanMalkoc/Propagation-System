using UnityEngine;
using System.Collections.Generic;
using PropagationSystem;
using PropagationSystem.Editor;

#if UNITY_EDITOR
namespace PropagationSystem.Editor
{
    /// <summary>
    /// Grid-based brush sampler using raycasts:
    /// Grid hücrelerini mask UV üzerinden belirler, 
    /// her biri için raycast yapar, gerçek yüzeye spawn pozisyonu belirler.
    /// </summary>
    public class BrushGrid2D
    {
        #region Fields
        private Texture2D _maskTexture;
        private Texture2D _readableMask;
        private Vector3 _hitPoint;
        private Vector3 _hitNormal;
        private float _brushSize;
        private int _instanceCount;
        private int _meshIndex;
        #endregion

        #region Public API
        public StrokeData ApplyBrush(
            BrushDataSO brushData,
            Vector3 hitPoint,
            Vector3 hitNormal,
            float brushSize,
            int instanceCount,
            int meshIndex,
            Camera camera)
        {
            // 1) Initialize
            _maskTexture = brushData.maskTexture;
            _readableMask = MakeReadable(_maskTexture);
            _hitPoint = hitPoint;
            _hitNormal = hitNormal.normalized;
            _brushSize = brushSize;
            _instanceCount = instanceCount;
            _meshIndex = meshIndex;

            // 2) Compute grid size
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(_instanceCount));
            gridSize = Mathf.Max(gridSize, 1);

            // 3) Compute tangent/bitangent on surface
            Vector3 tangent = Vector3.Cross(_hitNormal, camera.transform.right).normalized;
            if (tangent == Vector3.zero) tangent = camera.transform.up;
            Vector3 bitangent = Vector3.Cross(_hitNormal, tangent).normalized;

            // 4) Cell size & center offset
            float cellSize = (_brushSize * 2f) / gridSize;
            float centerOffset = (gridSize - 1) / 2f;

            // 5) Generate rays
            var rays = new List<Ray>(_instanceCount);
            int placed = 0;
            for (int y = 0; y < gridSize && placed < _instanceCount; y++)
            {
                for (int x = 0; x < gridSize && placed < _instanceCount; x++)
                {
                    // UV coords
                    float offsetX = (x - centerOffset) * cellSize;
                    float offsetY = (y - centerOffset) * cellSize;
                    float u = (offsetX / (_brushSize * 2f)) + 0.5f;
                    float v = (offsetY / (_brushSize * 2f)) + 0.5f;

                    if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                    Color m = _readableMask.GetPixelBilinear(u, v);
                    if (m.grayscale <= 0.01f || m.a <= 0.01f) continue;

                    // World pos above surface
                    Vector3 worldPos = _hitPoint + tangent * offsetX + bitangent * offsetY;
                    Vector3 origin = worldPos + _hitNormal * 0.5f;       // biraz yukarý
                    Vector3 direction = -_hitNormal;                       // aþaðý

                    rays.Add(new Ray(origin, direction));
                    placed++;
                }
            }

            // 6) Raycast ve StrokeData oluþtur
            return BuildStrokeData(rays.ToArray());
        }
        #endregion

        #region Helpers
        private StrokeData BuildStrokeData(Ray[] rays)
        {
            var saved = new List<SavedPositions>(rays.Length);
            foreach (var ray in rays)
            {
                if (Physics.Raycast(ray, out var hit))
                {
                    // Rotation aligned
                    Vector3 normal = hit.normal;
                    Vector3 tangent = Vector3.Cross(normal, Vector3.up);
                    if (tangent.sqrMagnitude < 0.001f)
                        tangent = Vector3.Cross(normal, Vector3.right);
                    Vector3 forward = Vector3.Cross(tangent.normalized, normal);
                    Quaternion rot = forward.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(forward, normal)
                        : Quaternion.identity;

                    saved.Add(new SavedPositions
                    {
                        position = hit.point,
                        rotation = rot,
                        scale = Vector3.one
                    });
                }
            }

            return new StrokeData
            {
                savedPositions = saved.ToArray(),
                brushMode = PropagationBrushWindow.BrushMode.Paint,
                meshIndex = _meshIndex
            };
        }

        private Texture2D MakeReadable(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(
                source.width, source.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            var readable = new Texture2D(source.width, source.height);
            readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return readable;
        }
        #endregion
    }
}
#endif
