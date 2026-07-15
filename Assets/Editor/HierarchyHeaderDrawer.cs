using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HierarchyHeaderDrawer
{
    private const string HeaderPrefix = "# ";

    private static readonly Color HeaderColor =
        new Color32(0, 55, 50, 255);

    private static GUIStyle _headerStyle;

    static HierarchyHeaderDrawer()
    {
        // 防止重复订阅
        EditorApplication.hierarchyWindowItemOnGUI -= DrawHierarchyItem;
        EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyItem;
    }

    private static void DrawHierarchyItem(
        int instanceID,
        Rect selectionRect)
    {
        GameObject target =
            EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (target == null)
        {
            return;
        }

        if (!target.name.StartsWith(HeaderPrefix))
        {
            return;
        }

        // 此时已经进入 GUI 绘制阶段，可以安全访问 EditorStyles
        InitializeStyle();

        Rect backgroundRect = selectionRect;
        backgroundRect.x = 0;
        backgroundRect.width += selectionRect.x;

        EditorGUI.DrawRect(backgroundRect, HeaderColor);

        string headerName =
            target.name.Substring(HeaderPrefix.Length);

        GUI.Label(backgroundRect, headerName, _headerStyle);
    }

    private static void InitializeStyle()
    {
        if (_headerStyle != null)
        {
            return;
        }

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        _headerStyle.normal.textColor = Color.white;
    }
}