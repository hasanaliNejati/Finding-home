using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshExtruderComponent : MonoBehaviour
{
    [Header("Input Mesh")]
    [Tooltip("The flat 2D mesh to extrude along Z axis")]
    public Mesh sourceMesh;
    
    [Header("Extrusion Settings")]
    [Tooltip("Depth of extrusion along Z axis")]
    public float extrusionDepth = 2f;
    
    [Header("Auto Generate Circle (Optional)")]
    [Tooltip("If enabled, will create a circle mesh instead of using sourceMesh")]
    public bool useCircleMesh = false;
    
    [Tooltip("Radius of the circle (if useCircleMesh is enabled)")]
    public float circleRadius = 1f;
    
    [Tooltip("Number of segments for the circle (if useCircleMesh is enabled)")]
    [Range(8, 64)]
    public int circleSegments = 32;
    
    [Header("Export Settings")]
    [Tooltip("Path to save the mesh asset (relative to Assets folder)")]
    public string exportPath = "Meshes/ExtrudedMesh";
    
    [Tooltip("Name of the exported mesh file")]
    public string exportFileName = "ExtrudedMesh";
    
    private MeshFilter meshFilter;
    private Mesh originalMesh;
    private Mesh currentExtrudedMesh;
    
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        
        // Save original mesh
        if (meshFilter.sharedMesh != null)
        {
            originalMesh = meshFilter.sharedMesh;
        }
    }
    
    [ContextMenu("Extrude Mesh")]
    public void ExtrudeMesh()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        
        Mesh meshToExtrude = null;
        
        // Use circle mesh if enabled
        if (useCircleMesh)
        {
            meshToExtrude = MeshExtruderUtility.CreateCircleMesh(circleRadius, circleSegments);
            Debug.Log($"[MeshExtruder] Created circle mesh with radius {circleRadius} and {circleSegments} segments");
        }
        // Use source mesh if provided
        else if (sourceMesh != null)
        {
            meshToExtrude = sourceMesh;
            Debug.Log($"[MeshExtruder] Using provided source mesh with {sourceMesh.vertexCount} vertices");
        }
        // Use current mesh if available
        else if (meshFilter.sharedMesh != null)
        {
            meshToExtrude = meshFilter.sharedMesh;
            Debug.Log($"[MeshExtruder] Using current mesh with {meshToExtrude.vertexCount} vertices");
        }
        else
        {
            Debug.LogError("[MeshExtruder] No mesh found! Please assign a sourceMesh or enable useCircleMesh");
            return;
        }
        
        if (meshToExtrude == null)
        {
            Debug.LogError("[MeshExtruder] Failed to get mesh for extrusion!");
            return;
        }
        
        // Extrude the mesh
        Mesh extrudedMesh = MeshExtruderUtility.ExtrudeMeshAlongZ(meshToExtrude, extrusionDepth);
        
        if (extrudedMesh != null)
        {
            meshFilter.mesh = extrudedMesh;
            currentExtrudedMesh = extrudedMesh;
            Debug.Log($"[MeshExtruder] Successfully extruded mesh! New vertex count: {extrudedMesh.vertexCount}, Depth: {extrusionDepth}");
        }
        else
        {
            Debug.LogError("[MeshExtruder] Failed to extrude mesh!");
        }
    }
    
    [ContextMenu("Reset to Original Mesh")]
    public void ResetToOriginalMesh()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        
        if (originalMesh != null)
        {
            meshFilter.mesh = originalMesh;
            Debug.Log("[MeshExtruder] Reset to original mesh");
        }
        else
        {
            Debug.LogWarning("[MeshExtruder] No original mesh saved to reset to");
        }
    }
    
    [ContextMenu("Save Extruded Mesh as Asset")]
    public void SaveMeshAsAsset()
    {
#if UNITY_EDITOR
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        
        Mesh meshToSave = currentExtrudedMesh;
        if (meshToSave == null && meshFilter.sharedMesh != null)
        {
            meshToSave = meshFilter.sharedMesh;
        }
        
        if (meshToSave == null)
        {
            Debug.LogError("[MeshExtruder] No extruded mesh to save! Please extrude a mesh first.");
            return;
        }
        
        // Create a copy of the mesh to save as asset
        Mesh meshCopy = new Mesh();
        meshCopy.vertices = meshToSave.vertices;
        meshCopy.triangles = meshToSave.triangles;
        meshCopy.uv = meshToSave.uv;
        meshCopy.normals = meshToSave.normals;
        meshCopy.tangents = meshToSave.tangents;
        meshCopy.colors = meshToSave.colors;
        meshCopy.name = string.IsNullOrEmpty(exportFileName) ? "ExtrudedMesh" : exportFileName;
        
        // Ensure export path is valid
        string path = exportPath;
        if (string.IsNullOrEmpty(path))
        {
            path = "Meshes";
        }
        
        // Remove leading/trailing slashes and ensure it starts with Assets/
        path = path.TrimStart('/', '\\');
        path = path.TrimEnd('/', '\\');
        if (!path.StartsWith("Assets/"))
        {
            path = "Assets/" + path;
        }
        
        // Create directory if it doesn't exist
        string directoryPath = path;
        if (!AssetDatabase.IsValidFolder(directoryPath))
        {
            string[] folders = directoryPath.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
        
        // Save mesh asset
        string assetPath = path + "/" + meshCopy.name + ".asset";
        AssetDatabase.CreateAsset(meshCopy, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[MeshExtruder] Mesh saved successfully at: {assetPath}");
        
        // Select the asset in the project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = meshCopy;
#else
        Debug.LogWarning("[MeshExtruder] Save mesh feature is only available in Editor mode!");
#endif
    }
    
    void OnValidate()
    {
        // Ensure extrusion depth is positive
        if (extrusionDepth < 0)
        {
            extrusionDepth = 0;
        }
        
        // Ensure circle radius is positive
        if (circleRadius < 0)
        {
            circleRadius = 0;
        }
    }
}
