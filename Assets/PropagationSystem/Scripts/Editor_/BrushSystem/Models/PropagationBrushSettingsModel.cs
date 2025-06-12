#if UNITY_EDITOR

using PropagationSystem;
using PropagationSystem.Editor;
using UnityEditor;
using UnityEngine;

public class PropagationBrushSettingsModel
{
    #region Brush Parameters

    public float brushSize { get; private set; }
    public float brushDensity { get; private set; }

    #endregion

    #region Visual Assets

    public Material brushMaterial { get; private set; }
    public Mesh brushMesh { get; private set; }

    #endregion

    #region Brush State

    public BrushSetSO currentBrushSetSO { get; private set; }
    public BrushDataSO selectedBrushSO { get; private set; }

    public int selectedBrushIndex { get; private set; } = 0;
    public int selectedMeshIndex { get; private set; } = 0;

    public PropagationBrushWindow.BrushMode brushMode { get; private set; } = PropagationBrushWindow.BrushMode.Paint;
    public PropagationBrushWindow.PropagationMode sampleMode { get; private set; } = PropagationBrushWindow.PropagationMode.Random;

    #endregion

    #region Constructor

    public PropagationBrushSettingsModel()
    {
        string brushMeshPath = AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHMESHGUID);
        string brushMaterialPath = AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHMATERIALGUID);

        brushMesh = (Mesh)EditorGUIUtility.Load(brushMeshPath) as Mesh;
        brushMaterial = (Material)EditorGUIUtility.Load(brushMaterialPath) as Material;
    }

    #endregion

    #region Setters

    public void SetBrushSize(float size) { brushSize = size; }
  
    public void SetBrushDensity(float density) { brushDensity = density; }
    
    public void SetBrushMode(PropagationBrushWindow.BrushMode mode) => brushMode = mode;
    public void SetSampleMode(PropagationBrushWindow.PropagationMode mode) => sampleMode = mode;
    public void SetSelectedMeshIndex(int meshIndex) => selectedMeshIndex = meshIndex;
    public void SetCurrentBrushSet(BrushSetSO brushSetSO) => currentBrushSetSO = brushSetSO;

    public void SetCurrentBrushSO(int brushIndex)
    {
        selectedBrushSO = currentBrushSetSO.brushes[brushIndex];
        selectedBrushIndex = brushIndex;
    }

    #endregion
}

#endif
