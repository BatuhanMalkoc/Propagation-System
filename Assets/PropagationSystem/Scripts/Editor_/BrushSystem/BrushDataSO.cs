// Assets/PropagationSystem/BrushDataSO.cs

using UnityEngine;

namespace PropagationSystem
{
    [CreateAssetMenu(fileName = "BrushData", menuName = "PropagationSystem/BrushData", order = 1)]
    public class BrushDataSO : ScriptableObject
    {
        [Tooltip("Mask veya örnekleme için kullanýlacak texture")]
        public Texture2D maskTexture;
        public bool Invert;
        public string brushName = "Default Brush";
        // Ýleride ek parametre eklemek istediðinde buraya ekleyebilirsin:
        // public float brushSize = 1f;
        // public float strength = 1f;
    }



  
}
