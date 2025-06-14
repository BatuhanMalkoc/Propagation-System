using UnityEngine;
using System.Collections.Generic;
using PropagationSystem;
using PropagationSystem.Editor;


#if UNITY_EDITOR

namespace PropagationSystem.Editor
{
    public class BrushRandom2D 
    {
        #region Fields
        private List<PixelSample> _validPixels = new List<PixelSample>();
        private Texture2D _originalTexture;
        private Vector3 _hitPoint;
        private Vector3 _hitNormal;
        private float _brushSize;
        private float _brushDensity;
        private int _MeshIndex;
        private int _InstanceCount;

        private AdditionalBrushData data;
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
            AdditionalBrushData additionalBrushData,
            Camera camera)
        {
            // Initialize fields
            _originalTexture = brush.maskTexture;
            _hitPoint = hitPoint;
            _hitNormal = hitNormal.normalized;
            _brushSize = brushSize;
            _brushDensity = density;
            _MeshIndex = meshIndex;
            _InstanceCount = count;
            data = additionalBrushData;
            // Process texture and sample pixels
            Texture2D renderTexture = CreateReadableTexture(_originalTexture);
            SampleValidPixels(renderTexture);

            Debug.Log("Geçerli pixel sayýsý: " + _validPixels.Count);

            // Generate rays based on sampled pixels and camera orientation
            Ray[] rays = GenerateRays(camera, count);

            // Perform raycasts and build BrushPaintData array
            return BuildStrokeData(rays,PropagationBrushWindow.BrushMode.Paint);
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
            _validPixels.Clear();
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
                        _validPixels.Add(new PixelSample
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
            int count = Mathf.Min(rayCount, _validPixels.Count);
            List<Ray> rays = new List<Ray>(count);

            // Compute plane axes based on camera-relative brush plane
            Vector3 tangent = Vector3.Cross(_hitNormal, camera.transform.right).normalized;
            if (tangent == Vector3.zero)
                tangent = camera.transform.up;

            Vector3 bitangent = Vector3.Cross(_hitNormal, tangent).normalized;

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, _validPixels.Count);
                PixelSample sample = _validPixels[index];

                Vector2 uv = new Vector2(
                    (float)sample.position.x / _originalTexture.width,
                    (float)sample.position.y / _originalTexture.height
                );

                // Convert UV to world position using rotated axes (flip X and Y as needed)
                Vector3 worldPos = _hitPoint
                    + -(uv.x - 0.5f) * _brushSize * 2 * bitangent
                    - (uv.y - 0.5f) * _brushSize * 2 *tangent;

                Vector3 origin = worldPos + _hitNormal * 0.1f;
                Vector3 direction = -_hitNormal;
                rays.Add(new Ray(origin, direction));
            }

            return rays.ToArray();
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

                Quaternion baseRot = Quaternion.identity;
                if (forward.sqrMagnitude > 0.001f)
                    baseRot = Quaternion.LookRotation(forward, normal);

                // 2) Calculate random/static offsets
                Vector3 randomEuler = CalculateRandomRotation(
                    data.randomRotationMinEuler,
                    data.randomRotationMaxEuler);
                Vector3 totalEulerOffset = data.staticRotationEuler + randomEuler;
                Quaternion offsetRot = Quaternion.Euler(totalEulerOffset);

                // 3) Final rotation
                Quaternion finalRot = baseRot * offsetRot;

                // 4) Position and scale
                Vector3 randomPosOff = CalculateRandomPositionOffset(
                    data.randomPositionOffsetMin,
                    data.randomPositionOffsetMax);
                Vector3 finalPos = hitInfo.point + data.staticPositionOffset + randomPosOff;

                Vector3 randomScale = CalculateRandomScale(
                    data.randomScaleMin,
                    data.randomScaleMax);
                
                randomScale *= CalculateRandomSize(data.randomSizeMin, data.randomSizeMax);

     

                Vector3 finalScale = Vector3.Scale(data.staticScale, randomScale);

               

                // 5) Save
                savedPositionsArray[i] = new SavedPositions
                {
                    position = finalPos,
                    rotation = finalRot,
                    scale = finalScale * data.staticSize
                };
            }

            return new StrokeData
            {
                savedPositions = savedPositionsArray,
                brushMode = brushMode,
                meshIndex = _MeshIndex // Assuming meshIndex is not used in this context
            };
        }

        private Vector3 CalculateRandomPositionOffset(Vector3 minRange,Vector3 maxRange)
        {
           return new Vector3(
                Random.Range(minRange.x, maxRange.x),
                Random.Range(minRange.y, maxRange.y),
                Random.Range(minRange.z, maxRange.z)
            );
        }
     private Vector3 CalculateRandomRotation(Vector3 minRange,Vector3 maxRange)
        {
                        return new Vector3(
                Random.Range(minRange.x, maxRange.x),
                Random.Range(minRange.y, maxRange.y),
                Random.Range(minRange.z, maxRange.z)
            );
        }

        private Vector3 CalculateRandomScale(Vector3 minRange,Vector3 maxRange)
        {
            return new Vector3(
                Random.Range(minRange.x, maxRange.x),
                Random.Range(minRange.y, maxRange.y),
                Random.Range(minRange.z, maxRange.z)
            );
        }
        #endregion

        private float CalculateRandomSize(float minSize, float maxSize)
        {
            return Random.Range(minSize, maxSize);
        }
    }

    public struct PixelSample
    {
        public Vector2Int position;
        public Color color;
    }
}

public struct AdditionalBrushData
{
    public Vector3 staticPositionOffset;
    public Vector3 staticScale;
    public Vector3 staticRotationEuler;
    public float staticSize;

    public Vector3 randomPositionOffsetMin;
    public Vector3 randomPositionOffsetMax;
    public bool randomPositionOffsetPerComponent;

    public Vector3 randomScaleMin;
    public Vector3 randomScaleMax;
    public bool randomScalePerComponent;
    public float randomSizeMin;
    public float randomSizeMax;
    public Vector3 randomRotationMinEuler;
    public Vector3 randomRotationMaxEuler;
    public bool randomRotationPerComponent; 

}


#endif