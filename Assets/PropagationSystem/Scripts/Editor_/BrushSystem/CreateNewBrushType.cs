
#if UNITY_EDITOR
using PropagationSystem.Editor;
using PropagationSystem;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace PropagationSystem.Editor
{

    public class CreateNewBrushType : EditorWindow
    {

        public static BrushSetSO brushSet;
        public Texture2D maskTexture;
        public string brushName;
        public bool inverted;

        public static void OpenWindow()
        {
           var window = GetWindow<CreateNewBrushType>("Create New Brush Type",true);
            window.minSize = new Vector2(300, 400);
            window.maxSize = new Vector2(400, 500);
        }

        public static void SetBrushSet(BrushSetSO BrushSet)
        {

            brushSet = BrushSet;
        }
        private void OnGUI()
        {

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            maskTexture = (Texture2D)EditorGUILayout.ObjectField(maskTexture, typeof(Texture2D), false,GUILayout.Height(64),GUILayout.Width(64)) as Texture2D;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Brush Name");
            brushName = EditorGUILayout.TextField(brushName);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Inverted    ");
            inverted = EditorGUILayout.Toggle(inverted);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();


            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Confirm", GUILayout.Height(32), GUILayout.Width(128)))
            {

                if (maskTexture != null && !string.IsNullOrEmpty(brushName))
                {

                    BrushDataSO brushDataSO = ScriptableObject.CreateInstance<BrushDataSO>();
                              
                   brushDataSO.brushName = brushName;
                    brushDataSO.maskTexture = maskTexture;
                    brushDataSO.Invert = inverted;
                    
                   string absolutePath = EditorUtility.OpenFolderPanel("Select Folder To Save", "Assets", "");
                    if (string.IsNullOrEmpty(absolutePath)) return;

                    // Assets klasörüne göre relative path üret
                    string relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);

                    // Asset dosyasýnýn adýný ekle
                    string assetName = brushName.Replace(" ", "_");
                    string uniqueFileName = GetUniqueAssetPath(absolutePath, assetName);

                    string fullAssetPath = Path.Combine(relativePath, uniqueFileName);
                    string fullFileSystemPath = Path.Combine(absolutePath, uniqueFileName);


                    // Asset'i oluþtur
                    AssetDatabase.CreateAsset(brushDataSO, fullAssetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    BrushDataSO loadedBrush = AssetDatabase.LoadAssetAtPath<BrushDataSO>(fullAssetPath);

                    if (loadedBrush != null)
                    {

                        brushSet.brushes.Add(brushDataSO);

                        EditorUtility.SetDirty(brushSet);
                    }
                    Close();
                }
                else
                {
                    bool isShown = false;
                    if (maskTexture == null && !isShown)
                    {
                        EditorUtility.DisplayDialog("Error", "Please Assign A Mask Texture", "OK");
                        isShown = true;
                    }

                    if (string.IsNullOrEmpty(brushName) && !isShown)
                    {
                        EditorUtility.DisplayDialog("Error", "Pleaese Assign A Brush Name", "OK");
                        isShown = true;
                    }
                }

            }
            if (GUILayout.Button("Cancel", GUILayout.Height(32), GUILayout.Width(128)))
            {

                Close();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static string GetUniqueAssetPath(string folderAbsolutePath, string baseName)
        {
            string safeName = baseName.Replace(" ", "_");
            string extension = ".asset";

            string finalName = safeName;
            int counter = 1;

            while (File.Exists(Path.Combine(folderAbsolutePath, finalName + extension)))
            {
                finalName = $"{safeName}_{counter}";
                counter++;
            }

            return finalName + extension; // Örn: "Brush_2.asset"
        }
    }
}
#endif