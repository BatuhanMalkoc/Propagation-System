// Assets/PropagationSystem/Editor/PropagationBrushWindow.cs
using UnityEditor;
using UnityEngine;
using PropagationSystem;
using System.Linq;

namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
    public class PropagationBrushWindow : EditorWindow
    {
        private static SceneData sceneData;
        private BrushDataSO selectedBrush;
        private bool brushEnabled;

        [MenuItem("Tools/Propagation Brush")]
        public static void OpenWindow()
        {
            GetWindow<PropagationBrushWindow>("Propagation Brush");
            PropagationBrush.OnBrushApplied += OnBrushApplied;
        }

        public static void CloseWindow()
        {
            PropagationBrush.OnBrushApplied -= OnBrushApplied;
            GetWindow<PropagationBrushWindow>().Close();
        }
        private void OnDisable()
        {
            PropagationBrush.OnBrushApplied -= OnBrushApplied;
        }
        private static void OnBrushApplied(BrushPaintData[] datas)
        {
          for(int i = 0; i < datas.Length; i++)
            {
                Matrix4x4 trsMatrix = Matrix4x4.TRS(datas[i].position, Quaternion.LookRotation(datas[i].normal), Vector3.one);

                sceneData.propagatedObjectDatas[0].trsMatrices.Add(trsMatrix);
                EditorUtility.SetDirty(sceneData);
            }

          


        }
        private void OnGUI()
        {
            GUILayout.Label("Brush Ayarlarý", EditorStyles.boldLabel);

            // 1) ScriptableObject seçimi
            selectedBrush = (BrushDataSO)EditorGUILayout.ObjectField(
                "Brush Data", selectedBrush, typeof(BrushDataSO), false);
            EditorGUILayout.Space();

            sceneData = (SceneData)EditorGUILayout.ObjectField(
                "Scene Data", sceneData, typeof(SceneData), false);

            // 2) Enable/Disable toggle
            brushEnabled = EditorGUILayout.Toggle("Brush Aktif", brushEnabled);

            // 3) Uygula butonu
            if (GUILayout.Button("Uygula"))
            {
                if (brushEnabled)
                    PropagationBrush.SetCurrentBrush(selectedBrush);
                else
                    PropagationBrush.SetCurrentBrush(null);
            }

            // 4) Bilgi
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Aktif Brush:", PropagationBrush.GetCurrentBrush()?.name ?? "Yok");
        }
    }

#endif
}


public struct BrushPaintData
{
    public Vector3 position;
    public Vector3 normal;


    public BrushPaintData(Vector3 position, Vector3 normal)
    {
        this.position = position;
        this.normal = normal;
 
    }
}
