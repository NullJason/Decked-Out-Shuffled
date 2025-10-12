using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIListLayout : MonoBehaviour
{
    [Header("Mode")]
    public bool horizontal = true;
    public bool vertical = false; // if both true -> table/grid mode

    [Header("Spacing & Padding")]
    public float spacingX = 8f;
    public float spacingY = 8f;
    public float indentPerItem = 0f; // additive offset each next item (in primary axis)
    public float paddingLeft = 8f;
    public float paddingRight = 8f;
    public float paddingTop = 8f;
    public float paddingBottom = 8f;
    public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }

    [Header("Table Settings (when both horizontal & vertical)")]
    public Constraint tableConstraint = Constraint.Flexible;
    [Min(1)] public int constraintCount = 3; // columns if FixedColumnCount, rows if FixedRowCount
    public bool uniformCellSize = false;
    public Vector2 cellSize = new Vector2(100, 30); // used if uniformCellSize=true

    [Header("Sizing & Alignment")]
    public bool useChildPreferredSize = true; // if false, use child's RectTransform.sizeDelta (current)
    public enum AlignHorizontal { Left, Center, Right }
    public enum AlignVertical { Top, Middle, Bottom }
    public AlignHorizontal alignX = AlignHorizontal.Left;
    public AlignVertical alignY = AlignVertical.Top;

    [Header("Rotation")]
    [Range(-180f, 180f)] public float rotationDegrees = 0f; // rotate layout around container pivot
    public bool rotateChildrenWithList = false; // if true, apply rotation to children localEulerAngles.z

    [Header("Editor / Preview")]
    [Tooltip("Show a transparent preview in Scene view")]
    public bool showPreview = true;
    [Range(0f, 1f)] public float previewAlpha = 0.35f;
    public Color previewColor = Color.cyan;
    public bool autoApplyInEditor = false; // if true, ApplyLayout runs in editor when validated

    [Header("Apply Behavior")]
    public bool setSizeDeltaToChild = false; // if uniformCellSize true, optionally set child's sizeDelta to cellSize when applying
    public bool ignoreInactiveChildren = true;

    // ========== Public API ==========
    public void ApplyLayout()
    {
        RectTransform container = transform as RectTransform;
        if (container == null) return;

        List<RectTransform> children = GatherChildren(container);
        if (children.Count == 0) return;

        // compute positions in local space (container local)
        List<Rect> childRects = GetChildRects(children);
        List<Vector2> positions = ComputeLayoutPositions(container, childRects);

        // Apply positions and optional rotations/sizes
        for (int i = 0; i < children.Count; i++)
        {
            RectTransform child = children[i];
            Vector2 pos = positions[i];

            // set anchored position (respecting pivot)
            child.anchoredPosition = pos;

            // set rotation if requested
            if (rotateChildrenWithList)
            {
                Vector3 e = child.localEulerAngles;
                e.z = rotationDegrees;
                child.localEulerAngles = e;
            }

            // optionally set sizeDelta
            if (uniformCellSize && setSizeDeltaToChild)
            {
                Vector2 size = cellSize;
                child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            }
        }
    }

    // Gather children respecting ignoreInactiveChildren
    List<RectTransform> GatherChildren(RectTransform container)
    {
        List<RectTransform> list = new List<RectTransform>();
        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (ignoreInactiveChildren && !child.gameObject.activeInHierarchy) continue;
            list.Add(child);
        }
        return list;
    }

    // Get child rectangle sizes (width, height) used for layout
    public List<Rect> GetChildRects(List<RectTransform> children)
    {
        List<Rect> rects = new List<Rect>(children.Count);
        for (int i = 0; i < children.Count; i++)
        {
            RectTransform r = children[i];
            float w = Mathf.Abs(r.rect.width);
            float h = Mathf.Abs(r.rect.height);

            // If LayoutElement exists and useChildPreferredSize is true prefer preferred sizes
            if (useChildPreferredSize)
            {
                var le = r.GetComponent<LayoutElement>();
                if (le != null)
                {
                    if (le.ignoreLayout == false)
                    {
                        if (le.preferredWidth > 0) w = le.preferredWidth;
                        if (le.preferredHeight > 0) h = le.preferredHeight;
                    }
                }
                else
                {
                    // try Text/ContentSizeFitter? We'll assume rect is fine
                }
            }

            if (uniformCellSize)
            {
                w = cellSize.x;
                h = cellSize.y;
            }

            rects.Add(new Rect(0, 0, w, h));
        }
        return rects;
    }

    // Core layout calculation: returns anchoredPosition for each child in container's local space.
    public List<Vector2> ComputeLayoutPositions(RectTransform container, List<Rect> childRects)
    {
        List<Vector2> positions = new List<Vector2>(childRects.Count);

        float containerWidth = container.rect.width;
        float containerHeight = container.rect.height;
        float xStart = paddingLeft;
        float yStart = -paddingTop; // Unity UI's Y goes up for anchoredPosition; anchoredPosition.y positive moves up. We'll treat top as negative offsets.

        // We'll position relative to container pivot (RectTransform anchoredPosition is relative to pivot).
        // First compute raw positions in 'unrotated local coordinates with pivot at (0,0) center' then translate to anchoredPosition.
        // Easier approach: compute positions with origin at top-left corner (local space where top-left = (-pivot.x*width, (1-pivot.y)*height) ), then convert.

        Vector2 topLeftLocal = new Vector2(-container.pivot.x * containerWidth, (1f - container.pivot.y) * containerHeight);

        // Table/grid flow: compute columns/rows
        bool isTable = horizontal && vertical;
        int cols = 1, rows = 1;

        if (isTable)
        {
            if (tableConstraint == Constraint.FixedColumnCount)
            {
                cols = Mathf.Max(1, constraintCount);
                rows = Mathf.CeilToInt((float)childRects.Count / cols);
            }
            else if (tableConstraint == Constraint.FixedRowCount)
            {
                rows = Mathf.Max(1, constraintCount);
                cols = Mathf.CeilToInt((float)childRects.Count / rows);
            }
            else // Flexible -> try to wrap by container width, fall back to single row
            {
                // naive wrapping: place horizontally until width exceeded
                List<float> rowWidths = new List<float>();
                cols = 0;
                float curW = 0;
                int curCols = 0;
                for (int i = 0; i < childRects.Count; i++)
                {
                    float w = childRects[i].width;
                    if (curCols == 0) curW = paddingLeft + w;
                    else curW += spacingX + w;
                    curCols++;
                    // if next would exceed container width, close row
                    bool last = (i == childRects.Count - 1);
                    bool exceed = !last && (curW + childRects[i + 1].width + paddingRight + spacingX > containerWidth);
                    if (exceed || last)
                    {
                        rowWidths.Add(curW);
                        cols = Math.Max(cols, curCols);
                        curCols = 0;
                        curW = 0;
                    }
                }
                if (rowWidths.Count == 0) rowWidths.Add(0);
                rows = rowWidths.Count;
            }
        }
        else
        {
            cols = horizontal ? childRects.Count : 1;
            rows = vertical ? childRects.Count : 1;
        }

        // For table/grid compute cell sizes per column/row if variable-sized: find max widths per column and heights per row
        float[] colWidths = new float[cols];
        float[] rowHeights = new float[rows];
        if (isTable)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int idx = r * cols + c;
                    if (idx >= childRects.Count) break;
                    colWidths[c] = Mathf.Max(colWidths[c], childRects[idx].width);
                    rowHeights[r] = Mathf.Max(rowHeights[r], childRects[idx].height);
                }
            }
        }
        else
        {
            // not table: colWidths each child width, rowHeights each child height
            // but we will still place sequentially
        }

        // total content size
        float totalWidth = 0, totalHeight = 0;
        if (isTable)
        {
            float cw = paddingLeft + paddingRight;
            for (int c = 0; c < cols; c++) cw += colWidths[c];
            cw += spacingX * Mathf.Max(0, cols - 1);
            totalWidth = cw;

            float ch = paddingTop + paddingBottom;
            for (int r = 0; r < rows; r++) ch += rowHeights[r];
            ch += spacingY * Mathf.Max(0, rows - 1);
            totalHeight = ch;
        }
        else
        {
            // single-axis flow
            if (horizontal)
            {
                float w = paddingLeft + paddingRight;
                for (int i = 0; i < childRects.Count; i++)
                {
                    if (i > 0) w += spacingX;
                    w += childRects[i].width + indentPerItem * i;
                }
                totalWidth = w;
                // height is max child height + padding
                float mh = 0;
                for (int i = 0; i < childRects.Count; i++) mh = Mathf.Max(mh, childRects[i].height);
                totalHeight = paddingTop + paddingBottom + mh;
            }
            else // vertical only
            {
                float h = paddingTop + paddingBottom;
                for (int i = 0; i < childRects.Count; i++)
                {
                    if (i > 0) h += spacingY;
                    h += childRects[i].height + indentPerItem * i;
                }
                totalHeight = h;
                float mw = 0;
                for (int i = 0; i < childRects.Count; i++) mw = Mathf.Max(mw, childRects[i].width);
                totalWidth = paddingLeft + paddingRight + mw;
            }
        }

        // We'll anchor content within container according to alignment settings
        // compute content origin (top-left) in local coordinates
        float originX = topLeftLocal.x;
        float originY = topLeftLocal.y;

        // horizontal alignment: Left / Center / Right
        if (alignX == AlignHorizontal.Left)
        {
            originX += paddingLeft;
        }
        else if (alignX == AlignHorizontal.Center)
        {
            originX += (containerWidth - totalWidth) * 0.5f + paddingLeft;
        }
        else // Right
        {
            originX += (containerWidth - totalWidth) + paddingLeft;
        }

        // vertical alignment: Top / Middle / Bottom
        if (alignY == AlignVertical.Top)
        {
            originY -= paddingTop;
        }
        else if (alignY == AlignVertical.Middle)
        {
            originY -= (containerHeight - totalHeight) * 0.5f + paddingTop;
        }
        else // Bottom
        {
            originY -= (containerHeight - totalHeight) + paddingTop;
        }

        // Fill positions:
        if (isTable)
        {
            // compute column x offsets and row y offsets
            float[] colX = new float[cols];
            float[] rowY = new float[rows];

            float curX = originX;
            for (int c = 0; c < cols; c++)
            {
                colX[c] = curX + colWidths[c] * 0.5f; // we'll align child center to cell center horizontally
                curX += colWidths[c] + spacingX;
            }

            float curY = originY;
            for (int r = 0; r < rows; r++)
            {
                // rowY[r] should be center Y of that row
                rowY[r] = curY - (rowHeights[r] * 0.5f);
                curY -= (rowHeights[r] + spacingY);
            }

            for (int i = 0; i < childRects.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                Vector2 center = new Vector2(colX[c], rowY[r]);
                // child pivot matters: we return anchoredPosition such that child's anchoredPosition = center considering child's pivot.
                RectTransform child = (RectTransform)transform.GetChild(i);
                Vector2 childPivot = child.pivot;
                // candidate anchored pos currently is center relative to container local; anchoredPosition uses pivot as reference (center).
                // This is acceptable: anchoredPosition is the position of the pivot in local space.
                positions.Add(center);
            }
        }
        else
        {
            if (horizontal)
            {
                float curX = originX;
                for (int i = 0; i < childRects.Count; i++)
                {
                    float w = childRects[i].width;
                    float h = childRects[i].height;
                    // place child center vertically according to alignY
                    float centerY = originY - (totalHeight - paddingTop - paddingBottom) * 0.5f;
                    // But better: compute based on alignY for single row:
                    if (alignY == AlignVertical.Top)
                        centerY = originY - h * 0.5f;
                    else if (alignY == AlignVertical.Middle)
                        centerY = topLeftLocal.y - (containerHeight * 0.5f);
                    else // Bottom
                        centerY = originY - (totalHeight - paddingBottom) + h * 0.5f;

                    Vector2 center = new Vector2(curX + w * 0.5f, centerY);
                    positions.Add(center);
                    curX += w + spacingX + indentPerItem;
                }
            }
            else // vertical
            {
                float curY = originY;
                for (int i = 0; i < childRects.Count; i++)
                {
                    float w = childRects[i].width;
                    float h = childRects[i].height;
                    // horizontal alignment within vertical list:
                    float centerX = originX + (totalWidth - paddingLeft - paddingRight) * 0.5f;
                    if (alignX == AlignHorizontal.Left)
                        centerX = originX + w * 0.5f;
                    else if (alignX == AlignHorizontal.Center)
                        centerX = topLeftLocal.x + (containerWidth * 0.5f);
                    else // Right
                        centerX = originX + (totalWidth - paddingRight) - w * 0.5f;

                    Vector2 center = new Vector2(centerX, curY - h * 0.5f);
                    positions.Add(center);
                    curY -= h + spacingY + indentPerItem;
                }
            }
        }

        // Apply rotation around container pivot if needed:
        if (Mathf.Abs(rotationDegrees) > 0.0001f)
        {
            float rad = rotationDegrees * Mathf.Deg2Rad;
            Vector2 pivotLocal = Vector2.zero; // container pivot in local coordinates is (0,0)
            // However anchoredPosition is relative to container local origin. We want to rotate positions around the container's pivot point
            // container pivot local coordinates = ( (0 - container.pivot.x*width), (0 + (1-pivot.y)*height) )? Actually we used topLeftLocal earlier.
            // Simpler: compute pivot point in local coords at container.TransformPoint(Vector3.zero)?? anchoredPosition is local; the pivot point in local coords is zero for anchoredPosition positions (pivot of parent). So rotate around Vector2.zero.
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2 p = positions[i];
                float x = p.x;
                float y = p.y;
                float rx = x * Mathf.Cos(rad) - y * Mathf.Sin(rad);
                float ry = x * Mathf.Sin(rad) + y * Mathf.Cos(rad);
                positions[i] = new Vector2(rx, ry);
            }
        }

        return positions;
    }

    // Editor hook so layout can be previewed live if autoApplyInEditor is set
    void OnValidate()
    {
#if UNITY_EDITOR
        if (autoApplyInEditor && !Application.isPlaying)
        {
            // apply layout in editor (do not register undo)
            ApplyLayout();
            // Force scene repaint
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    // small helper to compute child world rect corners (used by editor preview)
    public Rect GetChildRectInContainerSpace(RectTransform child)
    {
        return new Rect(0, 0, Mathf.Abs(child.rect.width), Mathf.Abs(child.rect.height));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UIListLayout))]
