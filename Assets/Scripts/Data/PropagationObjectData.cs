using UnityEngine;
using System.Collections.Generic;



namespace PropagationSystem
{
    [System.Serializable]
    public struct TransformData
    {
        public List<Matrix4x4> trsMatrices;  //Transform , Rotation , Scale
        public int meshIndex; // Added
    }


    public struct TransformTransferData
    {
        public Matrix4x4 trsMatrices;  //Transform , Rotation , Scale
    }

}