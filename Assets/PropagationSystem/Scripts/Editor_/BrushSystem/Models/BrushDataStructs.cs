using PropagationSystem.Editor;
using UnityEngine;

#if UNITY_EDITOR 

namespace PropagationSystem.Editor
{
    public struct StrokeData
    {
        BrushPaintData[] brushPaintDatas;
        PropagationBrushWindow.BrushMode brushMode;
    }
    public struct BrushPaintData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;


        public BrushPaintData(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;

        }

      
    }
}
#endif