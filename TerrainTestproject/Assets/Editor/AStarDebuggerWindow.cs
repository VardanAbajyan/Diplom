using UnityEngine;
using UnityEditor;

public class AStarDebuggerWindow : EditorWindow
{
    private PathfindingGrid grid; // Заменили Grid на PathfindingGrid
    private Pathfinding pathfinding;
    private Texture2D mapTexture; 

    private bool flipX = false;
    private bool flipY = true; 

    [MenuItem("Window/RTS Debugger/Pathfinding Scanner")]
    public static void ShowWindow() => GetWindow<AStarDebuggerWindow>("Path Scanner");

    void OnInspectorUpdate()
    {
        Repaint();
    }

    void OnGUI()
    {
        GUILayout.Label("2D Pathfinding Map View", EditorStyles.boldLabel);

        if (grid == null) grid = FindFirstObjectByType<PathfindingGrid>();
        grid = (PathfindingGrid)EditorGUILayout.ObjectField("Grid Component", grid, typeof(PathfindingGrid), true);

        if (grid == null)
        {
            EditorGUILayout.HelpBox("Объект с компонентом PathfindingGrid не найден!", MessageType.Error);
            return;
        }

        pathfinding = grid.GetComponent<Pathfinding>();

        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        flipX = EditorGUILayout.ToggleLeft("Отразить по X", flipX, GUILayout.Width(100));
        flipY = EditorGUILayout.ToggleLeft("Отразить по Y", flipY, GUILayout.Width(100));
        
        if (EditorGUI.EndChangeCheck() && mapTexture != null)
        {
            GenerateMapTexture(); 
            SceneView.RepaintAll();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Scan & Refresh Map", GUILayout.Height(30)))
        {
            grid.CreateGrid();
            GenerateMapTexture();
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(10);
        
        Node[,] nodes = grid.GetGrid();

        if (nodes != null && nodes.Length > 0 && grid.gridSizeX > 0)
        {
            if (mapTexture == null) GenerateMapTexture();
            DrawMap();
        }
        else
        {
            string status = Application.isPlaying ? "Ожидание инициализации..." : "Нажмите Scan";
            EditorGUILayout.HelpBox($"Сетка пуста. {status}", MessageType.Info);
        }
    }

    void GenerateMapTexture()
    {
        Node[,] nodes = grid.GetGrid();
        if (nodes == null || grid.gridSizeX == 0 || grid.gridSizeY == 0) return;

        mapTexture = new Texture2D(grid.gridSizeX, grid.gridSizeY);
        mapTexture.filterMode = FilterMode.Point;

        Color walkableColor = new Color(0.15f, 0.15f, 0.15f); 
        Color obstacleColor = new Color(0.1f, 0.3f, 0.8f);    

        for (int x = 0; x < grid.gridSizeX; x++)
        {
            for (int y = 0; y < grid.gridSizeY; y++)
            {
                Node n = nodes[x, y];
                int texX = flipX ? (grid.gridSizeX - 1 - x) : x;
                int texY = flipY ? (grid.gridSizeY - 1 - y) : y; 
                
                if (n != null && !n.isWalkable)
                    mapTexture.SetPixel(texX, texY, obstacleColor);
                else
                    mapTexture.SetPixel(texX, texY, walkableColor);
            }
        }
        mapTexture.Apply(); 
    }

    void DrawMap()
    {
        float windowWidth = position.width - 20;
        Rect mapRect = GUILayoutUtility.GetRect(windowWidth, windowWidth);
        EditorGUI.DrawRect(mapRect, new Color(0.1f, 0.1f, 0.1f));

        if (mapTexture != null)
        {
            GUI.DrawTexture(mapRect, mapTexture, ScaleMode.StretchToFill);
        }

        // ИСПРАВЛЕНИЕ: Теперь мы рисуем путь, используя координаты напрямую
        if (pathfinding != null && pathfinding.debugPath != null && pathfinding.debugPath.Count > 1)
        {
            Handles.BeginGUI();
            Handles.color = Color.green; // Сделаем путь зеленым, чтобы лучше видеть
            for (int i = 0; i < pathfinding.debugPath.Count - 1; i++)
            {
                // Используем .worldPosition, так как в debugPath лежат объекты Node
                Vector2 p1 = WorldToMapPos(pathfinding.debugPath[i].worldPosition, mapRect);
                Vector2 p2 = WorldToMapPos(pathfinding.debugPath[i+1].worldPosition, mapRect);
                Handles.DrawAAPolyLine(4f, p1, p2);
            }
            Handles.EndGUI();
        }
    }

    Vector2 WorldToMapPos(Vector3 worldPos, Rect mapRect)
    {
        float worldSizeX = grid.gridSizeX * (grid.nodeRadius * 2);
        float worldSizeY = grid.gridSizeY * (grid.nodeRadius * 2);

        float percentX = (worldPos.x + worldSizeX / 2f) / worldSizeX;
        float percentY = (worldPos.z + worldSizeY / 2f) / worldSizeY;

        float finalPercentX = flipX ? (1 - Mathf.Clamp01(percentX)) : Mathf.Clamp01(percentX);
        float finalPercentY = flipY ? Mathf.Clamp01(percentY) : (1 - Mathf.Clamp01(percentY));

        return new Vector2(
            mapRect.x + finalPercentX * mapRect.width,
            mapRect.y + finalPercentY * mapRect.height
        );
    }
}