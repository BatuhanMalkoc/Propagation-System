// Assets/PropagationSystem/Editor/PropagationBrushWindow.cs
using UnityEditor;
using UnityEngine;
using PropagationSystem;
using PropagationSystem.Editor;
using Unity.VisualScripting;
using UnityEditor.Analytics;
using System.IO;
using System;
namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
    public class PropagationBrushWindow : EditorWindow
    {
        #region Variables

        #region Static Variables 

        private static SceneData sceneData;
        private static int selectedMeshIndex_Static;
        public static Action<int> OnBrushStroke;

        private Vector2 scrollPos;
        #endregion

        #region Brush Variables

        private Texture2D selectedBrushTexture;
        private BrushDataSO selectedBrushSO;
        private static BrushSetSO brushSet;
        private bool brushEnabled;
        private float brushSize = 1f; // Ýleride BrushDataSO'dan alýnabilir
        private int density = 50;
        private bool isBrushEnabled = false; // Brush'un aktif olup olmadýðýný tutar
        private int selectedBrushIndex;
        private static Mesh brushMesh;
        private static Material brushMaterial;
        public enum BrushMode
        {
            Paint,
            Erase
        }

        public enum PropagationMode
        {
            Random,
            RandomWeighted,
            Density
        }

        private PropagationMode SampleMode = PropagationMode.Random;
        private BrushMode brushMode = BrushMode.Paint;
        #endregion

        #region Mesh Variables

        private int selectedMeshIndex = 0;

        #endregion

        #region EditorPrefs

        const string SceneData_PrefKey = "EditorPref_SceneDataKey";
        const string BrushSet_PrefKey = "EditorPref_BrushSetKey";
        const string BrushSize_PrefKey = "EditorPref_BrushSizeKey";
        const string Density_PrefKey = "EditorPref_DensityKey";
        const string SelectedMeshIndex_PrefKey = "EditorPref_SelectedMeshIndexKey";
        const string SelectedBrushIndex_PrefKey = "EditorPref_SelectedBrushIndexKey";

        #endregion

        #region GUID Keys

        private const string BRUSHICONGUID = "b50705d6170a42d4a9480d8e579bca3b";
        private const string ERASEICONGUID = "b1c9c7ca80357484b993b162ad1bf81b";
        private const string PROPAGATIONICONGUID = "53b798f87c3af534893455e7f6514777";
        private const string REMOVEBRUSHICONGUID = "d3ef903f0f464a84396bb1ca53951c6b";
        private const string ADDBRUSHICONGUID = "391b56a0b0f4c714aab3c36bd98ed49f";
        private const string ADDMESHTYPEICONGUID = "16c2e3a7c6ce0284f99dee00097bf695";
        private const string REMOVEMESHTYPEICONGUID = "76dcae87ac35952478b0c03f6443e691";
        private const string DEFAULTBRUSHGUID = "8c4177208ea9dda4b86c883af3f144f9";
        private const string BRUSHPLANEGUID = "c165c719ed1e59d46a5ae3c460f62d64";
        private const string BRUSHMATERIALGUID = "e253b5f545f25494b97e5e5eacebf0b9";
        #endregion

        #endregion

        #region Window Utilities

        [MenuItem("Tools/Propagation Brush")]
        public static void OpenWindow()
        {
           var window = GetWindow<PropagationBrushWindow>("Propagation Brush");

            string brushMeshPath = AssetDatabase.GUIDToAssetPath(BRUSHPLANEGUID);
            brushMesh = (Mesh) EditorGUIUtility.Load(brushMeshPath) as Mesh;
            string brushMaterialPath = AssetDatabase.GUIDToAssetPath(BRUSHMATERIALGUID);
            brushMaterial = (Material) EditorGUIUtility.Load(brushMaterialPath) as Material;
            window.minSize = new Vector2(409, 1004);
        }



        public static void Refresh()
        {
            string brushMeshPath = AssetDatabase.GUIDToAssetPath(BRUSHPLANEGUID);
            string brushMaterialPath = AssetDatabase.GUIDToAssetPath(BRUSHMATERIALGUID);

            brushMesh = (Mesh)EditorGUIUtility.Load(brushMeshPath) as Mesh;
            brushMaterial = (Material)EditorGUIUtility.Load(brushMaterialPath) as Material;
            
        }




        public static SceneData GetCurrentSceneData()
        {
            return sceneData;
        }
        public static BrushSetSO GetCurrentBrushSet()
        {

            return brushSet;
        }
        public static void CloseWindow()
        {
            PropagationBrush.OnBrushApplied -= OnBrushApplied;
            GetWindow<PropagationBrushWindow>().Close();
        }
        void UpdatePropagationBrush()
        {
            PropagationBrush.SetCurrentBrush(selectedBrushSO);
            PropagationBrush.SetCurrentBrushMode(brushMode);
        }
        #endregion

        private static void OnBrushApplied(BrushPaintData[] datas)
        {

            Undo.RecordObject(sceneData, "Brush Stroke");


            Debug.Log("Event çaðýrýldý data countýda þu :" + datas.Length);

          for(int i = 0; i < datas.Length; i++)
            {
               


                SavedPositions savedPositions = new SavedPositions();
                savedPositions.position = datas[i].position;
                savedPositions.rotation = datas[i].rotation;
                savedPositions.scale = datas[i].scale;




                sceneData.propagatedObjectDatas[selectedMeshIndex_Static].trsMatrices.Add(savedPositions);

                Debug.Log("Eklendi" + selectedMeshIndex_Static);
            }

        
          EditorUtility.SetDirty(sceneData);
            OnBrushStroke?.Invoke(selectedMeshIndex_Static);
            EditorPreviewer.CalculateFrustum();

        }


        

        #region GUI Functions
        void GUI_Label()
        {
            GUILayout.Label("Propagation", EditorStyles.boldLabel); //Header
        }

        void GUI_UpdateIcon()
        {
            string windowIconPath = AssetDatabase.GUIDToAssetPath(PROPAGATIONICONGUID);
            Texture2D windowIcon = EditorGUIUtility.Load(windowIconPath) as Texture2D;
            titleContent = new GUIContent(" Propagation", windowIcon);
        }

        void GUI_SceneData()
        {
            sceneData = (SceneData)EditorGUILayout.ObjectField("Scene Data", sceneData, typeof(SceneData), false);
        }

        void GUI_MeshList()
        {
            GUILayout.Label("Mesh Picker", EditorStyles.boldLabel);

            SerializedObject so = new SerializedObject(sceneData);
            SerializedProperty meshList = so.FindProperty("propagatedMeshDefinitions");
            so.ApplyModifiedProperties();

            if (sceneData.propagatedMeshDefinitions.Count == 0)
            {
                EditorGUILayout.HelpBox("Please Add Mesh To Propagate.", MessageType.Warning);
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            Texture2D selectedPreview = AssetPreview.GetAssetPreview(sceneData.propagatedMeshDefinitions[selectedMeshIndex].mesh);

            GUILayout.Label(selectedPreview, GUILayout.Width(150), GUILayout.Height(150));

            GUILayout.Label("Selected Mesh: " + sceneData.propagatedMeshDefinitions[selectedMeshIndex].name, EditorStyles.centeredGreyMiniLabel);


            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

      
            int buttonSize = 80;
            int padding = 10;
            int viewWidth = (int)EditorGUIUtility.currentViewWidth;
            int buttonsPerRow = Mathf.Max(1, (viewWidth - padding) / (buttonSize + padding));

            GUILayout.BeginVertical();

            for (int i = 0; i < sceneData.propagatedMeshDefinitions.Count; i++)
            {
                if (i % buttonsPerRow == 0)
                {
                    if (i != 0) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                Texture2D preview = AssetPreview.GetAssetPreview(sceneData.propagatedMeshDefinitions[i].mesh);
                GUIContent buttonContent = new GUIContent(preview, sceneData.propagatedMeshDefinitions[i].name);

                if (GUILayout.Button(buttonContent, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    selectedMeshIndex = i;
                    selectedMeshIndex_Static = i; // Update the selected mesh index
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
           
            string removeMeshIconPath = AssetDatabase.GUIDToAssetPath(REMOVEMESHTYPEICONGUID);
            
            Texture2D removeMeshIcon = EditorGUIUtility.Load(removeMeshIconPath) as Texture2D;

            GUI_CreateNewMesh();
            if (GUILayout.Button(removeMeshIcon,GUILayout.Width(48),GUILayout.Height(48)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Are you sure?",
                    "This will delete selected mesh type and all data belongs to it?",
                    "Yes",    // Accept button
                    "No"      // Cancel button
                );

                if (confirmed)
                {
                    // Silme iþlemi
                  sceneData.propagatedMeshDefinitions.RemoveAt(selectedMeshIndex);

                    selectedMeshIndex = 0;

                    sceneData.OnValidateExternalCall();

                    EditorUtility.SetDirty(sceneData);

                }
            }

         
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void GUI_BrushSet()
        {    
            brushSet = (BrushSetSO)EditorGUILayout.ObjectField("Brush Set", brushSet, typeof(BrushSetSO), false);
        }

        void GUI_BrushSettings()
        {

            Color oldColor = GUI.backgroundColor;

            GUILayout.Label("Brush Settings", EditorStyles.boldLabel);
        
            if (selectedBrushTexture != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();

                GUILayout.Label(selectedBrushTexture, GUILayout.Height(128), GUILayout.Width(128));

                if (selectedBrushSO != null)
                {
                    GUILayout.Label("Selected Brush: " + selectedBrushSO.brushName);

                }

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            int selectedBrushIndex = -1;
            int buttonSize = 64;
            int padding = 5;

            int viewWidth = (int)EditorGUIUtility.currentViewWidth;
            int buttonsPerRow = Mathf.Max(1, (viewWidth - padding) / (buttonSize + padding));

            GUILayout.BeginVertical();

            for (int i = 0; i < brushSet.brushes.Count; i++)
            {
                if (i % buttonsPerRow == 0)
                {
                    if (i != 0) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                GUIContent buttonContent = new GUIContent("", brushSet.brushes[i].maskTexture);

                if (GUILayout.Button(buttonContent, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    selectedBrushIndex = i;

                    this.selectedBrushIndex = i; // Update the selected brush index

                    selectedBrushSO = brushSet.brushes[i];

                    selectedBrushTexture = brushSet.brushes[i].maskTexture;

                    PropagationBrush.SetCurrentBrush(brushSet.brushes[i]);

                    Debug.Log(this.selectedBrushIndex);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            string addBrushIconPath = AssetDatabase.GUIDToAssetPath(ADDBRUSHICONGUID);
            string removeBrushIconPath = AssetDatabase.GUIDToAssetPath(REMOVEBRUSHICONGUID);
            Texture2D addBrushIcon = EditorGUIUtility.Load(addBrushIconPath) as Texture2D;
            Texture2D removeBrushIcon = EditorGUIUtility.Load(removeBrushIconPath) as Texture2D;
            if(GUILayout.Button(addBrushIcon,GUILayout.Width(48),GUILayout.Height(48)))
            {
                CreateNewBrushType.OpenWindow();
                CreateNewBrushType.SetBrushSet(brushSet);
            }
            if (GUILayout.Button(removeBrushIcon, GUILayout.Width(48), GUILayout.Height(48)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Are You Sure?",
                    "This will delete selected brush",
                    "Delete",
                    "Cancel"

                    );

                if (confirmed)
                {
                    brushSet.brushes.RemoveAt(this.selectedBrushIndex);
                    if(brushSet.brushes.Count ==0)
                    {
                        brushSet.brushes.Add(GetDefaultBrush());
                    }
                    EditorUtility.SetDirty(brushSet);
                    selectedBrushIndex = 0;
                }


            }
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 100f);
            density = EditorGUILayout.IntSlider("Density", density, 1, 100);

            PropagationBrush.SetBrushSize(brushSize);

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string brushIconPath = AssetDatabase.GUIDToAssetPath(BRUSHICONGUID);
            string eraserIconPath = AssetDatabase.GUIDToAssetPath(ERASEICONGUID);
            Texture2D brushIcon = EditorGUIUtility.Load(brushIconPath) as Texture2D;
            Texture2D eraserIcon = EditorGUIUtility.Load(eraserIconPath) as Texture2D;

            


            if(brushMode == BrushMode.Paint)
            {
                GUI.backgroundColor = Color.cyan;
            }
        
            if (GUILayout.Button(brushIcon,GUILayout.Width(64),GUILayout.Height(64)))
            {
                if(!isBrushEnabled)
                {
                    isBrushEnabled = true;
                    PropagationBrush.OnOff(isBrushEnabled);
                    PropagationBrush.SetMesh(brushMesh);
                    PropagationBrush.SetMaterial(brushMaterial);
                }
                brushMode = BrushMode.Paint;
            }
            GUI.backgroundColor = oldColor;
            string brushButtonText = isBrushEnabled ? "Disable Brush" : "Enable Brush";
            if (GUILayout.Button(brushButtonText,GUILayout.Width(128),GUILayout.Height(64)))
            {
                isBrushEnabled = !isBrushEnabled;
                brushMode = BrushMode.Paint; // Reset brush mode to Paint when turning off
                PropagationBrush.OnOff(isBrushEnabled);
                PropagationBrush.SetMesh(brushMesh);
                PropagationBrush.SetMaterial(brushMaterial);
            }
            if (brushMode == BrushMode.Erase)
            {
                GUI.backgroundColor = Color.red;
            }
           
            if (GUILayout.Button(eraserIcon,GUILayout.Width(64),GUILayout.Height(64)))
            {
                if (!isBrushEnabled)
                {
                    isBrushEnabled = true;
                    PropagationBrush.OnOff(isBrushEnabled);
                    PropagationBrush.SetMesh(brushMesh);
                    PropagationBrush.SetMaterial(brushMaterial);
                }
                brushMode = BrushMode.Erase;
            }
            GUI.backgroundColor = oldColor;


            GUI.backgroundColor = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

           

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

           



            if (SampleMode == PropagationMode.Random)
            {
                GUI.backgroundColor = Color.green;
            }
            if (GUILayout.Button("Random", GUILayout.Width(128), GUILayout.Height(64)))
            {
                SampleMode = PropagationMode.Random;
            }
            GUI.backgroundColor = oldColor;
            if (SampleMode == PropagationMode.RandomWeighted)
            {
                GUI.backgroundColor = Color.green;
            }
            if (GUILayout.Button("Random Weighted", GUILayout.Width(128), GUILayout.Height(64)))
            {
                SampleMode = PropagationMode.RandomWeighted;
            }
            GUI.backgroundColor = oldColor;
            if (SampleMode == PropagationMode.Density)
            {
                GUI.backgroundColor = Color.green;
            }
            if (GUILayout.Button("By Density", GUILayout.Width(128), GUILayout.Height(64)))
            {
               SampleMode = PropagationMode.Density;
            }

            GUI.backgroundColor = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void GUI_CreateNewMesh()
        {
            string addMeshIconPath = AssetDatabase.GUIDToAssetPath(ADDMESHTYPEICONGUID);
            Texture2D addMeshIcon = EditorGUIUtility.Load(addMeshIconPath) as Texture2D;

            if (GUILayout.Button(addMeshIcon,GUILayout.Width(48),GUILayout.Height(48)))
            {
                EditorPreviewer.SetPreviewMode(false);
                CreateNewMeshType.OpenWindow();
            }
        }

        private BrushDataSO GetDefaultBrush()
        {
            string defaultFolder = "Assets/EditorGenerated/Brushes";
            string defaultSearchName = "DefaultBrush";

            // 1. Mevcut asset'leri kontrol et
            string[] guids = AssetDatabase.FindAssets($"{defaultSearchName} t:BrushDataSO", new[] { defaultFolder });

            if (guids != null && guids.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                BrushDataSO existing = AssetDatabase.LoadAssetAtPath<BrushDataSO>(existingPath);
                if (existing != null)
                {
                    Debug.Log("[BrushSystem] Var olan DefaultBrush kullanýldý.");
                    return existing;
                }
            }

            // 2. Orijinal default brush'ý al
            string sourcePath = AssetDatabase.GUIDToAssetPath(DEFAULTBRUSHGUID);
            BrushDataSO original = AssetDatabase.LoadAssetAtPath<BrushDataSO>(sourcePath);
           
            if (original == null)
            {
                Debug.LogError("Default brush bulunamadý!");
                return null;
            }

            // 3. Kopyasýný oluþtur
            BrushDataSO copy = ScriptableObject.Instantiate(original);

            // 4. Klasörleri oluþtur (yoksa)
            if (!AssetDatabase.IsValidFolder(defaultFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/EditorGenerated"))
                    AssetDatabase.CreateFolder("Assets", "EditorGenerated");

                AssetDatabase.CreateFolder("Assets/EditorGenerated", "Brushes");
            }

            // 5. Uygun isim üret
            string fullFolderPath = Path.Combine(Application.dataPath, "EditorGenerated/Brushes");
            string uniqueFileName = CreateNewBrushType.GetUniqueAssetPath(fullFolderPath, defaultSearchName); // örn: DefaultBrush_1.asset
            string fullAssetPath = $"{defaultFolder}/{uniqueFileName}";

            // 6. Asset olarak kaydet
            AssetDatabase.CreateAsset(copy, fullAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 7. Yükleyip geri dön
            BrushDataSO loaded = AssetDatabase.LoadAssetAtPath<BrushDataSO>(fullAssetPath);
            Debug.Log("[BrushSystem] Yeni DefaultBrush instance'ý oluþturuldu.");
            return loaded;
        }


        private void OnGUI()
        {
            // 2) Mevcut tüm GUI içeriðinizi aþaðýdaki iki satýr arasýna alýn:
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));

            GUI_Label();
            GUI_UpdateIcon();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI_SceneData();

            if (sceneData != null)
            {
                if (selectedMeshIndex_Static >= sceneData.propagatedMeshDefinitions.Count || selectedMeshIndex >= sceneData.propagatedMeshDefinitions.Count)
                {
                    selectedMeshIndex_Static = 0;
                    selectedMeshIndex = 0;
                }
            }
            EditorGUILayout.Space(2);

            if (sceneData == null)
            {
                EditorGUILayout.HelpBox("Assign A Scene Data First", MessageType.Warning);
                EditorGUILayout.EndVertical();
                // 3) ScrollView'u kapatmadan önce return ediyorsunuz, bu durumda
                //    EndScrollView() da çaðrýlmalý. Bu nedenle buraya bir EndScrollView() ekledik.
                EditorGUILayout.EndScrollView();
                return;
            }
            else if (sceneData.propagatedMeshDefinitions.Count == 0)
            {
                EditorGUILayout.HelpBox("Add Atleast One Mesh To Propagate", MessageType.Warning);
                EditorGUILayout.EndVertical();
                GUI_CreateNewMesh();
                // Burada da return varsa EndScrollView() unutulmamalý:
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI_BrushSet();

            EditorGUILayout.Space(2);

            EditorGUILayout.EndVertical();

            if (brushSet == null)
            {
                EditorGUILayout.HelpBox("Assign A Brush Set", MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }
            if (brushSet.brushes.Count == 0)
            {
                EditorGUILayout.HelpBox("Assign A Brush", MessageType.Warning);
                string addBrushIconPath = AssetDatabase.GUIDToAssetPath(ADDBRUSHICONGUID);
                Texture2D addBrushIcon = EditorGUIUtility.Load(addBrushIconPath) as Texture2D;

                if (GUILayout.Button(addBrushIcon, GUILayout.Width(48), GUILayout.Height(48)))
                {
                    CreateNewBrushType.OpenWindow();
                    CreateNewBrushType.SetBrushSet(brushSet);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }

            GUI_BrushSettings();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI_MeshList();

            EditorGUILayout.EndVertical();

            if (brushMesh == null || brushMaterial == null)
            {
                Refresh();
            }

            if (GUILayout.Button("Start Previewing"))
            {
                EditorPreviewer.Setup(sceneData);
                EditorPreviewer.SetPreviewMode(true);
            }
            if (GUILayout.Button("Stop Previewing"))
            {
                EditorPreviewer.SetPreviewMode(false);
            }

            UpdatePropagationBrush();

            // 4) Burada ScrollView'u kapatýyoruz:
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region On Enable Disable
        private void OnEnable()
        {
            string path = EditorPrefs.GetString(SceneData_PrefKey, null);

            PropagationBrush.OnBrushApplied += OnBrushApplied;

            if (!string.IsNullOrEmpty(path))
            {
                sceneData = AssetDatabase.LoadAssetAtPath<SceneData>(path);
            }

            string brushSetPath = EditorPrefs.GetString(BrushSet_PrefKey, null);

            if (!string.IsNullOrEmpty(brushSetPath))
            {
                brushSet = AssetDatabase.LoadAssetAtPath<BrushSetSO>(brushSetPath);
            }

            brushSize = EditorPrefs.GetFloat(BrushSize_PrefKey, 1f);

            selectedMeshIndex = EditorPrefs.GetInt(SelectedMeshIndex_PrefKey, 0);

            if (sceneData != null)
            {
                if (selectedMeshIndex >= sceneData.propagatedMeshDefinitions.Count)
                {
                    selectedMeshIndex = 0;
                }
            }
            

            selectedBrushIndex = EditorPrefs.GetInt(SelectedBrushIndex_PrefKey, 0);    

            if (selectedBrushIndex >= brushSet.brushes.Count)
            {
                selectedBrushIndex = 0;
            }

            selectedBrushSO = brushSet?.brushes[selectedBrushIndex];
            selectedBrushTexture = selectedBrushSO?.maskTexture;

            density = EditorPrefs.GetInt(Density_PrefKey, 50);
          
        }

        
        void OnDisable()
        {
            PropagationBrush.OnBrushApplied -= OnBrushApplied;
            // Pencere kapanýrken seçilen referansýn yolunu kaydet
            if (sceneData != null)
            {
                string path = AssetDatabase.GetAssetPath(sceneData);
                EditorPrefs.SetString(SceneData_PrefKey, path);
                
            }
            else
            {
                EditorPrefs.DeleteKey(SceneData_PrefKey);

            }

            if (brushSet != null)
            {
                string brushSetPath = AssetDatabase.GetAssetPath(brushSet);
                EditorPrefs.SetString(BrushSet_PrefKey, brushSetPath);
            }
            else
            {
                EditorPrefs.DeleteKey(BrushSet_PrefKey);
            }

            EditorPrefs.SetFloat(BrushSize_PrefKey, brushSize);
            EditorPrefs.SetInt(SelectedMeshIndex_PrefKey, selectedMeshIndex);
            EditorPrefs.SetInt(Density_PrefKey, density);
            EditorPrefs.SetInt(SelectedBrushIndex_PrefKey, selectedBrushIndex);
        }
        #endregion
    }

#endif
}






public struct BrushPaintData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;


    public BrushPaintData(Vector3 position,Quaternion rotation,Vector3 scale)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;

    }
}
