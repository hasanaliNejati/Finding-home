using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.View
{
    /// <summary>
    /// Holds all serialized data for CardView to prevent data loss when swapping view components in prefab variants
    /// Works with World Space Canvas for proper Z-ordering
    /// </summary>
    public class CardViewData : MonoBehaviour
    {
        [Header("Canvas Reference")]
        [Tooltip("The World Space Canvas that contains all UI elements")]
        [SerializeField] public Canvas worldCanvas;
        
        [Header("Visual References")]
        [SerializeField] public Image spriteRenderer;
        [SerializeField] public TextMeshProUGUI nameText;
        [SerializeField] public TextMeshProUGUI valueText;
        
        [Header("Progress")]
        [SerializeField] public Slider progressSlider;
        
        [Header("Settings")]
        [SerializeField] public float speedLerp = 10f;
        
        [Header("Stacking")]
        // Offset for stacked cards in top-down view (on X-Z plane)
        [SerializeField] public Vector3 groupOffset = new Vector3(0, 0f, -0.1f);
        
        [Header("Canvas Sorting")]
        [Tooltip("Base sorting order for the canvas")]
        [SerializeField] public int baseSortingOrder = 0;
        [Tooltip("Multiplier for Z position to sorting order conversion")]
        [SerializeField] public float sortingOrderMultiplier = 100f;
    }
}