public class UIListLayoutEditor : Editor
{
    UIListLayout t;
    SerializedProperty horizontal, vertical;
    SerializedProperty spacingX, spacingY, indentPerItem, paddingLeft, paddingRight, paddingTop, paddingBottom;
    SerializedProperty tableConstraint, constraintCount, uniformCellSize, cellSize;
    SerializedProperty useChildPreferredSize, alignX, alignY;
    SerializedProperty rotationDegrees, rotateChildrenWithList;
    SerializedProperty showPreview, previewAlpha, previewColor, autoApplyInEditor;
    SerializedProperty setSizeDeltaToChild, ignoreInactiveChildren;

    void OnEnable()
    {
        t = (UIListLayout)target;
        horizontal = serializedObject.FindProperty("horizontal");
        vertical = serializedObject.FindProperty("vertical");
        spacingX = serializedObject.FindProperty("spacingX");
        spacingY = serializedObject.FindProperty("spacingY");
        indentPerItem = serializedObject.FindProperty("indentPerItem");
        paddingLeft = serializedObject.FindProperty("paddingLeft");
        paddingRight = serializedObject.FindProperty("paddingRight");
        paddingTop = serializedObject.FindProperty("paddingTop");
        paddingBottom = serializedObject.FindProperty("paddingBottom");
        tableConstraint = serializedObject.FindProperty("tableConstraint");
        constraintCount = serializedObject.FindProperty("constraintCount");
        uniformCellSize = serializedObject.FindProperty("uniformCellSize");
        cellSize = serializedObject.FindProperty("cellSize");
        useChildPreferredSize = serializedObject.FindProperty("useChildPreferredSize");
        alignX = serializedObject.FindProperty("alignX");
        alignY = serializedObject.FindProperty("alignY");
        rotationDegrees = serializedObject.FindProperty("rotationDegrees");
        rotateChildrenWithList = serializedObject.FindProperty("rotateChildrenWithList");
        showPreview = serializedObject.FindProperty("showPreview");
        previewAlpha = serializedObject.FindProperty("previewAlpha");
        previewColor = serializedObject.FindProperty("previewColor");
        autoApplyInEditor = serializedObject.FindProperty("autoApplyInEditor");
        setSizeDeltaToChild = serializedObject.FindProperty("setSizeDeltaToChild");
        ignoreInactiveChildren = serializedObject.FindProperty("ignoreInactiveChildren");
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(horizontal);
        EditorGUILayout.PropertyField(vertical);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Spacing & Padding", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spacingX);
        EditorGUILayout.PropertyField(spacingY);
        EditorGUILayout.PropertyField(indentPerItem);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(paddingLeft);
        EditorGUILayout.PropertyField(paddingRight);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(paddingTop);
        EditorGUILayout.PropertyField(paddingBottom);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Table Settings (when both H+V)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tableConstraint);
        if ((UIListLayout.Constraint)tableConstraint.enumValueIndex != UIListLayout.Constraint.Flexible)
            EditorGUILayout.PropertyField(constraintCount);
        EditorGUILayout.PropertyField(uniformCellSize);
        if (uniformCellSize.boolValue) EditorGUILayout.PropertyField(cellSize);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sizing & Alignment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useChildPreferredSize);
        EditorGUILayout.PropertyField(alignX);
        EditorGUILayout.PropertyField(alignY);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rotationDegrees);
        EditorGUILayout.PropertyField(rotateChildrenWithList);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor / Preview", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showPreview);
        EditorGUILayout.Slider(previewAlpha, 0f, 1f, new GUIContent("Preview Alpha"));
        EditorGUILayout.PropertyField(previewColor);
        EditorGUILayout.PropertyField(autoApplyInEditor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Apply Behavior", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(setSizeDeltaToChild);
        EditorGUILayout.PropertyField(ignoreInactiveChildren);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply Layout"))
        {
            Undo.RecordObject(t, "Apply UIListLayout");
            t.ApplyLayout();
            EditorUtility.SetDirty(t);
        }
        if (GUILayout.Button("Repaint Preview"))
        {
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI(SceneView sv)
    {
        if (t == null) return;
        if (!t.showPreview) return;
        // compute layout preview
        RectTransform container = t.transform as RectTransform;
        if (container == null) return;

        // Gather children (mirror same logic)
        List<RectTransform> children = new List<RectTransform>();
        for (int i = 0; i < container.childCount; i++)
        {
            var ch = container.GetChild(i) as RectTransform;
            if (ch == null) continue;
            if (t.ignoreInactiveChildren && !ch.gameObject.activeInHierarchy) continue;
            children.Add(ch);
        }
        if (children.Count == 0) return;

        // compute child rects and positions via the component's methods
        var childRects = t.GetChildRects(children);
        var positions = t.ComputeLayoutPositions(container, childRects);

        // Draw translucent rectangles in world space
        Handles.BeginGUI();
        Color fill = t.previewColor;
        fill.a = t.previewAlpha;
        Color outline = t.previewColor;
        outline.a = Mathf.Clamp01(t.previewAlpha + 0.2f);

        for (int i = 0; i < children.Count; i++)
        {
            RectTransform child = children[i];
            Vector2 posLocal = positions[i]; // anchoredPosition candidate (pivot point)
            // compute corners of the child's rect in container local space using child's pivot and size
            Vector2 size = new Vector2(childRects[i].width, childRects[i].height);
            Vector2 pivot = child.pivot;
            // compute bottom-left local point of rect (in container local space):
            Vector2 center = posLocal;
            Vector2 bottomLeft = center + new Vector2(-size.x * (0.5f - (pivot.x - 0.5f)), -size.y * (0.5f - (pivot.y - 0.5f)));
            // produce 4 corners in container local space
            Vector3[] corners = new Vector3[4];
            corners[0] = new Vector3(bottomLeft.x, bottomLeft.y); // bl
            corners[1] = new Vector3(bottomLeft.x + size.x, bottomLeft.y); // br
            corners[2] = new Vector3(bottomLeft.x + size.x, bottomLeft.y + size.y); // tr
            corners[3] = new Vector3(bottomLeft.x, bottomLeft.y + size.y); // tl

            // apply rotation around container pivot if needed:
            if (Mathf.Abs(t.rotationDegrees) > 0.001f)
            {
                float rad = t.rotationDegrees * Mathf.Deg2Rad;
                for (int c = 0; c < 4; c++)
                {
                    float x = corners[c].x;
                    float y = corners[c].y;
                    float rx = x * Mathf.Cos(rad) - y * Mathf.Sin(rad);
                    float ry = x * Mathf.Sin(rad) + y * Mathf.Cos(rad);
                    corners[c] = new Vector3(rx, ry);
                }
            }

            // transform container-local corners to world, then to GUI (screen) coords
            Vector2[] guiPts2 = new Vector2[4];
            for (int c = 0; c < 4; c++)
            {
                Vector3 world = container.TransformPoint(corners[c]);
                Vector2 screenP = HandleUtility.WorldToGUIPoint(world);
                guiPts2[c] = screenP;
            }

            // Convert Vector2[] -> Vector3[] for Handles API
            Vector3[] guiPts3 = new Vector3[guiPts2.Length];
            for (int k = 0; k < guiPts2.Length; k++)
                guiPts3[k] = new Vector3(guiPts2[k].x, guiPts2[k].y, 0f);

            // Draw. Use DrawSolidRectangleWithOutline if available, otherwise fallback to convex polygon
            #if UNITY_2021_1_OR_NEWER
            Handles.DrawSolidRectangleWithOutline(guiPts3, fill, outline);
            #else
            // older versions may not have the Vector3[] overload; try AA convex polygon + outline
            Handles.DrawAAConvexPolygon(guiPts3);
            Handles.DrawPolyLine(guiPts3[0], guiPts3[1], guiPts3[2], guiPts3[3], guiPts3[0]);
            #endif
        }

        Handles.EndGUI();
    }
}
#endif
