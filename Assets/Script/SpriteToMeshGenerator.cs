using System.Collections.Generic;
using UnityEngine;

public static class SpriteToMeshGenerator
{
    /// <summary>
    /// یک مش از اسپرایت می‌سازد
    /// </summary>
    /// <param name="sprite">اسپرایت ورودی</param>
    /// <returns>مش ساخته شده از اسپرایت</returns>
    public static Mesh CreateMeshFromSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("Sprite is null!");
            return null;
        }

        Mesh mesh = new Mesh();
        mesh.name = sprite.name + "_Mesh";

        // گرفتن vertices و triangles از sprite
        Vector3[] vertices = new Vector3[sprite.vertices.Length];
        Vector2[] uvs = new Vector2[sprite.uv.Length];
        ushort[] triangles = sprite.triangles;

        // تبدیل vertices از فضای sprite به فضای world
        for (int i = 0; i < sprite.vertices.Length; i++)
        {
            vertices[i] = sprite.vertices[i];
            uvs[i] = sprite.uv[i];
        }

        // تبدیل ushort triangles به int triangles
        int[] trianglesInt = new int[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            trianglesInt[i] = triangles[i];
        }

        // تنظیم داده‌های مش
        mesh.vertices = vertices;
        mesh.triangles = trianglesInt;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// یک مش ساده (مستطیل) از اسپرایت می‌سازد (بدون در نظر گیری شکل پیچیده)
    /// </summary>
    /// <param name="sprite">اسپرایت ورودی</param>
    /// <returns>مش ساده ساخته شده</returns>
    public static Mesh CreateSimpleMeshFromSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("Sprite is null!");
            return null;
        }

        Mesh mesh = new Mesh();
        mesh.name = sprite.name + "_SimpleMesh";

        // گرفتن rect و pivot از sprite
        Rect rect = sprite.rect;
        Vector2 pivot = sprite.pivot;
        Vector2 size = sprite.bounds.size;

        // محاسبه offset برای pivot
        float pivotX = (pivot.x / rect.width) - 0.5f;
        float pivotY = (pivot.y / rect.height) - 0.5f;

        // ساخت vertices برای یک مستطیل ساده
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-size.x * 0.5f - pivotX * size.x, -size.y * 0.5f - pivotY * size.y, 0);
        vertices[1] = new Vector3(size.x * 0.5f - pivotX * size.x, -size.y * 0.5f - pivotY * size.y, 0);
        vertices[2] = new Vector3(-size.x * 0.5f - pivotX * size.x, size.y * 0.5f - pivotY * size.y, 0);
        vertices[3] = new Vector3(size.x * 0.5f - pivotX * size.x, size.y * 0.5f - pivotY * size.y, 0);

        // ساخت triangles (دو مثلث برای مستطیل)
        int[] triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;

        // ساخت UVs
        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(0, 1);
        uvs[3] = new Vector2(1, 1);

        // تنظیم داده‌های مش
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// یک مش ستونی (extruded) از اسپرایت می‌سازد
    /// </summary>
    /// <param name="sprite">اسپرایت ورودی</param>
    /// <param name="height">ارتفاع ستون</param>
    /// <returns>مش ستونی ساخته شده</returns>
    public static Mesh CreateColumnMeshFromSprite(Sprite sprite, float height = 1f)
    {
        if (sprite == null)
        {
            Debug.LogError("Sprite is null!");
            return null;
        }

        Mesh mesh = new Mesh();
        mesh.name = sprite.name + "_ColumnMesh";

        // بررسی اینکه آیا sprite vertices دارد یا نه
        Vector2[] spriteVertices2D = sprite.vertices;
        ushort[] baseTriangles = sprite.triangles;
        Vector2[] baseUVs = sprite.uv;

        // اگر sprite vertices ندارد، از bounds استفاده کن (مستطیل ساده)
        if (spriteVertices2D == null || spriteVertices2D.Length == 0 || baseTriangles == null || baseTriangles.Length == 0)
        {
            Debug.LogWarning($"Sprite '{sprite.name}' has no custom vertices. Using simple rectangle shape for column.");
            return CreateColumnMeshFromSimpleShape(sprite, height);
        }

        Vector3[] baseVertices = new Vector3[spriteVertices2D.Length];
        for (int i = 0; i < spriteVertices2D.Length; i++)
        {
            baseVertices[i] = new Vector3(spriteVertices2D[i].x, 0, spriteVertices2D[i].y);
        }

        int baseVertexCount = baseVertices.Length;
        int baseTriangleCount = baseTriangles.Length / 3;

        // ساخت vertices: پایه + بالا + سطوح جانبی
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        // 1. اضافه کردن vertices پایه (y = 0)
        for (int i = 0; i < baseVertexCount; i++)
        {
            vertices.Add(new Vector3(baseVertices[i].x, 0, baseVertices[i].y));
            uvs.Add(baseUVs[i]);
            normals.Add(Vector3.down);
        }

        // 2. اضافه کردن vertices بالا (y = height)
        for (int i = 0; i < baseVertexCount; i++)
        {
            vertices.Add(new Vector3(baseVertices[i].x, height, baseVertices[i].y));
            uvs.Add(baseUVs[i]);
            normals.Add(Vector3.up);
        }

        // 3. اضافه کردن triangles برای پایه (معکوس برای دیده شدن از پایین)
        for (int i = 0; i < baseTriangleCount; i++)
        {
            int idx0 = baseTriangles[i * 3];
            int idx1 = baseTriangles[i * 3 + 1];
            int idx2 = baseTriangles[i * 3 + 2];
            
            triangles.Add(idx2);
            triangles.Add(idx1);
            triangles.Add(idx0);
        }

        // 4. اضافه کردن triangles برای بالا
        int topOffset = baseVertexCount;
        for (int i = 0; i < baseTriangleCount; i++)
        {
            int idx0 = baseTriangles[i * 3] + topOffset;
            int idx1 = baseTriangles[i * 3 + 1] + topOffset;
            int idx2 = baseTriangles[i * 3 + 2] + topOffset;
            
            triangles.Add(idx0);
            triangles.Add(idx1);
            triangles.Add(idx2);
        }

        // 5. ساخت سطوح جانبی (edge loops)
        // پیدا کردن edge های sprite
        List<Edge> edges = ExtractEdges(baseTriangles, baseVertexCount);
        
        int sideOffset = baseVertexCount * 2;

        foreach (var edge in edges)
        {
            int v0 = edge.v0;
            int v1 = edge.v1;

            Vector3 bottomV0 = vertices[v0];
            Vector3 bottomV1 = vertices[v1];
            Vector3 topV0 = vertices[v0 + baseVertexCount];
            Vector3 topV1 = vertices[v1 + baseVertexCount];

            // اضافه کردن vertices برای سطح جانبی (برای normals جداگانه)
            int sideV0Bottom = vertices.Count;
            int sideV1Bottom = vertices.Count + 1;
            int sideV0Top = vertices.Count + 2;
            int sideV1Top = vertices.Count + 3;

            vertices.Add(bottomV0);
            vertices.Add(bottomV1);
            vertices.Add(topV0);
            vertices.Add(topV1);

            // UV برای سطح جانبی
            float u0 = 0f;
            float u1 = 1f;
            float vBottom = 0f;
            float vTop = 1f;

            uvs.Add(new Vector2(u0, vBottom));
            uvs.Add(new Vector2(u1, vBottom));
            uvs.Add(new Vector2(u0, vTop));
            uvs.Add(new Vector2(u1, vTop));

            // محاسبه normal برای سطح جانبی
            Vector3 sideDir = (bottomV1 - bottomV0).normalized;
            Vector3 normal = new Vector3(-sideDir.z, 0, sideDir.x);

            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            // اضافه کردن triangles برای سطح جانبی (دو مثلث)
            triangles.Add(sideV0Bottom);
            triangles.Add(sideV0Top);
            triangles.Add(sideV1Bottom);

            triangles.Add(sideV1Bottom);
            triangles.Add(sideV0Top);
            triangles.Add(sideV1Top);
        }

        // تنظیم داده‌های مش
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        mesh.RecalculateBounds();

        Debug.Log($"Column mesh created: {vertices.Count} vertices, {triangles.Count / 3} triangles, bounds: {mesh.bounds}");

        return mesh;
    }

    private struct Edge
    {
        public int v0;
        public int v1;

        public Edge(int v0, int v1)
        {
            this.v0 = v0 < v1 ? v0 : v1;
            this.v1 = v0 < v1 ? v1 : v0;
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return v0 == other.v0 && v1 == other.v1;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return v0 * 10000 + v1;
        }
    }

    private static List<Edge> ExtractEdges(ushort[] triangles, int vertexCount)
    {
        Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();

        // شمارش استفاده از هر edge
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            Edge e0 = new Edge(v0, v1);
            Edge e1 = new Edge(v1, v2);
            Edge e2 = new Edge(v2, v0);

            if (!edgeCount.ContainsKey(e0)) edgeCount[e0] = 0;
            if (!edgeCount.ContainsKey(e1)) edgeCount[e1] = 0;
            if (!edgeCount.ContainsKey(e2)) edgeCount[e2] = 0;

            edgeCount[e0]++;
            edgeCount[e1]++;
            edgeCount[e2]++;
        }

        // فقط edge هایی که فقط یک بار استفاده شده‌اند (edge های خارجی)
        List<Edge> boundaryEdges = new List<Edge>();
        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 1)
            {
                boundaryEdges.Add(kvp.Key);
            }
        }

        return boundaryEdges;
    }

    /// <summary>
    /// یک مش ستونی از شکل ساده مستطیلی می‌سازد (برای sprite هایی که vertices ندارند)
    /// </summary>
    private static Mesh CreateColumnMeshFromSimpleShape(Sprite sprite, float height)
    {
        Mesh mesh = new Mesh();
        mesh.name = sprite.name + "_ColumnMesh_Simple";

        Vector2 size = sprite.bounds.size;
        Rect rect = sprite.rect;
        Vector2 pivot = sprite.pivot;

        // محاسبه offset برای pivot
        float pivotX = (pivot.x / rect.width) - 0.5f;
        float pivotY = (pivot.y / rect.height) - 0.5f;

        // ساخت vertices برای پایه (4 گوشه)
        Vector3[] baseVertices = new Vector3[4];
        baseVertices[0] = new Vector3(-size.x * 0.5f - pivotX * size.x, 0, -size.y * 0.5f - pivotY * size.y);
        baseVertices[1] = new Vector3(size.x * 0.5f - pivotX * size.x, 0, -size.y * 0.5f - pivotY * size.y);
        baseVertices[2] = new Vector3(-size.x * 0.5f - pivotX * size.x, 0, size.y * 0.5f - pivotY * size.y);
        baseVertices[3] = new Vector3(size.x * 0.5f - pivotX * size.x, 0, size.y * 0.5f - pivotY * size.y);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        // 1. اضافه کردن vertices پایه
        for (int i = 0; i < 4; i++)
        {
            vertices.Add(baseVertices[i]);
            uvs.Add(new Vector2(i % 2, i / 2));
            normals.Add(Vector3.down);
        }

        // 2. اضافه کردن vertices بالا
        for (int i = 0; i < 4; i++)
        {
            Vector3 topVertex = baseVertices[i];
            topVertex.y = height;
            vertices.Add(topVertex);
            uvs.Add(new Vector2(i % 2, i / 2));
            normals.Add(Vector3.up);
        }

        // 3. Triangles برای پایه (معکوس برای دیده شدن از پایین)
        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(1);
        triangles.Add(1);
        triangles.Add(2);
        triangles.Add(3);

        // 4. Triangles برای بالا
        triangles.Add(4);
        triangles.Add(5);
        triangles.Add(6);
        triangles.Add(5);
        triangles.Add(7);
        triangles.Add(6);

        // 5. سطوح جانبی (4 طرف)
        int[] sideIndices = new int[] { 0, 1, 2, 3 };
        int[] nextIndices = new int[] { 1, 3, 3, 2 };

        for (int side = 0; side < 4; side++)
        {
            int bottom0 = sideIndices[side];
            int bottom1 = nextIndices[side];
            int top0 = bottom0 + 4;
            int top1 = bottom1 + 4;

            Vector3 vBottom0 = vertices[bottom0];
            Vector3 vBottom1 = vertices[bottom1];
            Vector3 vTop0 = vertices[top0];
            Vector3 vTop1 = vertices[top1];

            int currentIndex = vertices.Count;

            // اضافه کردن vertices برای سطح جانبی
            vertices.Add(vBottom0);
            vertices.Add(vBottom1);
            vertices.Add(vTop0);
            vertices.Add(vTop1);

            // UV برای سطح جانبی
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));

            // محاسبه normal
            Vector3 sideDir = (vBottom1 - vBottom0).normalized;
            Vector3 normal = new Vector3(-sideDir.z, 0, sideDir.x);

            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            // Triangles برای سطح جانبی
            triangles.Add(currentIndex);
            triangles.Add(currentIndex + 2);
            triangles.Add(currentIndex + 1);

            triangles.Add(currentIndex + 1);
            triangles.Add(currentIndex + 2);
            triangles.Add(currentIndex + 3);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        mesh.RecalculateBounds();

        Debug.Log($"Simple column mesh created: {vertices.Count} vertices, {triangles.Count / 3} triangles, bounds: {mesh.bounds}");

        return mesh;
    }
}

