#if UNITY_EDITOR
using PropagationSystem.Editor;
using PropagationSystem;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace PropagationSystem.Editor
{
    public class CreateNewBrushType : EditorWindow
    {
        public static BrushSetSO brushSet;
        public Texture2D maskTexture;
        public string brushName = "";

        private GUIStyle headerStyle;
        private GUIStyle separatorStyle;
        private GUIStyle errorStyle;

        private void OnEnable()
        {
            InitStyles();
        }

        private void InitStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };

            separatorStyle = new GUIStyle()
            {
                normal = { background = EditorGUIUtility.whiteTexture },
                margin = new RectOffset(0, 0, 4, 4),
                fixedHeight = 1
            };

            errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
                alignment = TextAnchor.MiddleCenter
            };
        }

        public static void OpenWindow()
        {
            var window = GetWindow<CreateNewBrushType>("Add New Brush", true);
            window.minSize = new Vector2(320, 420);
            window.maxSize = new Vector2(420, 500);
        }

        public static void SetBrushSet(BrushSetSO BrushSet)
        {
            brushSet = BrushSet;
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Add New Brush", headerStyle);
            DrawSeparator();

            DrawTextureField();
            DrawNameField();

            GUILayout.Space(10);
            DrawSeparator();
            GUILayout.Space(10);

            DrawActionButtons();
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
        }

        private void DrawTextureField()
        {
            GUILayout.Label("Brush Mask Texture", EditorStyles.boldLabel);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            maskTexture = (Texture2D)EditorGUILayout.ObjectField(
                maskTexture,
                typeof(Texture2D),
                false,
                GUILayout.Width(128),
                GUILayout.Height(128)
            );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (maskTexture == null)
                GUILayout.Label("Please assign a texture.", errorStyle);

            GUILayout.Space(10);
        }

        private void DrawNameField()
        {
            GUILayout.Label("Brush Name", EditorStyles.boldLabel);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            brushName = EditorGUILayout.TextField(brushName, GUILayout.Width(240));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(brushName))
                GUILayout.Label("Please enter a name.", errorStyle);
        }

        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Height(32), GUILayout.Width(100)))
            {
                Close();
            }

            if (GUILayout.Button("Add Brush", GUILayout.Height(32), GUILayout.Width(150)))
            {
                TryCreateBrushAsset();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void TryCreateBrushAsset()
        {
            if (maskTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Brush Mask Texture.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(brushName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a Brush Name.", "OK");
                return;
            }

            var brushDataSO = ScriptableObject.CreateInstance<BrushDataSO>();
            brushDataSO.brushName = brushName;
            brushDataSO.maskTexture = maskTexture;

            string absolutePath = EditorUtility.OpenFolderPanel("Select Folder to Save", "Assets", "");
            if (string.IsNullOrEmpty(absolutePath)) return;

            string relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            string fileName = brushName.Replace(" ", "_");
            string uniqueName = GetUniqueAssetPath(absolutePath, fileName);
            string assetPath = Path.Combine(relativePath, uniqueName);

            AssetDatabase.CreateAsset(brushDataSO, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var loaded = AssetDatabase.LoadAssetAtPath<BrushDataSO>(assetPath);
            if (loaded != null)
            {
                brushSet.brushes.Add(loaded);
                EditorUtility.SetDirty(brushSet);
            }

            Close();
        }

        public static string GetUniqueAssetPath(string folderAbsolutePath, string baseName)
        {
            string extension = ".asset";
            string finalName = baseName;
            int counter = 1;

            while (File.Exists(Path.Combine(folderAbsolutePath, finalName + extension)))
            {
                finalName = $"{baseName}_{counter}";
                counter++;
            }

            return finalName + extension;
        }
    }
}
#endif
