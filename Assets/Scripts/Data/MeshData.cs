using System;
using UnityEngine;
using PropagationSystem;

namespace PropagationSystem
{

    [Serializable]
    public struct MeshData
    {
        public Mesh mesh;
        public Material material;
        public string name;
        [HideInInspector]
        public int meshIndex;// Added
        public bool useFrustumCulling; // Added to indicate if frustum culling is used for this mesh

    }
}