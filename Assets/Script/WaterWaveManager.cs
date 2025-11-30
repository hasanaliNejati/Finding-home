using System.Collections.Generic;
using UnityEngine;

public class WaterWaveManager : MonoBehaviour
{
    [Header("Water Object Settings")]
    [SerializeField] private List<GameObject> waterObjectPrefabs = new List<GameObject>();
    [SerializeField] private int waterObjectCount = 10;
    [SerializeField] private float spacing = 1f;
    [SerializeField] private Vector3 startPosition = Vector3.zero;
    [SerializeField] private Vector3 direction = Vector3.right;
    
    [Header("Wave Movement Settings")]
    [SerializeField] private float waveSpeed = 2f;
    [SerializeField] private float waveAmplitude = 0.5f;
    [SerializeField] private float phaseOffset = 0.5f;
    
    [Header("Parent Transform")]
    [SerializeField] private Transform waterParent;
    
    private List<WaterWaveObject> waterObjects = new List<WaterWaveObject>();
    
    private void Start()
    {
        CreateWaterObjects();
    }
    
    private void CreateWaterObjects()
    {
        if (waterObjectPrefabs == null || waterObjectPrefabs.Count == 0)
        {
            Debug.LogError("Water Object Prefabs list is empty or not assigned!");
            return;
        }
        
        // اگر parent تعیین نشده، خودش رو parent کنه
        if (waterParent == null)
        {
            waterParent = transform;
        }
        
        // پاک کردن آبجکت‌های قبلی
        ClearWaterObjects();
        
        // ساخت آبجکت‌های جدید
        for (int i = 0; i < waterObjectCount; i++)
        {
            // انتخاب prefab به صورت چرخه‌ای (اولی، دومی، سومی، دوباره اولی، ...)
            int prefabIndex = i % waterObjectPrefabs.Count;
            GameObject selectedPrefab = waterObjectPrefabs[prefabIndex];
            
            if (selectedPrefab == null)
            {
                Debug.LogWarning($"Water Object Prefab at index {prefabIndex} is null! Skipping...");
                continue;
            }
            
            GameObject waterObj = Instantiate(selectedPrefab, waterParent);
            waterObj.name = $"WaterObject_{i}_Type_{prefabIndex}";
            
            // محاسبه موقعیت اولیه با offset
            Vector3 position = startPosition + direction.normalized * (spacing * i);
            waterObj.transform.position = position;
            
            // اضافه کردن کامپوننت WaterWaveObject
            WaterWaveObject waveObject = waterObj.GetComponent<WaterWaveObject>();
            if (waveObject == null)
            {
                waveObject = waterObj.AddComponent<WaterWaveObject>();
            }
            
            // تنظیمات حرکت موج
            waveObject.Initialize(
                position,
                waveSpeed,
                waveAmplitude,
                phaseOffset * i // هر آبجکت یک phase offset متفاوت داره
            );
            
            // گرفتن مقدار اولیه sorting order از prefab انتخاب شده
            int initialSortingOrder = 0;
            SpriteRenderer prefabRenderer = selectedPrefab.GetComponent<SpriteRenderer>();
            if (prefabRenderer != null)
            {
                initialSortingOrder = prefabRenderer.sortingOrder;
            }
            
            // تنظیم Sorting Order برای SpriteRenderer (از مقدار اولیه کم می‌کنیم)
            SpriteRenderer spriteRenderer = waterObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = initialSortingOrder - i; // از مقدار اولیه کم می‌کنیم
            }
            
            waterObjects.Add(waveObject);
        }
    }
    
    private void ClearWaterObjects()
    {
        foreach (var waterObj in waterObjects)
        {
            if (waterObj != null)
            {
                DestroyImmediate(waterObj.gameObject);
            }
        }
        waterObjects.Clear();
    }
    
    // برای تغییر تنظیمات در runtime
    public void UpdateWaveSettings(float speed, float amplitude, float offset)
    {
        waveSpeed = speed;
        waveAmplitude = amplitude;
        phaseOffset = offset;
        
        foreach (var waterObj in waterObjects)
        {
            if (waterObj != null)
            {
                waterObj.UpdateSettings(waveSpeed, waveAmplitude, waterObj.PhaseOffset);
            }
        }
    }
    
    // برای ساخت مجدد آبجکت‌ها
    [ContextMenu("Recreate Water Objects")]
    public void RecreateWaterObjects()
    {
        CreateWaterObjects();
    }
    
    private void OnValidate()
    {
        // فقط در edit mode و اگر آبجکت‌ها ساخته شدن، دوباره بساز
        if (Application.isPlaying && waterObjects.Count > 0)
        {
            CreateWaterObjects();
        }
    }
}

