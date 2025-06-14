using UnityEngine;
using System.Collections.Generic;
using PropagationSystem;
using PropagationSystem.Editor;

#if UNITY_EDITOR

namespace PropagationSystem.Editor
{
    public class BrushRandomWeighted2D
    {
        #region Fields
        private List<WeightedPixel> _weightedPixels = new List<WeightedPixel>();
        private Texture2D _originalTexture;
        private Vector3 _hitPoint;
        private Vector3 _hitNormal;
        private float _brushSize;
        private float _brushDensity;
        private int _MeshIndex;
        private int _InstanceCount;
        private float _totalWeight;
        #endregion

        #region Public Methods
        public StrokeData ApplyBrush(
            BrushDataSO brush,
            Vector3 hitPoint,
            Vector3 hitNormal,
            float brushSize,
            float density,
            int count,
            int meshIndex,
            Camera camera)
        {
            _originalTexture = brush.maskTexture;
            _hitPoint = hitPoint;
            _hitNormal = hitNormal.normalized;
            _brushSize = brushSize;
            _brushDensity = density;
            _MeshIndex = meshIndex;
            _InstanceCount = count;

            Texture2D renderTexture = CreateReadableTexture(_originalTexture);
            SampleWeightedPixels(renderTexture);

            Debug.Log("Toplam aðýrlýklý piksel sayýsý: " + _weightedPixels.Count);

            Ray[] rays = GenerateWeightedRays(camera, count);

            return BuildStrokeData(rays, PropagationBrushWindow.BrushMode.Paint);
        }
        #endregion

        #region Helper Methods
        private Texture2D CreateReadableTexture(Texture2D source)
        {
            Texture2D readable = new Texture2D(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                false,
                true
            );

            readable.SetPixels(source.GetPixels());
            readable.Apply();

            return readable;
        }

        private void SampleWeightedPixels(Texture2D texture)
        {
            _weightedPixels.Clear();
            _totalWeight = 0;

            Color[] pixels = texture.GetPixels();
            int width = texture.width;
            int height = texture.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color col = pixels[y * width + x];
                    float grayscale = col.grayscale;

                    if (grayscale > 0.01f && col.a > 0.01f)
                    {
                        float weight = grayscale; // beyazlar 1.0, griler daha düþük
                        _weightedPixels.Add(new WeightedPixel
                        {
                            position = new Vector2Int(x, y),
                            weight = weight
                        });

                        _totalWeight += weight;
                    }
                }
            }
        }

        private Ray[] GenerateWeightedRays(Camera camera, int rayCount)
        {
            int count = Mathf.Min(rayCount, _weightedPixels.Count);
            List<Ray> rays = new List<Ray>(count);

            Vector3 tangent = Vector3.Cross(_hitNormal, camera.transform.right).normalized;
            if (tangent == Vector3.zero)
                tangent = camera.transform.up;

            Vector3 bitangent = Vector3.Cross(_hitNormal, tangent).normalized;

            for (int i = 0; i < count; i++)
            {
                WeightedPixel sample = SampleFromWeightedPixels();

                Vector2 uv = new Vector2(
                    (float)sample.position.x / _originalTexture.width,
                    (float)sample.position.y / _originalTexture.height
                );

                Vector3 worldPos = _hitPoint
                    + -(uv.x - 0.5f) * _brushSize * 2 * bitangent
                    - (uv.y - 0.5f) * _brushSize * 2 * tangent;

                Vector3 origin = worldPos + _hitNormal * 0.1f;
                Vector3 direction = -_hitNormal;
                rays.Add(new Ray(origin, direction));
            }

            return rays.ToArray();
        }

        private WeightedPixel SampleFromWeightedPixels()
        {
            float randomValue = Random.value * _totalWeight;
            float cumulative = 0f;

            foreach (var pixel in _weightedPixels)
            {
                cumulative += pixel.weight;
                if (randomValue <= cumulative)
                    return pixel;
            }

            return _weightedPixels[_weightedPixels.Count - 1]; // fallback
        }

        private StrokeData BuildStrokeData(Ray[] rays, PropagationBrushWindow.BrushMode brushMode)
        {
            int length = rays.Length;
            SavedPositions[] savedPositionsArray = new SavedPositions[length];

            for (int i = 0; i < length; i++)
            {
                Physics.Raycast(rays[i], out RaycastHit hitInfo);

                Vector3 normal = hitInfo.normal;
                Vector3 tangent = Vector3.Cross(normal, Vector3.up);

                if (tangent.sqrMagnitude < 0.001f)
                    tangent = Vector3.Cross(normal, Vector3.right);

                if (tangent.sqrMagnitude < 0.001f)
                    tangent = Vector3.forward;

                Vector3 forward = Vector3.Cross(tangent, normal);

                Quaternion rotation = Quaternion.identity;
                if (forward.sqrMagnitude > 0.001f)
                    rotation = Quaternion.LookRotation(forward, normal);

                savedPositionsArray[i] = new SavedPositions
                {
                    position = hitInfo.point,
                    rotation = rotation,
                    scale = Vector3.one
                };
            }

            return new StrokeData
            {
                savedPositions = savedPositionsArray,
                brushMode = brushMode,
                meshIndex = _MeshIndex
            };
        }

        #endregion

        #region Internal Struct
        private struct WeightedPixel
        {
            public Vector2Int position;
            public float weight;
        }
        #endregion
    }
}

#endif
