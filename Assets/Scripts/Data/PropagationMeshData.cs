using System;
using Unity.Collections;
using UnityEngine;

[Serializable]
public struct PropagationMeshData 
{
    public Mesh mesh;
    public Material material;
    public string name;
    [HideInInspector]
    public int meshIndex;// Added

}
