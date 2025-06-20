using UnityEngine;
using System.Collections.Generic;
using System;

namespace PropagationSystem
{
    [Serializable]
    public struct TransformData
    {
        public List<SavedPositions> instanceDatas;  //Transform , Rotation , Scale
         public int meshIndex; // Added

        public TransformData(bool init = true)
        {
            instanceDatas = new List<SavedPositions>();
            meshIndex = -1;
        }
    }

    public struct TransformTransferData
    {
        public Matrix4x4 trsMatrices;  //Transform , Rotation , Scale
    }


    [Serializable]
    public struct SavedPositions
    {
      [SerializeReference]  public Vector3 position;
      [SerializeReference]  public Quaternion rotation;
      [SerializeReference]  public Vector3 scale;
    }
}