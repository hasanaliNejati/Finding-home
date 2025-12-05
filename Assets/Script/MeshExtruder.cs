using UnityEngine;
using System.Collections.Generic;

public static class MeshExtruderUtility
{
    /// <summary>
    /// Extrudes a 2D mesh along the Z axis to create a 3D mesh
    /// </summary>
    /// <param name="sourceMesh">The flat 2D mesh to extrude</param>
    /// <param name="extrusionDepth">The depth/height of extrusion along Z axis</param>
    /// <returns>A new 3D mesh extruded along Z axis</returns>
    public static Mesh ExtrudeMeshAlongZ(Mesh sourceMesh, float extrusionDepth)
    {
        if (sourceMesh == null)
        {
            Debug.LogError("Source mesh is null!");
            return null;
        }

        Mesh extrudedMesh = new Mesh();
        
        Vector3[] sourceVertices = sourceMesh.vertices;
        int[] sourceTriangles = sourceMesh.triangles;
        Vector2[] sourceUVs = sourceMesh.uv;
        Vector3[] sourceNormals = sourceMesh.normals;

        // Calculate total vertices: original vertices + duplicated vertices for the back face
        int vertexCount = sourceVertices.Length;
        int totalVertices = vertexCount * 2; // Front face + Back face
        
        Vector3[] newVertices = new Vector3[totalVertices];
        Vector2[] newUVs = new Vector2[totalVertices];
        Vector3[] newNormals = new Vector3[totalVertices];
        
        // Copy front face vertices (at Z = 0)
        for (int i = 0; i < vertexCount; i++)
        {
            newVertices[i] = sourceVertices[i];
            newUVs[i] = sourceUVs != null && i < sourceUVs.Length ? sourceUVs[i] : Vector2.zero;
            newNormals[i] = sourceNormals != null && i < sourceNormals.Length ? sourceNormals[i] : Vector3.forward;
        }
        
        // Create back face vertices (at Z = extrusionDepth)
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 backVertex = sourceVertices[i];
            backVertex.z += extrusionDepth;
            newVertices[vertexCount + i] = backVertex;
            newUVs[vertexCount + i] = sourceUVs != null && i < sourceUVs.Length ? sourceUVs[i] : Vector2.zero;
            
            // Flip normal for back face
            Vector3 backNormal = sourceNormals != null && i < sourceNormals.Length ? sourceNormals[i] : Vector3.forward;
            backNormal = -backNormal;
            newNormals[vertexCount + i] = backNormal;
        }
        
        // Calculate total triangles
        // Front face triangles + Back face triangles (reversed) + Side faces (quads as 2 triangles each)
        int triangleCount = sourceTriangles.Length;
        int sideEdgeCount = GetEdgeCount(sourceVertices, sourceTriangles);
        int totalTriangles = (triangleCount * 2) + (sideEdgeCount * 6); // Front + Back + Sides
        
        int[] newTriangles = new int[totalTriangles];
        int triangleIndex = 0;
        
        // Front face triangles (same as source)
        for (int i = 0; i < triangleCount; i++)
        {
            newTriangles[triangleIndex++] = sourceTriangles[i];
        }
        
        // Back face triangles (reversed winding order)
        for (int i = 0; i < triangleCount; i += 3)
        {
            newTriangles[triangleIndex++] = sourceTriangles[i + 2] + vertexCount;
            newTriangles[triangleIndex++] = sourceTriangles[i + 1] + vertexCount;
            newTriangles[triangleIndex++] = sourceTriangles[i] + vertexCount;
        }
        
        // Side faces (connect front and back faces)
        List<Edge> edges = GetEdges(sourceVertices, sourceTriangles);
        foreach (Edge edge in edges)
        {
            int v0 = edge.v0;
            int v1 = edge.v1;
            int v2 = v0 + vertexCount; // Back face vertex corresponding to v0
            int v3 = v1 + vertexCount; // Back face vertex corresponding to v1
            
            // First triangle of the quad
            newTriangles[triangleIndex++] = v0;
            newTriangles[triangleIndex++] = v2;
            newTriangles[triangleIndex++] = v1;
            
            // Second triangle of the quad
            newTriangles[triangleIndex++] = v1;
            newTriangles[triangleIndex++] = v2;
            newTriangles[triangleIndex++] = v3;
        }
        
        extrudedMesh.vertices = newVertices;
        extrudedMesh.triangles = newTriangles;
        extrudedMesh.uv = newUVs;
        extrudedMesh.normals = newNormals;
        
        extrudedMesh.RecalculateBounds();
        extrudedMesh.RecalculateTangents();
        
        return extrudedMesh;
    }
    
    /// <summary>
    /// Gets all unique edges from the mesh
    /// </summary>
    private static List<Edge> GetEdges(Vector3[] vertices, int[] triangles)
    {
        Dictionary<Edge, bool> edgeMap = new Dictionary<Edge, bool>();
        List<Edge> edges = new List<Edge>();
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];
            
            AddEdge(edgeMap, edges, v0, v1);
            AddEdge(edgeMap, edges, v1, v2);
            AddEdge(edgeMap, edges, v2, v0);
        }
        
        return edges;
    }
    
    /// <summary>
    /// Adds an edge if it doesn't already exist (handles both directions)
    /// </summary>
    private static void AddEdge(Dictionary<Edge, bool> edgeMap, List<Edge> edges, int v0, int v1)
    {
        Edge edge1 = new Edge(v0, v1);
        Edge edge2 = new Edge(v1, v0);
        
        if (!edgeMap.ContainsKey(edge1) && !edgeMap.ContainsKey(edge2))
        {
            edgeMap[edge1] = true;
            edges.Add(edge1);
        }
    }
    
    /// <summary>
    /// Gets the count of unique edges
    /// </summary>
    private static int GetEdgeCount(Vector3[] vertices, int[] triangles)
    {
        return GetEdges(vertices, triangles).Count;
    }
    
    /// <summary>
    /// Helper class to represent an edge
    /// </summary>
    private class Edge
    {
        public int v0;
        public int v1;
        
        public Edge(int v0, int v1)
        {
            this.v0 = v0;
            this.v1 = v1;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return (v0 == other.v0 && v1 == other.v1);
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return v0.GetHashCode() ^ v1.GetHashCode();
        }
    }
    
    /// <summary>
    /// Creates a flat 2D circle mesh (example usage)
    /// </summary>
    public static Mesh CreateCircleMesh(float radius, int segments)
    {
        Mesh circleMesh = new Mesh();
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        
        // Center vertex
        vertices.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));
        normals.Add(Vector3.back); // Normal pointing along -Z
        
        // Circle vertices
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            
            vertices.Add(new Vector3(x, y, 0));
            uvs.Add(new Vector2(0.5f + x / (radius * 2), 0.5f + y / (radius * 2)));
            normals.Add(Vector3.back);
        }
        
        // Create triangles
        for (int i = 1; i <= segments; i++)
        {
            triangles.Add(0); // Center
            triangles.Add(i);
            triangles.Add(i + 1);
        }
        
        circleMesh.vertices = vertices.ToArray();
        circleMesh.triangles = triangles.ToArray();
        circleMesh.uv = uvs.ToArray();
        circleMesh.normals = normals.ToArray();
        
        circleMesh.RecalculateBounds();
        
        return circleMesh;
    }
    
    /// <summary>
    /// Example: Create a cylinder from a circle
    /// </summary>
    public static Mesh CreateCylinderMesh(float radius, float depth, int segments)
    {
        Mesh circleMesh = CreateCircleMesh(radius, segments);
        return ExtrudeMeshAlongZ(circleMesh, depth);
    }
}
