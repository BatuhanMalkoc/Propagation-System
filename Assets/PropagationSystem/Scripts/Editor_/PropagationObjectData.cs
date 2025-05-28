using UnityEngine;
using System.Collections.Generic;
using System;

namespace PropagationSystem
{
    [Serializable]
    public struct TransformData
    {
        public List<SavedPositions> trsMatrices;  //Transform , Rotation , Scale
         public int meshIndex; // Added
    }

    public struct TransformTransferData
    {
        public Matrix4x4 trsMatrices;  //Transform , Rotation , Scale
    }


    [Serializable]
    public struct SavedPositions
    {
      [SerializeReference]  public Vector3 position;
      [SerializeReference] public Quaternion rotation;
      [SerializeReference]  public Vector3 scale;
    }
}