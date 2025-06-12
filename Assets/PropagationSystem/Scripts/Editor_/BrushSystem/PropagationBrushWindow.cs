// Assets/PropagationSystem/Editor/PropagationBrushWindow.cs
using UnityEditor;
using UnityEngine;
using PropagationSystem;
using PropagationSystem.Editor;
using System.IO;
using System;
namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
  
    public class PropagationBrushWindow : EditorWindow //View class
    {

        #region Variables

        #region Static & Core References

        private PropagationBrushWindowPresenter presenter;
        private static SceneData sceneData;
        public static Action<int> OnBrushStroke;

        #endregion

        #region GUI State & Scroll

        private Vector2 scrollPos;

        #endregion

        #region Brush - Runtime State

        private bool brushEnabled;
        private float view_BrushSize = 1f;
        private int view_BrushDensity = 50;
        private int view_SelectedBrushIndex;
        private BrushSetSO view_SelectedBrushSet;
        private bool view_IsBrushEnabled = false;
        private int view_SelectedMeshIndex;
        private int view_InstanceCount;
        private bool view_IsPressing;
        #endregion

        #region Brush - Enums

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

        #region View - Visual Cache

        private Texture2D view_CurrentBrushTexture;
        private string view_SelectedBrushName;

        #endregion

        #region Editor Preferences Keys

        const string SceneData_PrefKey = "EditorPref_SceneDataKey";
        const string BrushSet_PrefKey = "EditorPref_BrushSetKey";
        const string BrushSize_PrefKey = "EditorPref_BrushSizeKey";
        const string Density_PrefKey = "EditorPref_DensityKey";
        const string SelectedMeshIndex_PrefKey = "EditorPref_SelectedMeshIndexKey";
        const string SelectedBrushIndex_PrefKey = "EditorPref_SelectedBrushIndexKey";

        #endregion

        #region Resource GUID Paths

        private string windowIconPath;
        private string brushMeshPath;
        private string brushMaterialPath;

        #endregion

        #region UI - View Event Actions

        public Action<float> OnBrushSizeChanged;
        public Action<int> OnBrushDensityChanged;
        public Action<bool> OnBrushToggled;
        public Action<int> OnSelectedBrushChanged;
        public Action<BrushSetSO> OnBrushSetChanged;
        public Action<int> OnSelectedMeshChanged;
        public Action<BrushMode> OnBrushModeChanged;
        public Action<PropagationMode> OnSampleModeChanged;
        public Action<int> OnInstanceCountChanged;
        public Action<MeshData> OnMeshCreated;
        public Action<SceneData> OnSceneDataChanged;
        public Action<int> OnMeshRemoved;
        public Action OnPaintRequested;
        #endregion

        #endregion


        #region Presenter Set Functions

        public void SetCurrentBrushTextureAndName(Texture2D texture, string brushName)
        {
            view_CurrentBrushTexture = texture;
            view_SelectedBrushName = brushName;
        }


        #endregion

        #region External View Events 

        private void OnNewMeshAdded(MeshData data)
        {
            OnMeshCreated?.Invoke(data);
        }

        private void OnNewMeshAddWindowClosed()
        {
            CreateNewMeshType.Instance.OnMeshCreated -= OnNewMeshAdded;
            CreateNewMeshType.Instance.OnWindowClosed -= OnNewMeshAddWindowClosed;
        }

        #endregion

        #region Window Utilities

        [MenuItem("Tools/Propagation Brush")]
        public static void OpenWindow()
        {
            PropagationBrushWindow window = GetWindow<PropagationBrushWindow>("Propagation Brush");

            window.minSize = new Vector2(409, 1004);

            window.presenter = new PropagationBrushWindowPresenter(window);

         

            window.Initialize();
        }

       

        public  void Refresh()
        {
            Initialize();
        }

        private  void Initialize()
        {

            SceneView.duringSceneGui -= OnSceneGUI; // double eklemeyi engelle
            SceneView.duringSceneGui += OnSceneGUI;
            windowIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.PROPAGATIONICONGUID);
            brushMeshPath = AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHMESHGUID);
            brushMaterialPath = AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHMATERIALGUID); 
            
        }

        public static SceneData GetCurrentSceneData()
        {
            return sceneData;
        }
     
        public static void CloseWindow()
        {
           
            GetWindow<PropagationBrushWindow>().Close();
        }
      
        #endregion

        private static void OnBrushApplied(BrushPaintData[] datas,bool isErase = false)
        {

            Undo.RecordObject(sceneData, "Brush Stroke");


           

            if (!isErase)
            {
                Debug.Log("Event çaðýrýldý data countýda þu :" + datas.Length);
                for (int i = 0; i < datas.Length; i++)
                {



                    SavedPositions savedPositions = new SavedPositions();
                    savedPositions.position = datas[i].position;
                    savedPositions.rotation = datas[i].rotation;
                    savedPositions.scale = datas[i].scale;




                    //sceneData.propagatedObjectDatas[selectedMeshIndex_Static].trsMatrices.Add(savedPositions);

                 
                }


                EditorUtility.SetDirty(sceneData);
                //OnBrushStroke?.Invoke(selectedMeshIndex_Static);
                EditorPreviewer.CalculateFrustum();
            }
            else
            {
                EditorUtility.SetDirty(sceneData);
               // OnBrushStroke?.Invoke(selectedMeshIndex_Static);
                EditorPreviewer.CalculateFrustum();
            }
        }


        public static int GetSelectedMeshIndex()
        {
            // return selectedMeshIndex_Static;
            return 0;
        }



        #region GUI Functions
        void GUI_Label()
        {
            GUILayout.Label("Propagation", EditorStyles.boldLabel); // Header
        }

        void GUI_UpdateIcon()
        {
            Texture2D windowIcon = EditorGUIUtility.Load(windowIconPath) as Texture2D;
            titleContent = new GUIContent(" Propagation", windowIcon);
        }

        void GUI_SceneData()
        {
            sceneData = (SceneData)EditorGUILayout.ObjectField("Scene Data", sceneData, typeof(SceneData), false);
            OnSceneDataChanged?.Invoke(sceneData);
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

            Texture2D selectedPreview = AssetPreview.GetAssetPreview(sceneData.propagatedMeshDefinitions[view_SelectedMeshIndex].mesh);

            GUILayout.Label(selectedPreview, GUILayout.Width(150), GUILayout.Height(150));
            GUILayout.Label("Selected Mesh: " + sceneData.propagatedMeshDefinitions[view_SelectedMeshIndex].name, EditorStyles.centeredGreyMiniLabel);

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
                    view_SelectedMeshIndex = i;
                    // selectedMeshIndex_Static = i;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string removeMeshIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.REMOVEMESHTYPEICONGUID);
            Texture2D removeMeshIcon = EditorGUIUtility.Load(removeMeshIconPath) as Texture2D;

            GUI_CreateNewMesh();

            if (GUILayout.Button(removeMeshIcon, GUILayout.Width(48), GUILayout.Height(48)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Are you sure?",
                    "This will delete selected mesh type and all data belongs to it?",
                    "Yes",
                    "No"
                );

                if (confirmed)
                {
                    OnMeshRemoved?.Invoke(view_SelectedMeshIndex);    
                    view_SelectedMeshIndex = 0;      
                }
            }

            OnSelectedMeshChanged?.Invoke(view_SelectedMeshIndex);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void GUI_BrushSet()
        {    
            view_SelectedBrushSet = (BrushSetSO)EditorGUILayout.ObjectField("Brush Set", view_SelectedBrushSet, typeof(BrushSetSO), false);
            OnBrushSetChanged?.Invoke(view_SelectedBrushSet);
        }

        void GUI_BrushSettings()
        {
            Color oldColor = GUI.backgroundColor;

            GUILayout.Label("Brush Settings", EditorStyles.boldLabel);

            if (view_CurrentBrushTexture != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();

                GUILayout.Label(view_CurrentBrushTexture, GUILayout.Height(128), GUILayout.Width(128));
                GUILayout.Label("Selected Brush: " + view_SelectedBrushName);

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

            for (int i = 0; i < view_SelectedBrushSet.brushes.Count; i++)
            {
                if (i % buttonsPerRow == 0)
                {
                    if (i != 0) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                GUIContent buttonContent = new GUIContent("", view_SelectedBrushSet.brushes[i].maskTexture);

                if (GUILayout.Button(buttonContent, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    this.view_SelectedBrushIndex = i;
                    OnSelectedBrushChanged?.Invoke(view_SelectedBrushIndex);
                    Debug.Log(this.view_SelectedBrushIndex);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string addBrushIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.ADDBRUSHICONGUID);
            string removeBrushIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.REMOVEBRUSHICONGUID);
            Texture2D addBrushIcon = EditorGUIUtility.Load(addBrushIconPath) as Texture2D;
            Texture2D removeBrushIcon = EditorGUIUtility.Load(removeBrushIconPath) as Texture2D;

            if (GUILayout.Button(addBrushIcon, GUILayout.Width(48), GUILayout.Height(48)))
            {
                CreateNewBrushType.OpenWindow();
                CreateNewBrushType.SetBrushSet(view_SelectedBrushSet);
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
                    view_SelectedBrushSet.brushes.RemoveAt(this.view_SelectedBrushIndex);

                    if (view_SelectedBrushSet.brushes.Count == 0)
                    {
                        view_SelectedBrushSet.brushes.Add(GetDefaultBrush());
                    }

                    EditorUtility.SetDirty(view_SelectedBrushSet);
                    selectedBrushIndex = 0;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            view_BrushSize = EditorGUILayout.Slider("Brush Size", view_BrushSize, 0.1f, 100f);
            view_BrushDensity = EditorGUILayout.IntSlider("Density", view_BrushDensity, 1, 100);
            view_InstanceCount = EditorGUILayout.IntSlider("Instance Count", view_InstanceCount,1,10000);

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string brushIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHICONGUID);
            string eraserIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.ERASEICONGUID);
            Texture2D brushIcon = EditorGUIUtility.Load(brushIconPath) as Texture2D;
            Texture2D eraserIcon = EditorGUIUtility.Load(eraserIconPath) as Texture2D;

            if (brushMode == BrushMode.Paint)
            {
                GUI.backgroundColor = Color.cyan;
            }

            if (GUILayout.Button(brushIcon, GUILayout.Width(64), GUILayout.Height(64)))
            {
                if (!view_IsBrushEnabled)
                {
                    view_IsBrushEnabled = true;
                    OnBrushToggled?.Invoke(view_IsBrushEnabled);
                }

                brushMode = BrushMode.Paint;
            }

            GUI.backgroundColor = oldColor;

            string brushButtonText = view_IsBrushEnabled ? "Disable Brush" : "Enable Brush";

            if (GUILayout.Button(brushButtonText, GUILayout.Width(128), GUILayout.Height(64)))
            {
                view_IsBrushEnabled = !view_IsBrushEnabled;
                OnBrushToggled?.Invoke(view_IsBrushEnabled);
            }

            if (brushMode == BrushMode.Erase)
            {
                GUI.backgroundColor = Color.red;
            }

            if (GUILayout.Button(eraserIcon, GUILayout.Width(64), GUILayout.Height(64)))
            {
                if (!view_IsBrushEnabled)
                {
                    view_IsBrushEnabled = true;
                    OnBrushToggled?.Invoke(view_IsBrushEnabled);
                }

                brushMode = BrushMode.Erase;
            }

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

            #region Presenter Calls 
            OnBrushSizeChanged?.Invoke(view_BrushSize);
            OnBrushDensityChanged?.Invoke(view_BrushDensity);
            OnInstanceCountChanged?.Invoke(view_InstanceCount);
            OnBrushModeChanged?.Invoke(brushMode);
            OnSampleModeChanged?.Invoke(SampleMode);
            #endregion
        }


        void GUI_CreateNewMesh()
        {
            string addMeshIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.ADDMESHTYPEICONGUID);
            Texture2D addMeshIcon = EditorGUIUtility.Load(addMeshIconPath) as Texture2D;

            if (GUILayout.Button(addMeshIcon,GUILayout.Width(48),GUILayout.Height(48)))
            {
                EditorPreviewer.SetPreviewMode(false);
                CreateNewMeshType.OpenWindow();
                CreateNewMeshType.Instance.OnMeshCreated += OnNewMeshAdded;
                CreateNewMeshType.Instance.OnWindowClosed += OnNewMeshAddWindowClosed;
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
            string sourcePath = AssetDatabase.GUIDToAssetPath(GUIDS.DEFAULTBRUSHGUID);
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
                //if (selectedMeshIndex_Static >= sceneData.propagatedMeshDefinitions.Count || selectedMeshIndex >= sceneData.propagatedMeshDefinitions.Count)
                //{
                  //  selectedMeshIndex_Static = 0;
                    //selectedMeshIndex = 0;
                //}
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

            if (view_SelectedBrushSet == null)
            {
                EditorGUILayout.HelpBox("Assign A Brush Set", MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }
            if (view_SelectedBrushSet.brushes.Count == 0)
            {
                EditorGUILayout.HelpBox("Assign A Brush", MessageType.Warning);
                string addBrushIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.ADDBRUSHICONGUID);
                Texture2D addBrushIcon = EditorGUIUtility.Load(addBrushIconPath) as Texture2D;

                if (GUILayout.Button(addBrushIcon, GUILayout.Width(48), GUILayout.Height(48)))
                {
                    CreateNewBrushType.OpenWindow();
                    CreateNewBrushType.SetBrushSet(view_SelectedBrushSet);
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

          

            if (GUILayout.Button("Start Previewing"))
            {
                EditorPreviewer.Setup(sceneData);
                EditorPreviewer.SetPreviewMode(true);
            }
            if (GUILayout.Button("Stop Previewing"))
            {
                EditorPreviewer.SetPreviewMode(false);
            }

            

            // 4) Burada ScrollView'u kapatýyoruz:
            EditorGUILayout.EndScrollView();

        
}




        private void OnSceneGUI(SceneView sceneView)
        {
            // 1. GUI Overlay
            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 200, 20), "Scene GUI aktif");
            Handles.EndGUI();

            // 2. Event Validasyonu
            Event e = Event.current;
            if (e == null)
            {
                Debug.Log("Event Null");
                return;
            }

            // 3. Brush Mode Güncelleme (Shift ile Erase Mode)
            BrushMode currentMode = e.shift ? BrushMode.Erase : brushMode;
            OnBrushModeChanged?.Invoke(currentMode);

            // 4. Brush Aktifken Seçimi Engelle + Input Kullanýmý
            if (view_IsBrushEnabled)
            {
                // Seçimi engelle (Brush aktifken)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                // Mouse input (sadece sol click için)
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    view_IsPressing = true;
                    e.Use(); // Event’i tüketiyoruz
                }

                if (view_IsPressing)
                {
                    Debug.Log("Basýyor");
                    OnPaintRequested?.Invoke();
                    view_IsPressing = false;
                }
            }
            else
            {
                // Brush kapalýysa: Seçim normal Unity davranýþýna döner (bu kýsmý boþ býrakýyoruz)
                // HandleUtility.AddDefaultControl çaðrýsý yapýlmazsa, Unity objeleri seçmeye devam eder.
            }
        }




        #endregion

        #region On Enable Disable
        private void OnEnable()
        {
            string path = EditorPrefs.GetString(SceneData_PrefKey, null);

           

            if (!string.IsNullOrEmpty(path))
            {
                sceneData = AssetDatabase.LoadAssetAtPath<SceneData>(path);
            }

            string brushSetPath = EditorPrefs.GetString(BrushSet_PrefKey, null);

            if (!string.IsNullOrEmpty(brushSetPath))
            {
                view_SelectedBrushSet = AssetDatabase.LoadAssetAtPath<BrushSetSO>(brushSetPath);
            }

            view_BrushSize = EditorPrefs.GetFloat(BrushSize_PrefKey, 1f);

            view_SelectedMeshIndex = EditorPrefs.GetInt(SelectedMeshIndex_PrefKey, 0);

            if (sceneData != null)
            {
                if (view_SelectedMeshIndex >= sceneData.propagatedMeshDefinitions.Count)
                {
                    view_SelectedMeshIndex = 0;
                }
            }
            

            view_SelectedBrushIndex = EditorPrefs.GetInt(SelectedBrushIndex_PrefKey, 0);    

            if (view_SelectedBrushIndex >= view_SelectedBrushSet.brushes.Count)
            {
                view_SelectedBrushIndex = 0;
            }

           
           

            view_BrushDensity = EditorPrefs.GetInt(Density_PrefKey, 50);
          
        }

        
        void OnDisable()
        {
           
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

            if (view_SelectedBrushSet != null)
            {
                string brushSetPath = AssetDatabase.GetAssetPath(view_SelectedBrushSet);
                EditorPrefs.SetString(BrushSet_PrefKey, brushSetPath);
            }
            else
            {
                EditorPrefs.DeleteKey(BrushSet_PrefKey);
            }

            SceneView.duringSceneGui -= OnSceneGUI;

            EditorPrefs.SetFloat(BrushSize_PrefKey, view_BrushSize);
            EditorPrefs.SetInt(SelectedMeshIndex_PrefKey, view_SelectedMeshIndex);
            EditorPrefs.SetInt(Density_PrefKey, view_BrushDensity);
            EditorPrefs.SetInt(SelectedBrushIndex_PrefKey, view_SelectedBrushIndex);
        }
        #endregion
    }

  

#endif
}




