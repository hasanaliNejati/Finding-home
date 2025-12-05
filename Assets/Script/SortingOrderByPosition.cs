using Script;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

/// <summary>
/// Component that automatically updates sorting order based on Z position
/// Works with Canvas, SpriteRenderer, and SortingGroup
/// </summary>
public class SortingOrderByPosition : MonoBehaviour
{
    [Header("Sorting Settings")]
    [Tooltip("Base sorting order (default: 0)")]
    [SerializeField] private int baseSortingOrder = 0;
    
    [Tooltip("Multiplier for Z position to sorting order conversion (default: 100)")]
    [SerializeField] private float sortingOrderMultiplier = SortingOrderUtility.DefaultSortingOrderMultiplier;
    
    [Header("Update Settings")]
    [Tooltip("Update sorting order every frame (recommended for moving objects)")]
    [SerializeField] private bool updateEveryFrame = true;
    
    [Tooltip("Update only when position changes (more efficient for static objects)")]
    [SerializeField] private bool updateOnPositionChange = false;
    
    private Canvas canvas;
    private SpriteRenderer spriteRenderer;
    private SortingGroup sortingGroup;
    private Vector3 lastPosition;
    private bool hasInitialized = false;
    
    private void Awake()
    {
        // Try to get Canvas component
        canvas = GetComponent<Canvas>();
        
        // Try to get SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Try to get SortingGroup component
        sortingGroup = GetComponent<SortingGroup>();
        
        // If components don't exist, try to find in children
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>();
        }
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (sortingGroup == null)
        {
            sortingGroup = GetComponentInChildren<SortingGroup>();
        }
        
        // Warn if no component found
        if (canvas == null && spriteRenderer == null && sortingGroup == null)
        {
            Debug.LogWarning($"[SortingOrderByPosition] No Canvas, SpriteRenderer, or SortingGroup found on {gameObject.name} or its children!");
        }
    }
    
    private void Start()
    {
        // Initialize sorting order
        UpdateSortingOrder();
        lastPosition = transform.position;
        hasInitialized = true;
    }
    
    private void Update()
    {
        if (!hasInitialized) return;
        
        if (updateEveryFrame)
        {
            UpdateSortingOrder();
        }
        else if (updateOnPositionChange)
        {
            // Check if position changed
            if (Vector3.Distance(transform.position, lastPosition) > 0.001f)
            {
                UpdateSortingOrder();
                lastPosition = transform.position;
            }
        }
    }
    
    /// <summary>
    /// Updates sorting order based on current Z position
    /// </summary>
    public void UpdateSortingOrder()
    {
        int sortingOrder = SortingOrderUtility.GetSortingOrderFromZ(
            transform.position.z,
            baseSortingOrder,
            sortingOrderMultiplier
        );
        
        // Update Canvas sorting order
        if (canvas != null)
        {
            canvas.sortingOrder = sortingOrder;
        }
        
        // Update SpriteRenderer sorting order
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
        
        // Update SortingGroup sorting order
        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = sortingOrder;
        }
    }
    
    /// <summary>
    /// Manually update sorting order (useful for external calls)
    /// </summary>
    [ContextMenu("Update Sorting Order")]
    public void ManualUpdate()
    {
        UpdateSortingOrder();
    }
    
    /// <summary>
    /// Set base sorting order
    /// </summary>
    public void SetBaseSortingOrder(int newBaseSortingOrder)
    {
        baseSortingOrder = newBaseSortingOrder;
        UpdateSortingOrder();
    }
    
    /// <summary>
    /// Set sorting order multiplier
    /// </summary>
    public void SetSortingOrderMultiplier(float newMultiplier)
    {
        sortingOrderMultiplier = newMultiplier;
        UpdateSortingOrder();
    }
}

