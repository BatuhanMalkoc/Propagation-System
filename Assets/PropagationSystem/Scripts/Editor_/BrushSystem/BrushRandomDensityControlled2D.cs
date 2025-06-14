using UnityEngine;
using System.Collections.Generic;
using PropagationSystem;
using PropagationSystem.Editor;

#if UNITY_EDITOR

public class BrushRandomDensityControlled2D
{
    private List<WeightedPixel> _weightedPixels = new List<WeightedPixel>();
    private Texture2D _originalTexture;
    private Vector3 _hitPoint;
    private Vector3 _hitNormal;
    private float _brushSize;
    private float _densityMultiplier;
    private int _meshIndex;
    private int _instanceCount;

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
        _densityMultiplier = density;
        _meshIndex = meshIndex;
        _instanceCount = count;

        Texture2D readable = CreateReadableTexture(_originalTexture);
        SampleWeightedPixels(readable);

        Ray[] rays = GenerateWeightedRays(camera, count);

        return BuildStrokeData(rays, PropagationBrushWindow.BrushMode.Paint);
    }

    private Texture2D CreateReadableTexture(Texture2D source)
    {
        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
        readable.SetPixels(source.GetPixels());
        readable.Apply();
        return readable;
    }

    private void SampleWeightedPixels(Texture2D texture)
    {
        _weightedPixels.Clear();
        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;

        float totalWeight = 0f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color col = pixels[y * width + x];
                float gray = col.grayscale;
                if (gray > 0.01f && col.a > 0.01f)
                {
                    float weight = gray * _densityMultiplier;
                    totalWeight += weight;

                    _weightedPixels.Add(new WeightedPixel
                    {
                        position = new Vector2Int(x, y),
                        weight = weight
                    });
                }
            }
        }

        // Normalize weights
        for (int i = 0; i < _weightedPixels.Count; i++)
        {
            var p = _weightedPixels[i];
            p.normalizedWeight = p.weight / totalWeight;
            _weightedPixels[i] = p;
        }
    }

    private Ray[] GenerateWeightedRays(Camera camera, int rayCount)
    {
        Ray[] rays = new Ray[rayCount];
        Vector3 tangent = Vector3.Cross(_hitNormal, camera.transform.right).normalized;
        if (tangent == Vector3.zero)
            tangent = camera.transform.up;

        Vector3 bitangent = Vector3.Cross(_hitNormal, tangent).normalized;

        for (int i = 0; i < rayCount; i++)
        {
            WeightedPixel pixel = GetWeightedRandomPixel();
            Vector2 uv = new Vector2(
                (float)pixel.position.x / _originalTexture.width,
                (float)pixel.position.y / _originalTexture.height
            );

            Vector3 worldPos = _hitPoint
                - (uv.x - 0.5f) * _brushSize * 2 * bitangent
                - (uv.y - 0.5f) * _brushSize * 2 * tangent;

            Vector3 origin = worldPos + _hitNormal * 0.1f;
            rays[i] = new Ray(origin, -_hitNormal);
        }

        return rays;
    }

    private WeightedPixel GetWeightedRandomPixel()
    {
        float r = Random.value;
        float cumulative = 0f;

        for (int i = 0; i < _weightedPixels.Count; i++)
        {
            cumulative += _weightedPixels[i].normalizedWeight;
            if (r <= cumulative)
                return _weightedPixels[i];
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
            if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.Cross(normal, Vector3.right);
            if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.forward;

            Vector3 forward = Vector3.Cross(tangent, normal);
            Quaternion rotation = forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward, normal) : Quaternion.identity;

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
            meshIndex = _meshIndex
        };
    }

    private struct WeightedPixel
    {
        public Vector2Int position;
        public float weight;
        public float normalizedWeight;
    }
}
#endif