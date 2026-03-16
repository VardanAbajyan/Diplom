using UnityEngine;
using System.Collections.Generic;

public class RTSController : MonoBehaviour
{
    [Header("Layer Settings")]
    public LayerMask groundLayer; 
    public LayerMask unitLayer;   

    [Header("Selection Box Visuals")]
    public Texture2D selectionTexture; 

    public List<Unit> selectedUnits = new List<Unit>();

    [Header("Formation Settings")]
    public float spacing = 10f; 

    private Vector3 mouseStartPos;
    private bool isDragging = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseStartPos = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            SelectUnitsInBox();
            isDragging = false;
        }

        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0) GiveMoveCommand();
    }

    void SelectSingleUnit()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent(out Unit unit))
            {
                unit.SetSelected(true);
                if (!selectedUnits.Contains(unit)) selectedUnits.Add(unit);
            }
        }
    }

    void SelectUnitsInBox()
    {
        if (Vector3.Distance(mouseStartPos, Input.mousePosition) < 5f)
        {
            if (!Input.GetKey(KeyCode.LeftShift)) DeselectAll();
            SelectSingleUnit();
            return;
        }

        if (!Input.GetKey(KeyCode.LeftShift)) DeselectAll();

        Rect rect = GetScreenRect(mouseStartPos, Input.mousePosition);
        foreach (Unit unit in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (rect.Contains(new Vector2(screenPos.x, Screen.height - screenPos.y)))
            {
                unit.SetSelected(true);
                if (!selectedUnits.Contains(unit)) selectedUnits.Add(unit);
            }
        }
    }

    void GiveMoveCommand()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            int count = selectedUnits.Count;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count)); 
            int rows = Mathf.CeilToInt((float)count / cols);

            for (int i = 0; i < count; i++)
            {
                if (selectedUnits[i] == null) continue;

                float xOffset = (i % cols - (cols - 1) / 2f) * spacing;
                float zOffset = (i / cols - (rows - 1) / 2f) * spacing;

                selectedUnits[i].MoveTo(hit.point + new Vector3(xOffset, 0, zOffset));
            }
        }
    }

    void DeselectAll()
    {
        foreach (Unit unit in selectedUnits) 
        {
            if (unit != null) unit.SetSelected(false);
        }
        selectedUnits.Clear();
    }

    void OnGUI()
    {
        if (isDragging)
        {
            Rect rect = GetScreenRect(mouseStartPos, Input.mousePosition);
            DrawScreenRect(rect, new Color(0.5f, 1f, 0.5f, 0.2f)); 
            DrawScreenRectBorder(rect, 2, new Color(0.5f, 1f, 0.5f, 0.8f));
        }
    }

    Rect GetScreenRect(Vector3 p1, Vector3 p2)
    {
        p1.y = Screen.height - p1.y;
        p2.y = Screen.height - p2.y;
        Vector3 topLeft = Vector3.Min(p1, p2);
        Vector3 bottomRight = Vector3.Max(p1, p2);
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, selectionTexture);
        GUI.color = Color.white;
    }

    void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
    }
}