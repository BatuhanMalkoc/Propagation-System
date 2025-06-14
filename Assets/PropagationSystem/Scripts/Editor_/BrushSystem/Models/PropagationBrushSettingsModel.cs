#if UNITY_EDITOR

using PropagationSystem;
using PropagationSystem.Editor;
using UnityEditor;
using UnityEngine;

public class PropagationBrushSettingsModel
{
    #region Brush Parameters

    public float brushSize { get; private set; }
    public int brushDensity { get; private set; }
    public int instanceCount { get; private set; } // Default instance count for brush strokes

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

    #region Transform Adjustments - Static

    public Vector3 staticPositionOffset { get; private set; } = Vector3.zero;
    public Vector3 staticScale { get; private set; } = Vector3.one;
    public Vector3 staticRotationEuler { get; private set; } = Vector3.zero;

    #endregion

    #region Transform Adjustments - Random

    public Vector3 randomPositionOffsetMin { get; private set; } = Vector3.zero;
    public Vector3 randomPositionOffsetMax { get; private set; } = Vector3.zero;
    public bool randomPositionOffsetPerComponent { get; private set; } = false;

    public Vector3 randomScaleMin { get; private set; } = Vector3.one;
    public Vector3 randomScaleMax { get; private set; } = Vector3.one;
    public bool randomScalePerComponent { get; private set; } = false;

    public Vector3 randomRotationMinEuler { get; private set; } = Vector3.zero;
    public Vector3 randomRotationMaxEuler { get; private set; } = Vector3.zero;
    public bool randomRotationPerComponent { get; private set; } = false;

    #endregion

    #region Constructor

    public PropagationBrushSettingsModel()
    {
        string brushMeshPath = AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHMESHGUID);
        string brushMaterialPath = AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHMATERIALGUID);

        brushMesh = EditorGUIUtility.Load(brushMeshPath) as Mesh;
        brushMaterial = EditorGUIUtility.Load(brushMaterialPath) as Material;
    }

    #endregion

    #region Setters

    public void SetInstanceCount(int count) => instanceCount = count;
    public void SetBrushSize(float size) => brushSize = size;
    public void SetBrushDensity(int density) => brushDensity = density;

    public void SetBrushMode(PropagationBrushWindow.BrushMode mode) => brushMode = mode;
    public void SetSampleMode(PropagationBrushWindow.PropagationMode mode) => sampleMode = mode;
    public void SetSelectedMeshIndex(int meshIndex) => selectedMeshIndex = meshIndex;
    public void SetCurrentBrushSet(BrushSetSO brushSetSO) => currentBrushSetSO = brushSetSO;

    public void SetCurrentBrushSO(int brushIndex)
    {
        selectedBrushSO = currentBrushSetSO.brushes[brushIndex];
        selectedBrushIndex = brushIndex;
    }

    // Static adjustments
    public void SetStaticPositionOffset(Vector3 offset) => staticPositionOffset = offset;
    public void SetStaticScale(Vector3 scale) => staticScale = scale;
    public void SetStaticRotation(Vector3 euler) => staticRotationEuler = euler;

    // Random adjustments
    public void SetRandomPositionOffset(Vector3 min, Vector3 max)
    {
        randomPositionOffsetMin = min;
        randomPositionOffsetMax = max;
    }
    public void SetRandomPositionOffsetPerComponent(bool perComponent) => randomPositionOffsetPerComponent = perComponent;

    public void SetRandomScale(Vector3 min, Vector3 max)
    {
        randomScaleMin = min;
        randomScaleMax = max;
    }
    public void SetRandomScalePerComponent(bool perComponent) => randomScalePerComponent = perComponent;

    public void SetRandomRotation(Vector3 min, Vector3 max)
    {
        randomRotationMinEuler = min;
        randomRotationMaxEuler = max;
    }
    public void SetRandomRotationPerComponent(bool perComponent) => randomRotationPerComponent = perComponent;

    #endregion
}

#endif
