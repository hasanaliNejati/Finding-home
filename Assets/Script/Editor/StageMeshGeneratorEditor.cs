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

    private void OnEnable()
    {
        generator = (StageMeshGenerator)target;
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

        if (GUILayout.Button("Generate Mesh", GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Generate Mesh");
            generator.GenerateMesh();
            EditorUtility.SetDirty(generator);
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
    }
}

