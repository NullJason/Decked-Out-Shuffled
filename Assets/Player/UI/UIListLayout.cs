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

    [Header("Sorting & Direction")]
    public bool sortByPriority = false;
    public enum LayoutDirection { LeftToRight, RightToLeft, TopToBottom, BottomToTop }
    public LayoutDirection layoutDirection = LayoutDirection.LeftToRight;
    
    [Header("Container Scaling")]
    public bool scaleHorizontal = false; // Adjust container width to fit content
    public bool scaleVertical = false;   // Adjust container height to fit content
    public float minWidth = 0f;          // Minimum width when scaling horizontally
    public float minHeight = 0f;         // Minimum height when scaling vertically
    public float maxWidth = 10000f;      // Maximum width when scaling horizontally  
    public float maxHeight = 10000f;     // Maximum height when scaling vertically
    
    [System.Flags]
    public enum ScaleDirection
    {
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
        Center = 16
    }
    public ScaleDirection scaleDirection = ScaleDirection.Center;

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

    // Child tracking for auto-updates
    private int lastChildCount = 0;
    private List<Transform> trackedChildren = new List<Transform>();
    private Vector3 lastContainerPosition;

    void Start()
    {
        UpdateChildTracking();
        ApplyLayout();
    }

    void OnEnable()
    {
        UpdateChildTracking();
        lastContainerPosition = transform.localPosition;
        ApplyLayout();
    }

    void Update()
    {
        // Check for new children in runtime
        if (transform.childCount != lastChildCount)
        {
            UpdateChildTracking();
            ApplyLayout();
        }
    }

    void OnTransformChildrenChanged()
    {
        // This is called when children are added/removed in the editor
        UpdateChildTracking();
        
#if UNITY_EDITOR
        if (autoApplyInEditor && !Application.isPlaying)
        {
            // Small delay to ensure new child is properly initialized
            EditorApplication.delayCall += () => {
                if (this != null) ApplyLayout();
            };
        }
#endif
    }

    void UpdateChildTracking()
    {
        lastChildCount = transform.childCount;
        trackedChildren.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            trackedChildren.Add(transform.GetChild(i));
        }
    }

    // ========== Public API ==========
    public void ApplyLayout()
    {
        RectTransform container = transform as RectTransform;
        if (container == null) return;

        List<RectTransform> children = GatherChildren(container);
        if (children.Count == 0) return;

        Vector3 originalPosition = container.localPosition;
        List<Rect> childRects = GetChildRects(children);
        Vector2 requiredSize = CalculateRequiredSize(container, childRects);
        ApplyContainerScaling(container, requiredSize, originalPosition);
        List<Vector2> positions = ComputeLayoutPositions(container, childRects);

        for (int i = 0; i < children.Count; i++)
        {
            RectTransform child = children[i];
            Vector2 pos = positions[i];

            child.anchoredPosition = pos;

            if (rotateChildrenWithList)
            {
                Vector3 e = child.localEulerAngles;
                e.z = rotationDegrees;
                child.localEulerAngles = e;
            }

            if (uniformCellSize && setSizeDeltaToChild)
            {
                Vector2 size = cellSize;
                child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            }
        }
        
        lastContainerPosition = container.localPosition;
    }

    // Calculate the required size for the container based on content
    private Vector2 CalculateRequiredSize(RectTransform container, List<Rect> childRects)
    {
        if (childRects.Count == 0) return new Vector2(container.rect.width, container.rect.height);

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
            else // Flexible -> try to wrap by container width
            {
                float availableWidth = container.rect.width - paddingLeft - paddingRight;
                cols = CalculateFlexibleColumns(childRects, availableWidth);
                rows = Mathf.CeilToInt((float)childRects.Count / cols);
            }
        }
        else
        {
            cols = horizontal ? childRects.Count : 1;
            rows = vertical ? childRects.Count : 1;
        }

        // Calculate column widths and row heights for table layout
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

        // Calculate total required size
        float requiredWidth = 0f;
        float requiredHeight = 0f;

        if (isTable)
        {
            // Table layout size calculation
            requiredWidth = paddingLeft + paddingRight;
            for (int c = 0; c < cols; c++) 
                requiredWidth += colWidths[c];
            requiredWidth += spacingX * Mathf.Max(0, cols - 1);

            requiredHeight = paddingTop + paddingBottom;
            for (int r = 0; r < rows; r++) 
                requiredHeight += rowHeights[r];
            requiredHeight += spacingY * Mathf.Max(0, rows - 1);
        }
        else
        {
            // Single-axis layout size calculation
            if (horizontal)
            {
                requiredWidth = paddingLeft + paddingRight;
                for (int i = 0; i < childRects.Count; i++)
                {
                    if (i > 0) requiredWidth += spacingX;
                    requiredWidth += childRects[i].width + indentPerItem * i;
                }
                
                // Height is max child height plus padding
                float maxChildHeight = 0f;
                for (int i = 0; i < childRects.Count; i++) 
                    maxChildHeight = Mathf.Max(maxChildHeight, childRects[i].height);
                requiredHeight = paddingTop + paddingBottom + maxChildHeight;
            }
            else // vertical only
            {
                requiredHeight = paddingTop + paddingBottom;
                for (int i = 0; i < childRects.Count; i++)
                {
                    if (i > 0) requiredHeight += spacingY;
                    requiredHeight += childRects[i].height + indentPerItem * i;
                }
                
                // Width is max child width plus padding
                float maxChildWidth = 0f;
                for (int i = 0; i < childRects.Count; i++) 
                    maxChildWidth = Mathf.Max(maxChildWidth, childRects[i].width);
                requiredWidth = paddingLeft + paddingRight + maxChildWidth;
            }
        }

        return new Vector2(requiredWidth, requiredHeight);
    }

    // Calculate flexible columns for table layout
    private int CalculateFlexibleColumns(List<Rect> childRects, float availableWidth)
    {
        if (childRects.Count == 0) return 1;
        
        int maxCols = childRects.Count;
        int bestCols = 1;
        
        for (int testCols = 1; testCols <= maxCols; testCols++)
        {
            int testRows = Mathf.CeilToInt((float)childRects.Count / testCols);
            
            // Calculate required width for this column configuration
            float[] colWidths = new float[testCols];
            for (int r = 0; r < testRows; r++)
            {
                for (int c = 0; c < testCols; c++)
                {
                    int idx = r * testCols + c;
                    if (idx >= childRects.Count) break;
                    colWidths[c] = Mathf.Max(colWidths[c], childRects[idx].width);
                }
            }
            
            float totalWidth = paddingLeft + paddingRight;
            for (int c = 0; c < testCols; c++) 
                totalWidth += colWidths[c];
            totalWidth += spacingX * Mathf.Max(0, testCols - 1);
            
            // If this fits or it's the first iteration, use it
            if (totalWidth <= availableWidth || testCols == 1)
            {
                bestCols = testCols;
            }
            else
            {
                // Once we exceed available width, use the previous configuration
                break;
            }
        }
        
        return bestCols;
    }

    // Apply container scaling based on calculated required size
    private void ApplyContainerScaling(RectTransform container, Vector2 requiredSize, Vector3 originalPosition)
    {
        Vector2 newSize = container.rect.size;
        Vector3 newPosition = container.localPosition;
        bool sizeChanged = false;
        bool positionChanged = false;

        if (scaleHorizontal)
        {
            float newWidth = Mathf.Clamp(requiredSize.x, minWidth, maxWidth);
            if (Mathf.Abs(newWidth - container.rect.width) > 0.01f)
            {
                float widthDelta = newWidth - container.rect.width;
                
                // Apply scaling direction
                if (scaleDirection.HasFlag(ScaleDirection.Center))
                {
                    // Center scaling - adjust position to maintain center
                    newPosition.x = originalPosition.x - widthDelta * 0.5f * container.pivot.x;
                }
                else
                {
                    // Directional scaling
                    if (scaleDirection.HasFlag(ScaleDirection.Left) && !scaleDirection.HasFlag(ScaleDirection.Right))
                    {
                        // Scale to left - move left by widthDelta
                        newPosition.x = originalPosition.x - widthDelta * (1f - container.pivot.x);
                    }
                    else if (scaleDirection.HasFlag(ScaleDirection.Right) && !scaleDirection.HasFlag(ScaleDirection.Left))
                    {
                        // Scale to right - position unchanged
                        newPosition.x = originalPosition.x + widthDelta * container.pivot.x;
                    }
                    else if (scaleDirection.HasFlag(ScaleDirection.Left) && scaleDirection.HasFlag(ScaleDirection.Right))
                    {
                        // Scale both directions - maintain center
                        newPosition.x = originalPosition.x - widthDelta * 0.5f * container.pivot.x;
                    }
                }
                
                newSize.x = newWidth;
                sizeChanged = true;
                positionChanged = true;
            }
        }

        if (scaleVertical)
        {
            float newHeight = Mathf.Clamp(requiredSize.y, minHeight, maxHeight);
            if (Mathf.Abs(newHeight - container.rect.height) > 0.01f)
            {
                float heightDelta = newHeight - container.rect.height;
                
                // Apply scaling direction
                if (scaleDirection.HasFlag(ScaleDirection.Center))
                {
                    // Center scaling - adjust position to maintain center
                    newPosition.y = originalPosition.y - heightDelta * 0.5f * container.pivot.y;
                }
                else
                {
                    // Directional scaling
                    if (scaleDirection.HasFlag(ScaleDirection.Top) && !scaleDirection.HasFlag(ScaleDirection.Bottom))
                    {
                        // Scale to top - move up by heightDelta
                        newPosition.y = originalPosition.y + heightDelta * (1f - container.pivot.y);
                    }
                    else if (scaleDirection.HasFlag(ScaleDirection.Bottom) && !scaleDirection.HasFlag(ScaleDirection.Top))
                    {
                        // Scale to bottom - position unchanged
                        newPosition.y = originalPosition.y - heightDelta * container.pivot.y;
                    }
                    else if (scaleDirection.HasFlag(ScaleDirection.Top) && scaleDirection.HasFlag(ScaleDirection.Bottom))
                    {
                        // Scale both directions - maintain center
                        newPosition.y = originalPosition.y - heightDelta * 0.5f * container.pivot.y;
                    }
                }
                
                newSize.y = newHeight;
                sizeChanged = true;
                positionChanged = true;
            }
        }

        if (sizeChanged)
        {
            container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
            container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);
            
            if (positionChanged)
            {
                container.localPosition = newPosition;
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
        if (sortByPriority)
        {
            list = SortChildrenByPriority(list);
        }

        list = ApplyLayoutDirection(list);
        
        return list;
    }
    private List<RectTransform> SortChildrenByPriority(List<RectTransform> children)
    {
        var prioritizedChildren = new List<(RectTransform transform, int priority)>();
        
        foreach (var child in children)
        {
            var priorityComponent = child.GetComponent<ListPriority>();
            int priority = 0;
            
            if (priorityComponent != null)
            {
                priority = priorityComponent.SortOrder;
            }
            else
            {
                priorityComponent = child.gameObject.AddComponent<ListPriority>();
                priorityComponent.SortOrder = 0;
            }
            
            prioritizedChildren.Add((child, priority));
        }
        
        prioritizedChildren.Sort((a, b) => b.priority.CompareTo(a.priority));
        
        var sortedList = new List<RectTransform>();
        foreach (var item in prioritizedChildren)
        {
            sortedList.Add(item.transform);
        }
        
        return sortedList;
    }

    private List<RectTransform> ApplyLayoutDirection(List<RectTransform> children)
    {
        bool shouldReverse = false;

        if (horizontal && !vertical)
        {
            shouldReverse = (layoutDirection == LayoutDirection.RightToLeft);
        }
        else if (vertical && !horizontal)
        {
            shouldReverse = (layoutDirection == LayoutDirection.BottomToTop);
        }
        else if (horizontal && vertical)
        {
            shouldReverse = (layoutDirection == LayoutDirection.RightToLeft);
        }

        if (shouldReverse)
        {
            children.Reverse();
        }

        return children;
    }
    [ContextMenu("Add ListPriority to All Children")]
    public void AddListPriorityToAllChildren()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<ListPriority>() == null)
            {
                child.gameObject.AddComponent<ListPriority>();
            }
        }
        
    #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    #endif
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
        
        // Calculate available space between padding
        float availableWidth = containerWidth - paddingLeft - paddingRight;
        float availableHeight = containerHeight - paddingTop - paddingBottom;

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
            else // Flexible -> try to wrap by container width
            {
                cols = CalculateFlexibleColumns(childRects, availableWidth);
                rows = Mathf.CeilToInt((float)childRects.Count / cols);
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

        // FIXED ALIGNMENT: Calculate content origin properly accounting for container pivot
        Vector2 pivotOffset = new Vector2(
            -container.pivot.x * containerWidth,
            (1f - container.pivot.y) * containerHeight
        );

        // Calculate starting position based on alignment
        float startX = pivotOffset.x;
        float startY = pivotOffset.y;

        // Horizontal alignment
        if (alignX == AlignHorizontal.Left)
        {
            startX += paddingLeft;
        }
        else if (alignX == AlignHorizontal.Center)
        {
            startX += (containerWidth - totalWidth) * 0.5f;
        }
        else // Right
        {
            startX += containerWidth - totalWidth - paddingRight;
        }

        // Vertical alignment
        if (alignY == AlignVertical.Top)
        {
            startY -= paddingTop;
        }
        else if (alignY == AlignVertical.Middle)
        {
            startY -= (containerHeight - totalHeight) * 0.5f;
        }
        else // Bottom
        {
            startY -= containerHeight - totalHeight - paddingBottom;
        }

        // Fill positions:
        if (isTable)
        {
            // compute column x offsets and row y offsets
            float[] colX = new float[cols];
            float[] rowY = new float[rows];

            float curX = startX;
            for (int c = 0; c < cols; c++)
            {
                colX[c] = curX + colWidths[c] * 0.5f;
                curX += colWidths[c] + spacingX;
            }

            float curY = startY;
            for (int r = 0; r < rows; r++)
            {
                rowY[r] = curY - rowHeights[r] * 0.5f;
                curY -= (rowHeights[r] + spacingY);
            }

            for (int i = 0; i < childRects.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                Vector2 center = new Vector2(colX[c], rowY[r]);
                positions.Add(center);
            }
        }
        else
        {
            if (horizontal)
            {
                float curX = startX;
                for (int i = 0; i < childRects.Count; i++)
                {
                    float w = childRects[i].width;
                    float h = childRects[i].height;
                    
                    // Calculate Y position based on vertical alignment
                    float centerY = startY;
                    if (alignY == AlignVertical.Top)
                        centerY -= h * 0.5f;
                    else if (alignY == AlignVertical.Middle)
                        centerY -= containerHeight * 0.5f;
                    else // Bottom
                        centerY -= containerHeight - h * 0.5f;

                    Vector2 center = new Vector2(curX + w * 0.5f, centerY);
                    positions.Add(center);
                    curX += w + spacingX + indentPerItem;
                }
            }
            else // vertical
            {
                float curY = startY;
                for (int i = 0; i < childRects.Count; i++)
                {
                    float w = childRects[i].width;
                    float h = childRects[i].height;
                    
                    // Calculate X position based on horizontal alignment
                    float centerX = startX;
                    if (alignX == AlignHorizontal.Left)
                        centerX += w * 0.5f;
                    else if (alignX == AlignHorizontal.Center)
                        centerX += containerWidth * 0.5f;
                    else // Right
                        centerX += containerWidth - w * 0.5f;

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
            Vector2 pivot = new Vector2(-container.pivot.x * containerWidth, (1f - container.pivot.y) * containerHeight);
            
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2 p = positions[i] - pivot;
                float x = p.x;
                float y = p.y;
                float rx = x * Mathf.Cos(rad) - y * Mathf.Sin(rad);
                float ry = x * Mathf.Sin(rad) + y * Mathf.Cos(rad);
                positions[i] = new Vector2(rx, ry) + pivot;
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
            UpdateChildTracking();
            // Use delay call to ensure all properties are set
            EditorApplication.delayCall += () => {
                if (this != null) ApplyLayout();
            };
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
    SerializedProperty scaleHorizontal, scaleVertical, minWidth, minHeight, maxWidth, maxHeight, scaleDirection;
    SerializedProperty rotationDegrees, rotateChildrenWithList;
    SerializedProperty showPreview, previewAlpha, previewColor, autoApplyInEditor;
    SerializedProperty setSizeDeltaToChild, ignoreInactiveChildren;
    SerializedProperty sortByPriority, layoutDirection;

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
        scaleHorizontal = serializedObject.FindProperty("scaleHorizontal");
        scaleVertical = serializedObject.FindProperty("scaleVertical");
        minWidth = serializedObject.FindProperty("minWidth");
        minHeight = serializedObject.FindProperty("minHeight");
        maxWidth = serializedObject.FindProperty("maxWidth");
        maxHeight = serializedObject.FindProperty("maxHeight");
        scaleDirection = serializedObject.FindProperty("scaleDirection");
        rotationDegrees = serializedObject.FindProperty("rotationDegrees");
        rotateChildrenWithList = serializedObject.FindProperty("rotateChildrenWithList");
        showPreview = serializedObject.FindProperty("showPreview");
        previewAlpha = serializedObject.FindProperty("previewAlpha");
        previewColor = serializedObject.FindProperty("previewColor");
        autoApplyInEditor = serializedObject.FindProperty("autoApplyInEditor");
        setSizeDeltaToChild = serializedObject.FindProperty("setSizeDeltaToChild");
        ignoreInactiveChildren = serializedObject.FindProperty("ignoreInactiveChildren");
        
        sortByPriority = serializedObject.FindProperty("sortByPriority");
        layoutDirection = serializedObject.FindProperty("layoutDirection");

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        if (t == null) return;
        if (!t.showPreview) return;
        
        RectTransform container = t.transform as RectTransform;
        if (container == null) return;

        List<RectTransform> children = new List<RectTransform>();
        for (int i = 0; i < container.childCount; i++)
        {
            var ch = container.GetChild(i) as RectTransform;
            if (ch == null) continue;
            if (t.ignoreInactiveChildren && !ch.gameObject.activeInHierarchy) continue;
            children.Add(ch);
        }
        if (children.Count == 0) return;

        var childRects = t.GetChildRects(children);
        var positions = t.ComputeLayoutPositions(container, childRects);

        Handles.BeginGUI();
        Color fill = t.previewColor;
        fill.a = t.previewAlpha;
        Color outline = t.previewColor;
        outline.a = Mathf.Clamp01(t.previewAlpha + 0.2f);

        for (int i = 0; i < children.Count; i++)
        {
            RectTransform child = children[i];
            Vector2 posLocal = positions[i];
            Vector2 size = new Vector2(childRects[i].width, childRects[i].height);
            Vector2 pivot = child.pivot;
            
            Vector2 center = posLocal;
            Vector2 bottomLeft = center + new Vector2(-size.x * pivot.x, -size.y * pivot.y);
            
            Vector2[] corners = new Vector2[4];
            corners[0] = new Vector2(bottomLeft.x, bottomLeft.y);
            corners[1] = new Vector2(bottomLeft.x + size.x, bottomLeft.y);
            corners[2] = new Vector2(bottomLeft.x + size.x, bottomLeft.y + size.y);
            corners[3] = new Vector2(bottomLeft.x, bottomLeft.y + size.y);

            if (Mathf.Abs(t.rotationDegrees) > 0.001f)
            {
                float rad = t.rotationDegrees * Mathf.Deg2Rad;
                Vector2 containerPivot = new Vector2(-container.pivot.x * container.rect.width, (1f - container.pivot.y) * container.rect.height);
                
                for (int c = 0; c < 4; c++)
                {
                    Vector2 p = corners[c] - containerPivot;
                    float x = p.x;
                    float y = p.y;
                    float rx = x * Mathf.Cos(rad) - y * Mathf.Sin(rad);
                    float ry = x * Mathf.Sin(rad) + y * Mathf.Cos(rad);
                    corners[c] = new Vector2(rx, ry) + containerPivot;
                }
            }

            Vector2[] guiPts2 = new Vector2[4];
            for (int c = 0; c < 4; c++)
            {
                Vector3 world = container.TransformPoint(corners[c]);
                Vector2 screenP = HandleUtility.WorldToGUIPoint(world);
                guiPts2[c] = screenP;
            }

            Vector3[] guiPts3 = new Vector3[guiPts2.Length];
            for (int k = 0; k < guiPts2.Length; k++)
                guiPts3[k] = new Vector3(guiPts2[k].x, guiPts2[k].y, 0f);

            #if UNITY_2021_1_OR_NEWER
            Handles.DrawSolidRectangleWithOutline(guiPts3, fill, outline);
            #else
            Handles.DrawAAConvexPolygon(guiPts3);
            Handles.DrawPolyLine(guiPts3[0], guiPts3[1], guiPts3[2], guiPts3[3], guiPts3[0]);
            #endif
        }

        Handles.EndGUI();
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

        if (t.horizontal && t.vertical)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Table Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tableConstraint);
            if ((UIListLayout.Constraint)tableConstraint.enumValueIndex != UIListLayout.Constraint.Flexible)
                EditorGUILayout.PropertyField(constraintCount);
            EditorGUILayout.PropertyField(uniformCellSize);
            if (uniformCellSize.boolValue)
                EditorGUILayout.PropertyField(cellSize);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sizing & Alignment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useChildPreferredSize);
        EditorGUILayout.PropertyField(alignX);
        EditorGUILayout.PropertyField(alignY);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Container Scaling", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(scaleHorizontal);
        EditorGUILayout.PropertyField(scaleVertical);

        if (t.scaleHorizontal || t.scaleVertical)
        {
            // Scale direction with smart selection logic
            UIListLayout.ScaleDirection currentDirection = (UIListLayout.ScaleDirection)scaleDirection.intValue;
            UIListLayout.ScaleDirection newDirection = currentDirection;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Scale Direction", EditorStyles.miniBoldLabel);

            bool centerSelected = (currentDirection & UIListLayout.ScaleDirection.Center) != 0;
            bool newCenterSelected = EditorGUILayout.Toggle("Center", centerSelected);

            if (newCenterSelected && !centerSelected)
            {
                // Center selected - deselect all others
                newDirection = UIListLayout.ScaleDirection.Center;
            }
            else if (!newCenterSelected && centerSelected)
            {
                // Center deselected - select nothing
                newDirection = 0;
            }

            if (!newCenterSelected)
            {
                EditorGUI.indentLevel++;

                bool leftSelected = (currentDirection & UIListLayout.ScaleDirection.Left) != 0;
                bool rightSelected = (currentDirection & UIListLayout.ScaleDirection.Right) != 0;
                bool topSelected = (currentDirection & UIListLayout.ScaleDirection.Top) != 0;
                bool bottomSelected = (currentDirection & UIListLayout.ScaleDirection.Bottom) != 0;

                EditorGUILayout.BeginHorizontal();
                bool newLeftSelected = EditorGUILayout.ToggleLeft("Left", leftSelected, GUILayout.Width(60));
                bool newRightSelected = EditorGUILayout.ToggleLeft("Right", rightSelected, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                bool newTopSelected = EditorGUILayout.ToggleLeft("Top", topSelected, GUILayout.Width(60));
                bool newBottomSelected = EditorGUILayout.ToggleLeft("Bottom", bottomSelected, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                // Update direction based on selections
                newDirection = 0;
                if (newLeftSelected) newDirection |= UIListLayout.ScaleDirection.Left;
                if (newRightSelected) newDirection |= UIListLayout.ScaleDirection.Right;
                if (newTopSelected) newDirection |= UIListLayout.ScaleDirection.Top;
                if (newBottomSelected) newDirection |= UIListLayout.ScaleDirection.Bottom;

                // If all horizontal and vertical directions are selected, automatically select Center
                if ((newLeftSelected && newRightSelected && t.scaleHorizontal) ||
                    (newTopSelected && newBottomSelected && t.scaleVertical))
                {
                    newDirection = UIListLayout.ScaleDirection.Center;
                }

                EditorGUI.indentLevel--;
            }

            scaleDirection.intValue = (int)newDirection;
            EditorGUILayout.EndVertical();

            if (t.scaleHorizontal)
            {
                EditorGUILayout.PropertyField(minWidth);
                EditorGUILayout.PropertyField(maxWidth);
            }

            if (t.scaleVertical)
            {
                EditorGUILayout.PropertyField(minHeight);
                EditorGUILayout.PropertyField(maxHeight);
            }
        }

        EditorGUILayout.Space();
        // EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Sorting & Direction", EditorStyles.boldLabel);
        if (sortByPriority != null)
        {
            EditorGUILayout.PropertyField(sortByPriority);
        }
        else
        {
            EditorGUILayout.HelpBox("SortByPriority property not found!", MessageType.Error);
        }
        if (t.sortByPriority)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("Children will be sorted by ListPriority component. Highest priority comes first.", MessageType.Info);
            EditorGUI.indentLevel--;
        }
        
        if (t.horizontal && !t.vertical)
        {
            EditorGUILayout.PropertyField(layoutDirection, new GUIContent("Horizontal Direction"));
        }
        else if (t.vertical && !t.horizontal)
        {
            EditorGUILayout.PropertyField(layoutDirection, new GUIContent("Vertical Direction"));
        }
        else if (t.horizontal && t.vertical)
        {
            EditorGUILayout.PropertyField(layoutDirection, new GUIContent("Flow Direction"));
        }


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
}
#endif