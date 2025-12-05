using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class SpriteToMeshComponent : MonoBehaviour
{
    public enum MeshType
    {
        Complex,    // شکل پیچیده
        Simple,     // مستطیل ساده
        Column      // ستونی
    }

    [Header("Sprite Input")]
    [Tooltip("اسپرایتی که می‌خواهید به مش تبدیل شود")]
    public Sprite sprite;

    [Header("Mesh Settings")]
    [Tooltip("نوع مش: Complex (شکل پیچیده), Simple (مستطیل), Column (ستونی)")]
    public MeshType meshType = MeshType.Complex;

    [Header("Column Settings")]
    [Tooltip("ارتفاع ستون (فقط برای نوع Column)")]
    [Range(0.1f, 10f)]
    public float columnHeight = 1f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (sprite != null)
        {
            GenerateMesh();
        }
    }

    private void InitializeComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    /// <summary>
    /// مش را از اسپرایت می‌سازد
    /// </summary>
    [ContextMenu("Generate Mesh")]
    public void GenerateMesh()
    {
        InitializeComponents();

        if (sprite == null)
        {
            Debug.LogWarning("Sprite is not assigned!");
            return;
        }

        if (meshFilter == null || meshRenderer == null)
        {
            Debug.LogError("MeshFilter or MeshRenderer component is missing!");
            return;
        }

        Mesh mesh;
        switch (meshType)
        {
            case MeshType.Complex:
                mesh = SpriteToMeshGenerator.CreateMeshFromSprite(sprite);
                break;
            case MeshType.Simple:
                mesh = SpriteToMeshGenerator.CreateSimpleMeshFromSprite(sprite);
                break;
            case MeshType.Column:
                mesh = SpriteToMeshGenerator.CreateColumnMeshFromSprite(sprite, columnHeight);
                break;
            default:
                mesh = SpriteToMeshGenerator.CreateMeshFromSprite(sprite);
                break;
        }

        if (mesh != null)
        {
            meshFilter.sharedMesh = mesh;
            
            // اگر متریال تنظیم نشده، از متریال پیش‌فرض استفاده کن
            if (meshRenderer.sharedMaterial == null)
            {
                Material defaultMaterial = new Material(Shader.Find("Sprites/Default"));
                if (defaultMaterial.shader.name != "Sprites/Default")
                {
                    defaultMaterial = new Material(Shader.Find("Standard"));
                }
                defaultMaterial.mainTexture = sprite.texture;
                meshRenderer.sharedMaterial = defaultMaterial;
            }
            else if (meshRenderer.sharedMaterial.mainTexture == null)
            {
                // اگر متریال texture ندارد، texture اسپرایت را اضافه کن
                Material mat = new Material(meshRenderer.sharedMaterial);
                mat.mainTexture = sprite.texture;
                meshRenderer.sharedMaterial = mat;
            }
        }
    }

    private void OnValidate()
    {
        InitializeComponents();
        
        if (sprite != null && meshFilter != null)
        {
            // در Edit Mode، مش را دوباره بساز
            if (!Application.isPlaying)
            {
                GenerateMesh();
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// مش را به عنوان Asset ذخیره می‌کند
    /// </summary>
    [ContextMenu("Save Mesh as Asset")]
    public void SaveMeshAsAsset()
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("No mesh to save! Generate mesh first.");
            return;
        }

        string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Mesh",
            sprite != null ? sprite.name + "_Mesh" : "NewMesh",
            "asset",
            "Choose where to save the mesh asset");

        if (string.IsNullOrEmpty(path))
            return;

        // کپی کردن mesh برای ذخیره
        Mesh meshToSave = Object.Instantiate(meshFilter.sharedMesh);
        meshToSave.name = System.IO.Path.GetFileNameWithoutExtension(path);

        UnityEditor.AssetDatabase.CreateAsset(meshToSave, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"Mesh saved to: {path}");
    }
#endif
}

