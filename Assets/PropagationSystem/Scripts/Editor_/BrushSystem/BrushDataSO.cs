// Assets/PropagationSystem/BrushDataSO.cs

using UnityEngine;

namespace PropagationSystem
{
    [CreateAssetMenu(fileName = "BrushData", menuName = "PropagationSystem/BrushData", order = 1)]
    public class BrushDataSO : ScriptableObject
    {
        [Tooltip("Mask veya �rnekleme i�in kullan�lacak texture")]
        public Texture2D maskTexture;
        public bool Invert;
        public string brushName = "Default Brush";
        // �leride ek parametre eklemek istedi�inde buraya ekleyebilirsin:
        // public float brushSize = 1f;
        // public float strength = 1f;
    }



  
}
