using UnityEngine;
using System.Collections.Generic;
using PropagationSystem;
using PropagationSystem.Editor;

namespace PropagationSystem.Editor
{
    public class BrushRandom2D 
    {
        #region Fields
        private List<PixelSample> validPixels = new List<PixelSample>();
        private Texture2D originalTexture;
        private Vector3 lastHitPoint;
        private Vector3 lastHitNormal;
        private float lastBrushSize;
        #endregion

        #region Public Methods
        public BrushPaintData[] ApplyBrush(
            BrushDataSO brush,
            Vector3 hitPoint,
            Vector3 hitNormal,
            float brushSize,
            float density,
            int count,
            Camera camera)
        {
            // Initialize fields
            originalTexture = brush.maskTexture;
            lastHitPoint = hitPoint;
            lastHitNormal = hitNormal.normalized;
            lastBrushSize = brushSize;

            // Process texture and sample pixels
            Texture2D renderTexture = CreateReadableTexture(originalTexture);
            SampleValidPixels(renderTexture);

            Debug.Log("Geçerli pixel sayýsý: " + validPixels.Count);

            // Generate rays based on sampled pixels and camera orientation
            Ray[] rays = GenerateRays(camera, count);

            // Perform raycasts and build BrushPaintData array
            return BuildBrushPaintData(rays);
        }
        #endregion

        #region Helper Methods
        private Texture2D CreateReadableTexture(Texture2D source)
        {
            // Create a readable RGBA32 copy of the original texture
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

        private void SampleValidPixels(Texture2D texture)
        {
            validPixels.Clear();
            Color[] pixels = texture.GetPixels();
            int width = texture.width;
            int height = texture.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color col = pixels[y * width + x];
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
        }

        private Ray[] GenerateRays(Camera camera, int rayCount)
        {
            int count = Mathf.Min(rayCount, validPixels.Count);
            List<Ray> rays = new List<Ray>(count);

            // Compute plane axes based on camera-relative brush plane
            Vector3 tangent = Vector3.Cross(lastHitNormal, camera.transform.right).normalized;
            if (tangent == Vector3.zero)
                tangent = camera.transform.up;

            Vector3 bitangent = Vector3.Cross(lastHitNormal, tangent).normalized;

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, validPixels.Count);
                PixelSample sample = validPixels[index];

                Vector2 uv = new Vector2(
                    (float)sample.position.x / originalTexture.width,
                    (float)sample.position.y / originalTexture.height
                );

                // Convert UV to world position using rotated axes (flip X and Y as needed)
                Vector3 worldPos = lastHitPoint
                    + -(uv.x - 0.5f) * lastBrushSize * 2 * bitangent
                    - (uv.y - 0.5f) * lastBrushSize * 2 *tangent;

                Vector3 origin = worldPos + lastHitNormal * 0.1f;
                Vector3 direction = -lastHitNormal;
                rays.Add(new Ray(origin, direction));
            }

            return rays.ToArray();
        }

        private BrushPaintData[] BuildBrushPaintData(Ray[] rays)
        {
            int length = rays.Length;
            BrushPaintData[] brushDatas = new BrushPaintData[length];

            for (int i = 0; i < length; i++)
            {
                Physics.Raycast(rays[i], out RaycastHit hitInfo);

                Quaternion hitRotation = Quaternion.identity;
                if (IsValidQuaternion(Quaternion.LookRotation(hitInfo.normal)))
                {
                    // Align forward to hit.normal
                    hitRotation = Quaternion.LookRotation(Vector3.Cross(hitInfo.normal,Vector3.right));
                }

                brushDatas[i] = new BrushPaintData
                {
                    position = hitInfo.point,
                    rotation = hitRotation,
                    scale = Vector3.one
                };
            }

            return brushDatas;
        }

        private static bool IsValidQuaternion(Quaternion q)
        {
            float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return magnitude > 0.0001f && !float.IsNaN(magnitude) && !float.IsInfinity(magnitude);
        }
        #endregion

    }

    public struct PixelSample
    {
        public Vector2Int position;
        public Color color;
    }
}