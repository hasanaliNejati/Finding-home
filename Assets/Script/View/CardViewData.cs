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
        
        [Header("Animation")]
        [Tooltip("Reference to Visual card GameObject (child of Canvas) for jump animation")]
        [SerializeField] public Transform visualCard;
        [Tooltip("Duration of linear movement in X and Z axes")]
        [SerializeField] public float moveDuration = 0.5f;
        [Tooltip("Height of the first jump animation")]
        [SerializeField] public float jumpHeight = 1f;
        [Tooltip("Duration of the first jump animation")]
        [SerializeField] public float jumpDuration = 0.6f;
        [Tooltip("Number of bounce iterations")]
        [SerializeField] public int bounceCount = 3;
        [Tooltip("Height reduction factor for each bounce (0.5 = half, 0.7 = 70% of previous)")]
        [SerializeField] [Range(0.1f, 1f)] public float heightReduction = 0.5f;
        [Tooltip("Duration reduction factor for each bounce (0.5 = half, 0.7 = 70% of previous)")]
        [SerializeField] [Range(0.1f, 1f)] public float durationReduction = 0.6f;
        [Tooltip("Vibrato (number of bounces) for each jump")]
        [SerializeField] [Range(1, 10)] public int vibrato = 2;
        [Tooltip("Elasticity (bounce strength) for each jump")]
        [SerializeField] [Range(0f, 2f)] public float elasticity = 1f;
        
        [Header("Drag Animation")]
        [Tooltip("Height of VisualCard when dragging")]
        [SerializeField] public float dragHeight = 0.3f;
        [Tooltip("Duration of height change when drag starts")]
        [SerializeField] public float dragHeightDuration = 0.2f;
        [Tooltip("Duration of height change when drag ends (usually faster)")]
        [SerializeField] public float dragHeightDownDuration = 0.1f;
        [Tooltip("Maximum rotation angle in degrees when dragging")]
        [SerializeField] public float maxDragRotationAngle = 25f;
        [Tooltip("Distance threshold below which rotation becomes 0")]
        [SerializeField] public float rotationZeroThreshold = 0.5f;
        [Tooltip("Distance at which rotation reaches maximum (should be > rotationZeroThreshold)")]
        [SerializeField] public float rotationMaxDistance = 2f;
    }
}

