using UnityEngine;

[ExecuteInEditMode]
public class WaterController : MonoBehaviour
{
    [Header("Water Material")]
    [SerializeField] private Material waterMaterial;
    
    [Header("Color Settings")]
    [SerializeField] private Color shallowColor = new Color(0.3f, 0.7f, 0.9f, 0.8f);
    [SerializeField] private Color deepColor = new Color(0.1f, 0.3f, 0.6f, 1.0f);
    [SerializeField] private float depthDistance = 1.0f;
    
    [Header("Foam Settings")]
    [SerializeField] private Color foamColor = Color.white;
    [SerializeField] private float foamDistance = 0.4f;
    [SerializeField] private float foamNoiseScale = 10.0f;
    [SerializeField] private float foamNoiseSpeed = 0.5f;
    [SerializeField] private float foamIntensity = 2.0f;
    
    [Header("Wave Settings")]
    [SerializeField] private float waveSpeed = 0.5f;
    [SerializeField] private float waveScale = 0.5f;
    [SerializeField] private float waveHeight = 0.1f;
    [SerializeField] private float waveFrequency = 1.0f;
    
    [Header("Surface")]
    [SerializeField] private float smoothness = 0.9f;
    [SerializeField] private float normalScale = 0.5f;

    private void Start()
    {
        if (waterMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                waterMaterial = renderer.sharedMaterial;
            }
        }
        
        UpdateMaterialProperties();
    }

    private void Update()
    {
        UpdateMaterialProperties();
    }

    private void UpdateMaterialProperties()
    {
        if (waterMaterial == null) return;

        // Color
        waterMaterial.SetColor("_ShallowColor", shallowColor);
        waterMaterial.SetColor("_DeepColor", deepColor);
        waterMaterial.SetFloat("_DepthDistance", depthDistance);
        
        // Foam
        waterMaterial.SetColor("_FoamColor", foamColor);
        waterMaterial.SetFloat("_FoamDistance", foamDistance);
        waterMaterial.SetFloat("_FoamNoiseScale", foamNoiseScale);
        waterMaterial.SetFloat("_FoamNoiseSpeed", foamNoiseSpeed);
        waterMaterial.SetFloat("_FoamIntensity", foamIntensity);
        
        // Waves
        waterMaterial.SetFloat("_WaveSpeed", waveSpeed);
        waterMaterial.SetFloat("_WaveScale", waveScale);
        waterMaterial.SetFloat("_WaveHeight", waveHeight);
        waterMaterial.SetFloat("_WaveFrequency", waveFrequency);
        
        // Surface
        waterMaterial.SetFloat("_Smoothness", smoothness);
        waterMaterial.SetFloat("_NormalScale", normalScale);
    }

    private void OnValidate()
    {
        UpdateMaterialProperties();
    }
}

