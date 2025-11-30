using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class StageMeshGenerator : MonoBehaviour
{
    [Header("Edge Points (Like Edge Collider 2D)")]
    [Tooltip("نقاط لبه مش - مثل Edge Collider 2D")]
    public List<Vector3> edgePoints = new List<Vector3>();

    [Header("Mesh Settings")]
    [Tooltip("نمایش حاشیه‌ها")]
    public bool showEdges = false;

    [Tooltip("ضخامت حاشیه‌ها (عرض نوار حاشیه)")]
    [Range(0.01f, 2f)]
    public float edgeThickness = 0.2f;

    [Tooltip("ارتفاع حاشیه نسبت به سطح (برای ایجاد عمق)")]
    [Range(-0.1f, 0.1f)]
    public float edgeHeightOffset = 0f;

    [Header("Corner Settings")]
    [Tooltip("میزان کرو بودن گوشه‌ها (0 = تیز، 1 = کاملاً کرو)")]
    [Range(0f, 1f)]
    public float cornerRoundness = 0.5f;

    [Tooltip("تعداد بخش‌های منحنی در هر گوشه (بیشتر = نرم‌تر)")]
    [Range(3, 20)]
    public int cornerSegments = 8;

    [Header("Hill Settings")]
    [Tooltip("نمایش تپه")]
    public bool showHill = false;

    [Tooltip("حداکثر ارتفاع تپه")]
    [Range(-200f, 0)]
    public float maxHillHeight = 2f;

    [Tooltip("منحنی ارتفاع (0 = خطی، 1 = درجه 2، 2 = درجه 3)")]
    [Range(0f, 2f)]
    public float heightCurve = 1f;

    [Tooltip("رزولوشن تپه (تعداد نقاط در هر جهت)")]
    [Range(10, 100)]
    public int hillResolution = 50;

    [Tooltip("Scale تپه (مقیاس ارتفاع)")]
    [Range(0.1f, 5f)]
    public float hillScale = 1f;

    [Tooltip("Offset تپه (جابجایی ارتفاع)")]
    [Range(-10f, 10f)]
    public float hillOffset = 0f;

    [Tooltip("متریال تپه")]
    public Material hillMaterial;

    [Header("Materials")]
    [Tooltip("متریال برای سطح مش (رنگ یکنواخت)")]
    public Material fillMaterial;

    [Tooltip("متریال برای لبه‌ها (با بافت)")]
    public Material edgeMaterial;

    [Header("Visual Settings")]
    [Tooltip("رنگ سطح مش")]
    public Color fillColor = Color.white;

    [Tooltip("تکرار بافت سطح (tile)")]
    public float fillTextureTiling = 1f;

    [Tooltip("تکرار بافت لبه")]
    public float edgeTextureTiling = 1f;

    [Header("Debug")]
    [Tooltip("نمایش نقاط در Scene View")]
    public bool showGizmos = true;

    [Tooltip("رنگ Gizmo")]
    public Color gizmoColor = Color.yellow;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh fillMesh;
    private Mesh edgeMesh;
    private Mesh hillMesh;
    private GameObject edgeObject;
    private GameObject hillObject;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // ساخت آبجکت جداگانه برای لبه‌ها
        if (edgeObject == null)
        {
            edgeObject = new GameObject("EdgeMesh");
            edgeObject.transform.SetParent(transform);
            edgeObject.transform.localPosition = Vector3.zero;
            edgeObject.transform.localRotation = Quaternion.identity;
            edgeObject.transform.localScale = Vector3.one;
        }
        
        // فعال/غیرفعال کردن edge object
        if (edgeObject != null)
        {
            edgeObject.SetActive(showEdges);
        }

        // ساخت آبجکت جداگانه برای تپه
        if (hillObject == null)
        {
            hillObject = new GameObject("HillMesh");
            hillObject.transform.SetParent(transform);
            hillObject.transform.localPosition = Vector3.zero;
            hillObject.transform.localRotation = Quaternion.identity;
            hillObject.transform.localScale = Vector3.one;
        }
        
        // فعال/غیرفعال کردن hill object
        if (hillObject != null)
        {
            hillObject.SetActive(showHill);
        }
    }

    [ContextMenu("Generate Mesh")]
    public void GenerateMesh()
    {
        if (edgePoints == null || edgePoints.Count < 3)
        {
            Debug.LogWarning("حداقل 3 نقطه برای ساخت مش نیاز است!");
            return;
        }

        InitializeComponents();

        // ساخت مش سطح
        GenerateFillMesh();

        // ساخت مش لبه‌ها (فقط اگر فعال باشد)
        if (showEdges)
        {
            GenerateEdgeMesh();
        }
        else
        {
            // پنهان کردن edge object
            if (edgeObject != null)
            {
                edgeObject.SetActive(false);
            }
        }

        // ساخت مش تپه (فقط اگر فعال باشد)
        if (showHill)
        {
            GenerateHillMesh();
        }
        else
        {
            // پنهان کردن hill object
            if (hillObject != null)
            {
                hillObject.SetActive(false);
            }
        }

        // تنظیم متریال‌ها
        SetupMaterials();
    }

    private void GenerateFillMesh()
    {
        fillMesh = new Mesh();
        fillMesh.name = "StageFillMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        // تبدیل نقاط به صفحه XZ (سطح زمین)
        List<Vector2> points2D = new List<Vector2>();
        foreach (var point in edgePoints)
        {
            points2D.Add(new Vector2(point.x, point.z));
        }

        // تولید نقاط با گوشه‌های کرو
        List<Vector2> roundedPoints2D = GenerateRoundedCorners(points2D);

        // محاسبه مرکز برای UV mapping بهتر
        Vector2 center = Vector2.zero;
        foreach (var point in roundedPoints2D)
        {
            center += point;
        }
        center /= roundedPoints2D.Count;

        // اضافه کردن رأس‌های سطح (فقط یک سطح تخت)
        for (int i = 0; i < roundedPoints2D.Count; i++)
        {
            vertices.Add(new Vector3(roundedPoints2D[i].x, 0, roundedPoints2D[i].y));
            
            // UV mapping بر اساس موقعیت واقعی با tile
            // استفاده از موقعیت مستقیم برای tile درست
            float u = roundedPoints2D[i].x * fillTextureTiling;
            float v = roundedPoints2D[i].y * fillTextureTiling;
            uvs.Add(new Vector2(u, v));
            colors.Add(fillColor);
        }

        // استفاده از Ear Clipping برای triangulation چندضلعی‌های محدب و مقعر
        List<int> indices = TriangulatePolygon(roundedPoints2D);
        triangles.AddRange(indices);

        fillMesh.vertices = vertices.ToArray();
        fillMesh.triangles = triangles.ToArray();
        fillMesh.uv = uvs.ToArray();
        fillMesh.colors = colors.ToArray();
        fillMesh.RecalculateNormals();
        fillMesh.RecalculateBounds();

        meshFilter.mesh = fillMesh;
    }

    private List<int> TriangulatePolygon(List<Vector2> points)
    {
        List<int> indices = new List<int>();
        
        if (points == null || points.Count < 3)
            return indices;

        // اگر فقط 3 نقطه داریم، یک مثلث ساده (ترتیب معکوس)
        if (points.Count == 3)
        {
            indices.Add(0);
            indices.Add(2);
            indices.Add(1);
            return indices;
        }

        // ایجاد لیست ایندکس‌ها
        List<int> vertexIndices = new List<int>();
        for (int i = 0; i < points.Count; i++)
        {
            vertexIndices.Add(i);
        }

        // Ear Clipping Algorithm
        while (vertexIndices.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < vertexIndices.Count; i++)
            {
                int prevIdx = vertexIndices[(i - 1 + vertexIndices.Count) % vertexIndices.Count];
                int currIdx = vertexIndices[i];
                int nextIdx = vertexIndices[(i + 1) % vertexIndices.Count];

                Vector2 prev = points[prevIdx];
                Vector2 curr = points[currIdx];
                Vector2 next = points[nextIdx];

                // بررسی اینکه آیا این یک "ear" است (مثلثی که هیچ نقطه دیگری داخلش نیست)
                if (IsEar(prev, curr, next, points, vertexIndices))
                {
                    // اضافه کردن مثلث (ترتیب معکوس برای دیده شدن از بالا)
                    indices.Add(prevIdx);
                    indices.Add(nextIdx);
                    indices.Add(currIdx);

                    // حذف گوشه از لیست
                    vertexIndices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            // اگر هیچ ear پیدا نشد، از Triangle Fan استفاده کن (fallback)
            if (!earFound)
            {
                for (int i = 1; i < vertexIndices.Count - 1; i++)
                {
                    indices.Add(vertexIndices[0]);
                    indices.Add(vertexIndices[i]);
                    indices.Add(vertexIndices[i + 1]);
                }
                break;
            }
        }

        // آخرین مثلث (ترتیب معکوس)
        if (vertexIndices.Count == 3)
        {
            indices.Add(vertexIndices[0]);
            indices.Add(vertexIndices[2]);
            indices.Add(vertexIndices[1]);
        }

        return indices;
    }

    private bool IsEar(Vector2 a, Vector2 b, Vector2 c, List<Vector2> allPoints, List<int> remainingIndices)
    {
        // بررسی اینکه مثلث ABC محدب است
        Vector2 ab = b - a;
        Vector2 bc = c - b;
        float cross = ab.x * bc.y - ab.y * bc.x;

        // اگر مثلث clockwise باشد، ear نیست
        if (cross <= 0)
            return false;

        // بررسی اینکه هیچ نقطه دیگری داخل مثلث نیست
        for (int i = 0; i < remainingIndices.Count; i++)
        {
            int idx = remainingIndices[i];
            Vector2 p = allPoints[idx];

            // اگر نقطه یکی از رأس‌های مثلث است، رد کن
            if (p == a || p == b || p == c)
                continue;

            // بررسی اینکه آیا نقطه داخل مثلث است
            if (IsPointInTriangle(p, a, b, c))
                return false;
        }

        return true;
    }

    private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // استفاده از Barycentric coordinates
        float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Mathf.Abs(denom) < 0.0001f)
            return false;

        float alpha = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denom;
        float beta = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denom;
        float gamma = 1 - alpha - beta;

        return alpha >= 0 && beta >= 0 && gamma >= 0;
    }

    private List<Vector2> GenerateRoundedCorners(List<Vector2> originalPoints)
    {
        if (originalPoints == null || originalPoints.Count < 3)
            return originalPoints;

        // اگر کرو بودن خاموش است، نقاط اصلی را برگردان
        if (cornerRoundness <= 0.001f)
        {
            return new List<Vector2>(originalPoints);
        }

        List<Vector2> roundedPoints = new List<Vector2>();
        int pointCount = originalPoints.Count;

        for (int i = 0; i < pointCount; i++)
        {
            int prevIndex = (i - 1 + pointCount) % pointCount;
            int nextIndex = (i + 1) % pointCount;

            Vector2 prevPoint = originalPoints[prevIndex];
            Vector2 currentPoint = originalPoints[i];
            Vector2 nextPoint = originalPoints[nextIndex];

            // محاسبه جهت‌های ورود و خروج
            Vector2 dir1 = (currentPoint - prevPoint).normalized;
            Vector2 dir2 = (nextPoint - currentPoint).normalized;

            // تشخیص اینکه گوشه محدب است یا مقعر (با استفاده از cross product)
            // cross product برای تعیین جهت چرخش
            float cross = dir1.x * dir2.y - dir1.y * dir2.x;
            bool isConvex = cross > 0; // اگر مثبت باشد، محدب است (counter-clockwise)

            // محاسبه زاویه داخلی
            float dot = Vector2.Dot(-dir1, dir2);
            dot = Mathf.Clamp(dot, -1f, 1f);
            float angleRad = Mathf.Acos(dot);
            float angle = angleRad * Mathf.Rad2Deg;

            // محاسبه طول یال‌ها
            float dist1 = Vector2.Distance(prevPoint, currentPoint);
            float dist2 = Vector2.Distance(currentPoint, nextPoint);
            float minDist = Mathf.Min(dist1, dist2);

            // محاسبه شعاع
            float radius = minDist * 0.5f * cornerRoundness;
            radius = Mathf.Min(radius, dist1 * 0.4f, dist2 * 0.4f);

            // اگر شعاع خیلی کوچک است، نقطه اصلی را اضافه کن
            if (radius < 0.01f)
            {
                roundedPoints.Add(currentPoint);
                continue;
            }

            // برای گوشه‌های مقعر (زاویه > 180)، منحنی باید به سمت بیرون برود
            // برای گوشه‌های محدب (زاویه < 180)، منحنی باید به سمت داخل برود
            Vector2 p1, p2, controlPoint;

            // تشخیص درست محدب/مقعر بر اساس زاویه
            bool isActuallyConvex = angle < 180f;

            if (isActuallyConvex)
            {
                // گوشه محدب - منحنی به سمت داخل
                p1 = currentPoint - dir1 * radius;
                p2 = currentPoint + dir2 * radius;
                controlPoint = currentPoint;
            }
            else
            {
                // گوشه مقعر - منحنی به سمت بیرون
                // برای گوشه مقعر، از روش ساده‌تر استفاده می‌کنیم
                
                // محاسبه نیم‌ساز (به سمت بیرون)
                Vector2 bisector = (dir1 + dir2).normalized;
                
                // برای گوشه مقعر، bisector باید به سمت بیرون باشد
                // استفاده از cross product برای تعیین جهت درست
                // برای مقعر، cross product منفی است
                if (cross < 0)
                {
                    // bisector را به سمت بیرون تنظیم می‌کنیم
                    // استفاده از عمود بر dir1 برای تعیین جهت
                    Vector2 perpDir1 = new Vector2(-dir1.y, dir1.x);
                    float dotWithBisector = Vector2.Dot(perpDir1, bisector);
                    if (dotWithBisector > 0)
                    {
                        bisector = -bisector;
                    }
                }
                
                // برای گوشه مقعر، از radius کوچکتر استفاده می‌کنیم تا منحنی نرم‌تر باشد
                float concaveRadius = radius * 0.6f;
                
                // محاسبه فاصله از گوشه (کوچکتر برای مقعر)
                float offset = concaveRadius;
                offset = Mathf.Min(offset, dist1 * 0.25f, dist2 * 0.25f);
                
                p1 = currentPoint - dir1 * offset;
                p2 = currentPoint + dir2 * offset;
                
                // کنترل نقطه برای منحنی بیرونی (به سمت بیرون)
                // فاصله کنترل نقطه را بر اساس شعاع تنظیم می‌کنیم
                float controlOffset = concaveRadius * 2f;
                controlPoint = currentPoint + bisector * controlOffset;
            }

            // اضافه کردن نقطه شروع (اگر اولین نقطه نیست و فاصله دارد)
            if (i > 0 && roundedPoints.Count > 0)
            {
                Vector2 lastPoint = roundedPoints[roundedPoints.Count - 1];
                if (Vector2.Distance(lastPoint, p1) > 0.01f)
                {
                    roundedPoints.Add(p1);
                }
            }
            else if (i == 0)
            {
                roundedPoints.Add(p1);
            }

            // تولید نقاط منحنی با Bezier quadratic
            int segments = cornerSegments;
            for (int j = 1; j <= segments; j++)
            {
                float t = (float)j / segments;
                // Bezier quadratic: (1-t)²P0 + 2(1-t)tP1 + t²P2
                float oneMinusT = 1f - t;
                Vector2 point = oneMinusT * oneMinusT * p1 + 
                               2f * oneMinusT * t * controlPoint + 
                               t * t * p2;
                roundedPoints.Add(point);
            }
        }

        return roundedPoints;
    }

    private void GenerateEdgeMesh()
    {
        if (edgePoints == null || edgePoints.Count < 2)
            return;

        edgeMesh = new Mesh();
        edgeMesh.name = "StageEdgeMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // محاسبه مرکز چندضلعی (یک بار)
        Vector3 polygonCenter = Vector3.zero;
        foreach (var pt in edgePoints)
        {
            polygonCenter += pt;
        }
        polygonCenter /= edgePoints.Count;

        // ساخت حاشیه‌ها به صورت نوار تخت در اطراف زمین
        for (int i = 0; i < edgePoints.Count; i++)
        {
            int nextIndex = (i + 1) % edgePoints.Count;
            Vector3 current = edgePoints[i];
            Vector3 next = edgePoints[nextIndex];

            // محاسبه جهت و عمود بر یال (در صفحه XZ)
            Vector3 direction = (next - current).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            
            // بررسی اینکه perpendicular به سمت داخل است یا خارج
            Vector3 toCenter = (polygonCenter - current).normalized;
            float dot = Vector3.Dot(perpendicular, toCenter);
            
            // اگر dot منفی باشد، perpendicular به سمت خارج است، پس باید معکوس کنیم
            if (dot < 0)
            {
                perpendicular = -perpendicular;
            }

            // نقاط خارجی (لبه زمین - edgePoints)
            Vector3 outerCurrent = current;
            Vector3 outerNext = next;
            
            // نقاط داخلی (داخل زمین - از لبه به سمت داخل)
            Vector3 innerCurrent = current - perpendicular * edgeThickness;
            Vector3 innerNext = next - perpendicular * edgeThickness;

            // اعمال ارتفاع (اگر نیاز باشد)
            float height = edgeHeightOffset;

            int baseIndex = vertices.Count;

            // اضافه کردن رأس‌ها (4 رأس برای هر بخش حاشیه)
            vertices.Add(new Vector3(innerCurrent.x, height, innerCurrent.z));
            vertices.Add(new Vector3(outerCurrent.x, height, outerCurrent.z));
            vertices.Add(new Vector3(innerNext.x, height, innerNext.z));
            vertices.Add(new Vector3(outerNext.x, height, outerNext.z));

            // UV برای بافت (بر اساس طول یال)
            float edgeLength = Vector3.Distance(current, next);
            float uvLength = edgeLength * edgeTextureTiling;
            
            uvs.Add(new Vector2(0, 0));           // inner current
            uvs.Add(new Vector2(1, 0));           // outer current
            uvs.Add(new Vector2(0, uvLength));    // inner next
            uvs.Add(new Vector2(1, uvLength));    // outer next

            // ساخت مثلث‌های حاشیه (2 مثلث برای هر بخش)
            // ترتیب معکوس برای دیده شدن از بالا
            triangles.Add(baseIndex + 0);  // inner current
            triangles.Add(baseIndex + 1);  // outer current
            triangles.Add(baseIndex + 2);  // inner next

            triangles.Add(baseIndex + 1);  // outer current
            triangles.Add(baseIndex + 3);   // outer next
            triangles.Add(baseIndex + 2);  // inner next
        }

        edgeMesh.vertices = vertices.ToArray();
        edgeMesh.triangles = triangles.ToArray();
        edgeMesh.uv = uvs.ToArray();
        edgeMesh.RecalculateNormals();
        edgeMesh.RecalculateBounds();

        // اعمال مش به آبجکت لبه
        MeshFilter edgeMeshFilter = edgeObject.GetComponent<MeshFilter>();
        if (edgeMeshFilter == null)
            edgeMeshFilter = edgeObject.AddComponent<MeshFilter>();

        edgeMeshFilter.mesh = edgeMesh;
    }

    private void GenerateHillMesh()
    {
        if (edgePoints == null || edgePoints.Count < 3)
            return;

        hillMesh = new Mesh();
        hillMesh.name = "HillMesh";

        // تبدیل نقاط به صفحه XZ
        List<Vector2> points2D = new List<Vector2>();
        foreach (var point in edgePoints)
        {
            points2D.Add(new Vector2(point.x, point.z));
        }

        // تولید نقاط با گوشه‌های کرو (اگر فعال باشد)
        List<Vector2> roundedPoints2D = GenerateRoundedCorners(points2D);

        // محاسبه محدوده چندضلعی (دقیقاً همان محدوده fill mesh)
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var point in roundedPoints2D)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.y);
            maxZ = Mathf.Max(maxZ, point.y);
        }

        // محاسبه حداکثر فاصله از لبه (برای نرمال‌سازی)
        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
        float maxDistance = DistanceToEdge(center, roundedPoints2D);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // ابتدا اضافه کردن نقاط لبه (با ارتفاع 0) - دقیقاً همان vertices fill mesh
        Dictionary<Vector2, int> edgeVertexMap = new Dictionary<Vector2, int>();
        for (int i = 0; i < roundedPoints2D.Count; i++)
        {
            Vector2 point2D = roundedPoints2D[i];
            // ارتفاع در لبه = 0
            vertices.Add(new Vector3(point2D.x, hillOffset, point2D.y));
            edgeVertexMap[point2D] = vertices.Count - 1;
            
            // UV mapping
            float u = point2D.x * fillTextureTiling;
            float v = point2D.y * fillTextureTiling;
            uvs.Add(new Vector2(u, v));
        }

        // ساخت grid از نقاط داخل چندضلعی (دقیقاً همان محدوده)
        float stepX = (maxX - minX) / hillResolution;
        float stepZ = (maxZ - minZ) / hillResolution;

        // Map برای نگهداری index هر نقطه در grid
        Dictionary<int, int> gridToVertexIndex = new Dictionary<int, int>();
        int width = hillResolution + 1;

        // ساخت vertices داخلی و map
        for (int z = 0; z <= hillResolution; z++)
        {
            for (int x = 0; x <= hillResolution; x++)
            {
                Vector2 point2D = new Vector2(minX + x * stepX, minZ + z * stepZ);
                
                // بررسی اینکه نقطه داخل چندضلعی است یا نه
                if (IsPointInsidePolygon(point2D, roundedPoints2D))
                {
                    // بررسی اینکه آیا این نقطه روی لبه است یا نه (با tolerance کوچک)
                    bool isOnEdge = false;
                    foreach (var edgePoint in roundedPoints2D)
                    {
                        if (Vector2.Distance(point2D, edgePoint) < 0.01f)
                        {
                            isOnEdge = true;
                            break;
                        }
                    }
                    
                    // اگر روی لبه است، از vertex موجود استفاده کن
                    if (isOnEdge)
                    {
                        // پیدا کردن نزدیک‌ترین نقطه لبه
                        Vector2 closestEdgePoint = roundedPoints2D[0];
                        float minDist = Vector2.Distance(point2D, closestEdgePoint);
                        foreach (var edgePoint in roundedPoints2D)
                        {
                            float dist = Vector2.Distance(point2D, edgePoint);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                closestEdgePoint = edgePoint;
                            }
                        }
                        
                        if (edgeVertexMap.ContainsKey(closestEdgePoint))
                        {
                            int edgeGridIndex = z * width + x;
                            gridToVertexIndex[edgeGridIndex] = edgeVertexMap[closestEdgePoint];
                        }
                        continue;
                    }
                    
                    // نقطه داخلی - محاسبه ارتفاع
                    float distance = DistanceToEdge(point2D, roundedPoints2D);
                    
                    // نرمال‌سازی فاصله (0 تا 1)
                    float normalizedDistance = maxDistance > 0 ? distance / maxDistance : 0f;
                    
                    // اعمال منحنی ارتفاع
                    float heightFactor = ApplyHeightCurve(normalizedDistance);
                    
                    // محاسبه ارتفاع نهایی با scale و offset
                    float height = (heightFactor * maxHillHeight * hillScale) + hillOffset;
                    
                    int vertexIndex = vertices.Count;
                    vertices.Add(new Vector3(point2D.x, height, point2D.y));
                    
                    // ذخیره mapping
                    int gridIndex = z * width + x;
                    gridToVertexIndex[gridIndex] = vertexIndex;
                    
                    // UV mapping
                    float u = (float)x / hillResolution;
                    float v = (float)z / hillResolution;
                    uvs.Add(new Vector2(u, v));
                }
            }
        }

        // ساخت مثلث‌ها برای grid
        for (int z = 0; z < hillResolution; z++)
        {
            for (int x = 0; x < hillResolution; x++)
            {
                int gridIndex0 = z * width + x;
                int gridIndex1 = z * width + (x + 1);
                int gridIndex2 = (z + 1) * width + x;
                int gridIndex3 = (z + 1) * width + (x + 1);

                // بررسی اینکه همه نقاط داخل چندضلعی هستند و در map وجود دارند
                if (gridToVertexIndex.ContainsKey(gridIndex0) && 
                    gridToVertexIndex.ContainsKey(gridIndex1) && 
                    gridToVertexIndex.ContainsKey(gridIndex2))
                {
                    int i0 = gridToVertexIndex[gridIndex0];
                    int i1 = gridToVertexIndex[gridIndex1];
                    int i2 = gridToVertexIndex[gridIndex2];
                    
                    // ترتیب معکوس برای دیده شدن از بالا
                    triangles.Add(i0);
                    triangles.Add(i1);
                    triangles.Add(i2);
                }

                if (gridToVertexIndex.ContainsKey(gridIndex1) && 
                    gridToVertexIndex.ContainsKey(gridIndex2) && 
                    gridToVertexIndex.ContainsKey(gridIndex3))
                {
                    int i1 = gridToVertexIndex[gridIndex1];
                    int i2 = gridToVertexIndex[gridIndex2];
                    int i3 = gridToVertexIndex[gridIndex3];
                    
                    // ترتیب معکوس برای دیده شدن از بالا
                    triangles.Add(i1);
                    triangles.Add(i3);
                    triangles.Add(i2);
                }
            }
        }

        hillMesh.vertices = vertices.ToArray();
        hillMesh.triangles = triangles.ToArray();
        hillMesh.uv = uvs.ToArray();
        hillMesh.RecalculateNormals();
        hillMesh.RecalculateBounds();

        // اعمال مش به آبجکت تپه
        MeshFilter hillMeshFilter = hillObject.GetComponent<MeshFilter>();
        if (hillMeshFilter == null)
            hillMeshFilter = hillObject.AddComponent<MeshFilter>();

        hillMeshFilter.mesh = hillMesh;

        // تنظیم متریال تپه
        MeshRenderer hillRenderer = hillObject.GetComponent<MeshRenderer>();
        if (hillRenderer == null)
            hillRenderer = hillObject.AddComponent<MeshRenderer>();

        if (hillMaterial != null)
        {
            hillRenderer.sharedMaterial = hillMaterial;
        }
        else
        {
            Material defaultHill = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultHill.color = new Color(0.8f, 0.6f, 0.4f); // رنگ قهوه‌ای برای تپه
            hillRenderer.sharedMaterial = defaultHill;
        }
    }

    private bool IsPointInsidePolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            if (((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
            {
                inside = !inside;
            }

            j = i;
        }

        return inside;
    }

    private float DistanceToEdge(Vector2 point, List<Vector2> edgePoints)
    {
        float minDistance = float.MaxValue;

        // محاسبه فاصله تا نزدیک‌ترین لبه
        for (int i = 0; i < edgePoints.Count; i++)
        {
            int nextIndex = (i + 1) % edgePoints.Count;
            Vector2 edgeStart = edgePoints[i];
            Vector2 edgeEnd = edgePoints[nextIndex];

            // فاصله تا خط لبه
            float distance = DistanceToLineSegment(point, edgeStart, edgeEnd);
            minDistance = Mathf.Min(minDistance, distance);
        }

        return minDistance;
    }

    private float DistanceToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        
        if (lineLength < 0.0001f)
            return Vector2.Distance(point, lineStart);

        Vector2 lineNormalized = line / lineLength;
        Vector2 toPoint = point - lineStart;
        
        float projection = Vector2.Dot(toPoint, lineNormalized);
        projection = Mathf.Clamp(projection, 0f, lineLength);
        
        Vector2 closestPoint = lineStart + lineNormalized * projection;
        return Vector2.Distance(point, closestPoint);
    }

    private float CalculateMaxDistanceFromEdge(List<Vector2> points)
    {
        float maxDistance = 0f;
        
        // نمونه‌گیری از نقاط داخل چندضلعی برای پیدا کردن حداکثر فاصله
        // استفاده از مرکز چندضلعی
        Vector2 center = Vector2.zero;
        foreach (var point in points)
        {
            center += point;
        }
        center /= points.Count;
        
        // محاسبه فاصله مرکز تا لبه
        maxDistance = DistanceToEdge(center, points);
        
        return maxDistance;
    }

    private float ApplyHeightCurve(float normalizedDistance)
    {
        if (heightCurve < 0.1f)
        {
            // خطی
            return normalizedDistance;
        }
        else if (heightCurve < 1.1f)
        {
            // درجه 2 (quadratic)
            return normalizedDistance * normalizedDistance;
        }
        else
        {
            // درجه 3 (cubic)
            return normalizedDistance * normalizedDistance * normalizedDistance;
        }
    }

    private void SetupMaterials()
    {
        // تنظیم متریال سطح
        if (fillMaterial != null)
        {
            meshRenderer.sharedMaterial = fillMaterial;
            // استفاده از MaterialPropertyBlock برای تغییرات بدون leak
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(mpb);
            
            // اگر متریال رنگ داشت، تنظیم کن
            if (fillMaterial.HasProperty("_Color"))
            {
                mpb.SetColor("_Color", fillColor);
            }
            // تنظیم تکرار بافت
            if (fillMaterial.HasProperty("_MainTex_ST"))
            {
                Vector4 tiling = fillMaterial.GetVector("_MainTex_ST");
                tiling.x = fillTextureTiling;
                tiling.y = fillTextureTiling;
                mpb.SetVector("_MainTex_ST", tiling);
            }
            meshRenderer.SetPropertyBlock(mpb);
        }
        else
        {
            // ساخت متریال پیش‌فرض
            Material defaultFill = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultFill.color = fillColor;
            // تنظیم tile
            if (defaultFill.HasProperty("_MainTex_ST"))
            {
                Vector4 tiling = defaultFill.GetVector("_MainTex_ST");
                tiling.x = fillTextureTiling;
                tiling.y = fillTextureTiling;
                defaultFill.SetVector("_MainTex_ST", tiling);
            }
            meshRenderer.sharedMaterial = defaultFill;
        }

        // تنظیم متریال لبه
        MeshRenderer edgeRenderer = edgeObject.GetComponent<MeshRenderer>();
        if (edgeRenderer == null)
            edgeRenderer = edgeObject.AddComponent<MeshRenderer>();

        if (edgeMaterial != null)
        {
            edgeRenderer.sharedMaterial = edgeMaterial;
            // استفاده از MaterialPropertyBlock برای تغییرات بدون leak
            MaterialPropertyBlock edgeMpb = new MaterialPropertyBlock();
            edgeRenderer.GetPropertyBlock(edgeMpb);
            
            // تنظیم تکرار بافت
            if (edgeMaterial.HasProperty("_MainTex_ST"))
            {
                Vector4 tiling = edgeMaterial.GetVector("_MainTex_ST");
                tiling.z = edgeTextureTiling;
                edgeMpb.SetVector("_MainTex_ST", tiling);
            }
            edgeRenderer.SetPropertyBlock(edgeMpb);
        }
        else
        {
            // ساخت متریال پیش‌فرض برای لبه
            Material defaultEdge = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            edgeRenderer.sharedMaterial = defaultEdge;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || edgePoints == null)
            return;

        Gizmos.color = gizmoColor;

        // رسم خطوط بین نقاط
        for (int i = 0; i < edgePoints.Count; i++)
        {
            int nextIndex = (i + 1) % edgePoints.Count;
            Vector3 current = transform.TransformPoint(edgePoints[i]);
            Vector3 next = transform.TransformPoint(edgePoints[nextIndex]);

            Gizmos.DrawLine(current, next);
            Gizmos.DrawSphere(current, 0.1f);
        }

        // رسم مرکز زمین
        if (edgePoints.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (var point in edgePoints)
            {
                center += point;
            }
            center /= edgePoints.Count;
            center = transform.TransformPoint(center);
            Gizmos.color = gizmoColor * 0.3f;
            Gizmos.DrawSphere(center, 0.05f);
        }
    }

    private void OnValidate()
    {
        // در Edit Mode، مش رو دوباره بساز
        if (!Application.isPlaying && edgePoints != null && edgePoints.Count >= 3)
        {
            GenerateMesh();
        }
    }

    // متدهای کمکی برای ویرایش نقاط
    public void AddPoint(Vector3 point)
    {
        if (edgePoints == null)
            edgePoints = new List<Vector3>();

        edgePoints.Add(point);
        GenerateMesh();
    }

    public void InsertPoint(int index, Vector3 point)
    {
        if (edgePoints == null)
            edgePoints = new List<Vector3>();

        if (index < 0)
            index = 0;
        if (index > edgePoints.Count)
            index = edgePoints.Count;

        edgePoints.Insert(index, point);
        GenerateMesh();
    }

    public void RemovePoint(int index)
    {
        if (edgePoints != null && index >= 0 && index < edgePoints.Count)
        {
            edgePoints.RemoveAt(index);
            GenerateMesh();
        }
    }

    public void ClearPoints()
    {
        if (edgePoints != null)
        {
            edgePoints.Clear();
            GenerateMesh();
        }
    }
}

