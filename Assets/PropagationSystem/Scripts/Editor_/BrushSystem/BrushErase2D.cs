using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
    public class BrushErase2D
    {
        public static void EraseInCircle(SceneData sceneData, int meshIndex, Vector3 center, float radius)
        {
            if (sceneData == null) return;
            if (meshIndex < 0 || meshIndex >= sceneData.propagatedObjectDatas.Count) return;

           

          

            TransformData tData = sceneData.propagatedObjectDatas[meshIndex];
            List<SavedPositions> list = tData.instanceDatas;
            float r2 = radius * radius;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if ((list[i].position - center).sqrMagnitude <= r2)
                    list.RemoveAt(i);
            }

            tData.instanceDatas = list;
            sceneData.propagatedObjectDatas[meshIndex] = tData;
            //EditorPreviewer.CalculateFrustum();
            EditorUtility.SetDirty(sceneData);
        }
    }
#endif
}
