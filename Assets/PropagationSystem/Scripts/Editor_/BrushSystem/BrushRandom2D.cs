using UnityEngine;
using System.Collections.Generic;
using PropagationSystem;
using PropagationSystem.Editor;

public class BrushRandom2D : IBrush
{
    private List<PixelSample> validPixels = new List<PixelSample>();
    private Texture2D originalTexture;
    private Vector3 lastHitPoint;
    private Vector3 lastHitNormal;
    private float lastBrushSize;

    public BrushPaintData[] ApplyBrush(BrushDataSO brush, Vector3 hitPoint, Vector3 hitNormal, float brushSize, float density, int count, Camera camera)
    {
        originalTexture = brush.maskTexture;

        // Create a readable RGBA32 copy
        Texture2D renderTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false, true);
        renderTexture.SetPixels(originalTexture.GetPixels());
        renderTexture.Apply();

        validPixels.Clear();
        Color[] pixels = renderTexture.GetPixels();

        for (int y = 0; y < renderTexture.height; y++)
        {
            for (int x = 0; x < renderTexture.width; x++)
            {
                Color col = pixels[y * renderTexture.width + x];
                if (col.grayscale > 0.01f && col.a > 0.01f)
                {
                    validPixels.Add(new PixelSample
                    {
                        position = new Vector2Int(x, y),
                        color = col
                    });
                }
            }
        }

        Debug.Log("Geçerli pixel sayýsý: " + validPixels.Count);

        lastHitPoint = hitPoint;
        lastHitNormal = hitNormal.normalized;
        lastBrushSize = brushSize;

        Ray[] rays = GetRay(hitPoint, hitNormal, camera, count);

        BrushPaintData[] brushPaintData = new BrushPaintData[rays.Length];

        for (int i = 0; i < rays.Length; i++)
        {
            Physics.Raycast(rays[i], out RaycastHit hitInfo);
            brushPaintData[i] = new BrushPaintData
            {
                position = hitInfo.point,
                rotation = IsValidQuaternion(Quaternion.LookRotation(Vector3.Cross(hitInfo.normal,Vector3.right))) ? Quaternion.LookRotation(Vector3.Cross(hitInfo.normal, Vector3.right)) : Quaternion.identity,
                scale = Vector3.one
            };
        }

        return brushPaintData;
    }

    static bool IsValidQuaternion(Quaternion q)
    {
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return magnitude > 0.0001f && !float.IsNaN(magnitude) && !float.IsInfinity(magnitude);
    }

    public Ray[] GetRay(Vector3 rayPosition, Vector3 rayDirection, Camera camera, int rayCount)
    {
        int count = Mathf.Min(rayCount, validPixels.Count);
        List<Ray> rays = new List<Ray>();

        // Compute plane axes based on camera-relative brush plane
        Vector3 tangent = Vector3.Cross(lastHitNormal, camera.transform.right).normalized;
        if (tangent == Vector3.zero) tangent = camera.transform.up;
        Vector3 bitangent = Vector3.Cross(lastHitNormal, tangent).normalized;

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, validPixels.Count);
            PixelSample sample = validPixels[index];

            Vector2 uv = new Vector2(
                (float)sample.position.x / originalTexture.width,
                (float)sample.position.y / originalTexture.height
            );

            // Rotate UV axes by 90 degrees around brush normal: swap tangent and bitangent
            Vector3 worldPos = lastHitPoint
                + -(uv.x - 0.5f) * lastBrushSize * bitangent
                - (uv.y - 0.5f) * lastBrushSize * tangent;

            rays.Add(new Ray(worldPos + lastHitNormal * 0.1f, -lastHitNormal));
        }

        return rays.ToArray();
    }

    // Unused overload
    public Ray[] GetRay(Vector3 rayPosition, Vector3 rayDirection, int rayCount)
    {
        throw new System.NotImplementedException();
    }
}

public struct PixelSample
{
    public Vector2Int position;
    public Color color;
}