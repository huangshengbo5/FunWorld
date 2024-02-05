using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour))] // 替换成你要编辑的组件的类型
public class RectSelector : Editor
{
    private Vector3 startDragPosition;
    private Vector3 endDragPosition;
    private bool isSelecting = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Clear Selection"))
        {
            ClearSelection();
        }
    }

    private void OnSceneGUI()
    {
        Event currentEvent = Event.current;

        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                if (currentEvent.button == 0 && !isSelecting)
                {
                    startDragPosition = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition).origin;
                    isSelecting = true;
                    currentEvent.Use();
                }
                break;

            case EventType.MouseUp:
                if (currentEvent.button == 0 && isSelecting)
                {
                    endDragPosition = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition).origin;
                    isSelecting = false;
                    DrawSelection();
                    currentEvent.Use();
                }
                break;

            case EventType.MouseMove:
                HandleUtility.Repaint();
                break;
        }

        if (isSelecting)
        {
            endDragPosition = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition).origin;
            HandleUtility.Repaint();
        }
    }

    private void DrawSelection()
    {
        float width = Mathf.Abs(endDragPosition.x - startDragPosition.x);
        float height = Mathf.Abs(endDragPosition.y - startDragPosition.y);
        Rect selectionRect = new Rect(startDragPosition.x, startDragPosition.y, width, height);

        // Do something with the selectionRect, for example, highlight selected objects
        Debug.Log("Selected Rect: " + selectionRect);
    }

    private void ClearSelection()
    {
        // Implement logic to clear selection
        Debug.Log("Selection cleared");
    }
}