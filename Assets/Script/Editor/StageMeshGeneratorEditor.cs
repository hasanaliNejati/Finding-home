using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StageMeshGenerator))]
[CanEditMultipleObjects]
public class StageMeshGeneratorEditor : Editor
{
    private StageMeshGenerator generator;
    private bool showPointEditor = true;
    private int selectedPointIndex = -1;
    private Vector3 newPointPosition = Vector3.zero;
    private bool isPaintingWatermark = false;
    private Vector3 lastPaintPosition = Vector3.zero;
    
    private void OnEnable()
    {
        generator = (StageMeshGenerator)target;
        // ثبت callback برای به‌روزرسانی مداوم Scene View
        SceneView.duringSceneGui += OnSceneGUIUpdate;
    }
    
    private void OnDisable()
    {
        // حذف callback
        SceneView.duringSceneGui -= OnSceneGUIUpdate;
    }


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Point Editor", EditorStyles.boldLabel);

        showPointEditor = EditorGUILayout.Foldout(showPointEditor, "Edit Points");

        if (showPointEditor)
        {
            EditorGUI.indentLevel++;
            


            if (generator.edgePoints == null || generator.edgePoints.Count == 0)
            {
                EditorGUILayout.HelpBox("هیچ نقطه‌ای تعریف نشده است. نقاط را اضافه کنید.", MessageType.Info);
            }
            else
            {
                // نمایش لیست نقاط
                for (int i = 0; i < generator.edgePoints.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    bool isSelected = selectedPointIndex == i;
                    if (GUILayout.Button(isSelected ? "●" : "○", GUILayout.Width(20)))
                    {
                        selectedPointIndex = isSelected ? -1 : i;
                        SceneView.RepaintAll();
                    }

                    EditorGUI.BeginChangeCheck();
                    Vector3 newPoint = EditorGUILayout.Vector3Field($"Point {i}", generator.edgePoints[i]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(generator, "Change Point Position");
                        generator.edgePoints[i] = newPoint;
                        generator.GenerateMesh();
                        EditorUtility.SetDirty(generator);
                    }

                    if (GUILayout.Button("Add After", GUILayout.Width(70)))
                    {
                        Undo.RecordObject(generator, "Add Point After");
                        Vector3 currentPoint = generator.edgePoints[i];
                        Vector3 pointToAdd = Vector3.zero;
                        
                        // محاسبه موقعیت نقطه جدید
                        if (generator.edgePoints.Count > 1)
                        {
                            if (i < generator.edgePoints.Count - 1)
                            {
                                // اگر نقطه بعدی وجود دارد، در وسط قرار بده
                                Vector3 nextPoint = generator.edgePoints[i + 1];
                                pointToAdd = (currentPoint + nextPoint) * 0.5f;
                            }
                            else
                            {
                                // اگر آخرین نقطه است، در جهت قبلی
                                Vector3 prevPoint = generator.edgePoints[i - 1];
                                Vector3 direction = (currentPoint - prevPoint).normalized;
                                pointToAdd = currentPoint + direction * 1f; // 1 واحد در جهت ادامه
                            }
                        }
                        else
                        {
                            // اگر فقط یک نقطه داریم، در سمت راست اضافه کن
                            pointToAdd = currentPoint + Vector3.right;
                        }
                        
                        // اضافه کردن نقطه بعد از نقطه انتخاب شده
                        generator.InsertPoint(i + 1, pointToAdd);
                        selectedPointIndex = i + 1; // انتخاب نقطه جدید
                        EditorUtility.SetDirty(generator);
                        SceneView.RepaintAll();
                        break;
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        Undo.RecordObject(generator, "Remove Point");
                        generator.RemovePoint(i);
                        EditorUtility.SetDirty(generator);
                        if (selectedPointIndex == i)
                            selectedPointIndex = -1;
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            // فیلد برای تعیین موقعیت نقطه جدید
            EditorGUILayout.LabelField("Add New Point", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            newPointPosition = EditorGUILayout.Vector3Field("Position", newPointPosition);
            if (EditorGUI.EndChangeCheck())
            {
                // اگر نقطه‌ای وجود دارد، موقعیت پیش‌فرض را تنظیم کن
                if (generator.edgePoints != null && generator.edgePoints.Count > 0)
                {
                    // اگر موقعیت صفر است، آخرین نقطه را به عنوان پیش‌فرض قرار بده
                    if (newPointPosition == Vector3.zero)
                    {
                        Vector3 lastPoint = generator.edgePoints[generator.edgePoints.Count - 1];
                        newPointPosition = lastPoint + Vector3.right;
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Point at Position"))
            {
                Undo.RecordObject(generator, "Add Point");
                generator.AddPoint(newPointPosition);
                EditorUtility.SetDirty(generator);
                
                // تنظیم موقعیت پیش‌فرض برای نقطه بعدی
                if (generator.edgePoints != null && generator.edgePoints.Count > 0)
                {
                    Vector3 lastPoint = generator.edgePoints[generator.edgePoints.Count - 1];
                    newPointPosition = lastPoint + Vector3.right;
                }
            }
            
            if (GUILayout.Button("Add at Last + Right"))
            {
                Undo.RecordObject(generator, "Add Point");
                Vector3 newPoint = Vector3.zero;
                if (generator.edgePoints != null && generator.edgePoints.Count > 0)
                {
                    Vector3 lastPoint = generator.edgePoints[generator.edgePoints.Count - 1];
                    newPoint = lastPoint + Vector3.right;
                }
                generator.AddPoint(newPoint);
                newPointPosition = newPoint;
                EditorUtility.SetDirty(generator);
            }

            if (GUILayout.Button("Clear All Points"))
            {
                if (EditorUtility.DisplayDialog("Clear Points", "آیا مطمئن هستید که می‌خواهید همه نقاط را پاک کنید؟", "بله", "خیر"))
                {
                    Undo.RecordObject(generator, "Clear All Points");
                    generator.ClearPoints();
                    EditorUtility.SetDirty(generator);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Watermark Brush Editor
        EditorGUILayout.LabelField("Watermark Brush", EditorStyles.boldLabel);
        
        if (generator.enableWatermark)
        {
            isPaintingWatermark = EditorGUILayout.Toggle("Paint Mode", isPaintingWatermark);
            
            if (isPaintingWatermark)
            {
                EditorGUILayout.HelpBox("در Scene View با کلیک چپ و drag کردن، واترمارک را نقاشی کنید.", MessageType.Info);
                
                // نمایش لیست texture های واترمارک
                EditorGUILayout.LabelField("Watermark Textures:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                if (generator.watermarkTextures == null || generator.watermarkTextures.Count == 0)
                {
                    EditorGUILayout.HelpBox("هیچ texture واترمارکی اضافه نشده است!", MessageType.Warning);
                }
                else
                {
                    for (int i = 0; i < generator.watermarkTextures.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        bool isSelected = generator.selectedWatermarkIndex == i;
                        if (GUILayout.Button(isSelected ? "●" : "○", GUILayout.Width(20)))
                        {
                            generator.selectedWatermarkIndex = i;
                            EditorUtility.SetDirty(generator);
                        }
                        
                        EditorGUI.BeginChangeCheck();
                        Texture2D tex = (Texture2D)EditorGUILayout.ObjectField(
                            $"Texture {i}", 
                            generator.watermarkTextures[i], 
                            typeof(Texture2D), 
                            false
                        );
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(generator, "Change Watermark Texture");
                            generator.watermarkTextures[i] = tex;
                            EditorUtility.SetDirty(generator);
                        }
                        
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            Undo.RecordObject(generator, "Remove Watermark Texture");
                            generator.watermarkTextures.RemoveAt(i);
                            if (generator.selectedWatermarkIndex >= generator.watermarkTextures.Count)
                                generator.selectedWatermarkIndex = Mathf.Max(0, generator.watermarkTextures.Count - 1);
                            EditorUtility.SetDirty(generator);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUI.indentLevel--;
                
                // دکمه اضافه کردن texture جدید
                if (GUILayout.Button("Add Watermark Texture"))
                {
                    Undo.RecordObject(generator, "Add Watermark Texture");
                    if (generator.watermarkTextures == null)
                        generator.watermarkTextures = new List<Texture2D>();
                    generator.watermarkTextures.Add(null);
                    EditorUtility.SetDirty(generator);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Brush Settings:", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                generator.brushSize = EditorGUILayout.Slider("Brush Size", generator.brushSize, 0.1f, 10f);
                generator.brushStrength = EditorGUILayout.Slider("Brush Strength", generator.brushStrength, 0f, 1f);
                generator.useAlphaFromTexture = EditorGUILayout.Toggle("Use Alpha from Texture", generator.useAlphaFromTexture);
                if (!generator.useAlphaFromTexture)
                {
                    EditorGUILayout.HelpBox("اگر این گزینه غیرفعال باشد، تصویر به صورت کامل (بدون گرادینت) می‌افتد.", MessageType.Info);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(generator);
                }
                
                // دکمه پاک کردن واترمارک
                if (GUILayout.Button("Clear All Watermarks", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Clear Watermarks", "آیا مطمئن هستید که می‌خواهید همه واترمارک‌ها را پاک کنید؟", "بله", "خیر"))
                    {
                        Undo.RecordObject(generator, "Clear Watermarks");
                        if (generator.WatermarkRenderTexture != null)
                        {
                            RenderTexture.active = generator.WatermarkRenderTexture;
                            GL.Clear(true, true, Color.clear);
                            RenderTexture.active = null;
                        }
                        generator.GenerateMesh();
                        EditorUtility.SetDirty(generator);
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("واترمارک غیرفعال است. برای استفاده، Enable Watermark را فعال کنید.", MessageType.Info);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Mesh", GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Generate Mesh");
            generator.GenerateMesh();
            EditorUtility.SetDirty(generator);
        }
    }

    private void OnSceneGUIUpdate(SceneView sceneView)
    {
        // این متد برای به‌روزرسانی مداوم Scene View استفاده می‌شود
        if (isPaintingWatermark)
        {
            sceneView.Repaint();
        }
    }

    private void OnSceneGUI()
    {
        if (generator.edgePoints == null)
            return;

        // رسم نقاط و امکان جابجایی
        for (int i = 0; i < generator.edgePoints.Count; i++)
        {
            Vector3 worldPos = generator.transform.TransformPoint(generator.edgePoints[i]);
            bool isSelected = selectedPointIndex == i;

            // تغییر اندازه Handle بر اساس انتخاب
            float handleSize = isSelected ? 0.2f : 0.15f;
            Handles.color = isSelected ? Color.red : generator.gizmoColor;

            // رسم Handle برای جابجایی
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(generator, "Move Point");
                generator.edgePoints[i] = generator.transform.InverseTransformPoint(newWorldPos);
                generator.GenerateMesh();
                EditorUtility.SetDirty(generator);
            }

            // رسم دایره برای نمایش نقطه
            Vector3 forward = Camera.current != null ? Camera.current.transform.forward : Vector3.forward;
            Handles.DrawSolidDisc(worldPos, forward, handleSize);

            // نمایش شماره نقطه
            Handles.Label(worldPos + Vector3.up * 0.3f, i.ToString(), EditorStyles.boldLabel);
        }

        // رسم خطوط بین نقاط
        Handles.color = generator.gizmoColor;
        for (int i = 0; i < generator.edgePoints.Count; i++)
        {
            int nextIndex = (i + 1) % generator.edgePoints.Count;
            Vector3 current = generator.transform.TransformPoint(generator.edgePoints[i]);
            Vector3 next = generator.transform.TransformPoint(generator.edgePoints[nextIndex]);
            Handles.DrawLine(current, next);
        }

        // امکان اضافه کردن نقطه جدید با کلیک
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, generator.transform.position);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                Vector3 localPoint = generator.transform.InverseTransformPoint(hitPoint);
                
                Undo.RecordObject(generator, "Add Point");
                generator.AddPoint(localPoint);
                newPointPosition = localPoint; // به‌روزرسانی فیلد موقعیت
                EditorUtility.SetDirty(generator);
                e.Use();
            }
        }

        // سیستم براش برای نقاشی واترمارک
        if (isPaintingWatermark && generator.enableWatermark && generator.FillMesh != null)
        {
            Event paintEvent = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            
            // Handle کردن event ها
            switch (paintEvent.type)
            {
                case EventType.MouseDown:
                case EventType.MouseDrag:
                case EventType.MouseMove:
                    if (paintEvent.button == 0)
                    {
                        // Raycast برای پیدا کردن نقطه روی mesh
                        Ray ray = HandleUtility.GUIPointToWorldRay(paintEvent.mousePosition);
                        RaycastHit hit;
                        
                        if (Physics.Raycast(ray, out hit))
                        {
                            if (hit.collider.gameObject == generator.gameObject)
                            {
                                // نمایش براش
                                Handles.color = new Color(0f, 1f, 0f, 0.3f);
                                Handles.DrawSolidDisc(hit.point, hit.normal, generator.brushSize);
                                Handles.color = Color.green;
                                Handles.DrawWireDisc(hit.point, hit.normal, generator.brushSize);
                                
                                // نقاشی در صورت drag یا کلیک
                                if (paintEvent.type == EventType.MouseDrag || paintEvent.type == EventType.MouseDown)
                                {
                                    Undo.RecordObject(generator, "Paint Watermark");
                                    
                                    // نقاشی در نقطه hit
                                    if (generator.selectedWatermarkIndex >= 0 && 
                                        generator.selectedWatermarkIndex < generator.watermarkTextures.Count &&
                                        generator.watermarkTextures[generator.selectedWatermarkIndex] != null)
                                    {
                                        generator.PaintWatermark(hit.point, generator.selectedWatermarkIndex);
                                        generator.SetupMaterials(); // فقط material را به‌روز کن، نه کل mesh
                                        EditorUtility.SetDirty(generator);
                                    }
                                    
                                    paintEvent.Use();
                                }
                                
                                lastPaintPosition = hit.point;
                                GUIUtility.hotControl = controlID;
                            }
                        }
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (paintEvent.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        paintEvent.Use();
                    }
                    break;
                    
                case EventType.Repaint:
                    // نمایش آخرین موقعیت براش
                    if (lastPaintPosition != Vector3.zero)
                    {
                        Handles.color = new Color(0f, 1f, 0f, 0.3f);
                        Handles.DrawSolidDisc(lastPaintPosition, Vector3.up, generator.brushSize);
                        Handles.color = Color.green;
                        Handles.DrawWireDisc(lastPaintPosition, Vector3.up, generator.brushSize);
                    }
                    break;
            }
            
            // Force repaint برای دنبال کردن mouse
            if (paintEvent.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
            }
        }
    }
}

