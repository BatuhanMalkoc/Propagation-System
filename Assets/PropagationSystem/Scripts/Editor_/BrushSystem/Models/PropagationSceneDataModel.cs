using PropagationSystem;
using PropagationSystem.Editor;
using UnityEditor;
using UnityEngine;


#if UNITY_EDITOR
public class PropagationSceneDataModel 
{

    private SceneData sceneData;


    public void SetSceneData(SceneData data)
    {
        sceneData = data;
    }

    public SceneData GetSceneData() { return sceneData; }


    public void AddNewMeshDefinition(MeshData createdMeshData)
    {
        sceneData.propagatedMeshDefinitions.Add(createdMeshData);

        EditorPreviewer.SetPreviewMode(false);

        EditorUtility.SetDirty(sceneData);
        sceneData.OnValidateExternalCall();
    }
    public void RemoveMeshDefinition(int meshIndex)
    {
        sceneData.propagatedMeshDefinitions.RemoveAt(meshIndex);

        sceneData.OnValidateExternalCall();
        EditorUtility.SetDirty(sceneData);
    }

    public void AddInstance(int index,SavedPositions transform)
    {
        if(sceneData.propagatedObjectDatas.Count <= index)
        {
            Debug.LogError("Mesh index out of range: " + index);
            return;
        }

        sceneData.propagatedObjectDatas[index].instanceDatas.Add(transform);
        sceneData.OnValidateExternalCall();
        EditorUtility.SetDirty(sceneData);
    }
    public void RemoveInstance(int meshIndex, int instanceIndex)
    {
        if (sceneData.propagatedObjectDatas.Count <= meshIndex)
        {
            Debug.LogError("Mesh index out of range: " + meshIndex);
            return;
        }
        if (sceneData.propagatedObjectDatas[meshIndex].instanceDatas.Count <= instanceIndex)
        {
            Debug.LogError("Instance index out of range: " + instanceIndex);
            return;
        }
        sceneData.propagatedObjectDatas[meshIndex].instanceDatas.RemoveAt(instanceIndex);
        sceneData.OnValidateExternalCall();
        EditorUtility.SetDirty(sceneData);
    }

    public void AddInstances(int index , SavedPositions[] transforms)
    {
        if (sceneData.propagatedObjectDatas.Count <= index)
        {
            Debug.LogError("Mesh index out of range: " + index);
            return;
        }
        sceneData.propagatedObjectDatas[index].instanceDatas.AddRange(transforms);
        sceneData.OnValidateExternalCall();
        EditorUtility.SetDirty(sceneData);
    }

    public void AddStrokeInstances(int index, StrokeData stroke)
    {
        
     
        sceneData.OnValidateExternalCall();
        EditorUtility.SetDirty(sceneData);
    }

}
#endif