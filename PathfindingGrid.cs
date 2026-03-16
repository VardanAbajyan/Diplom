using UnityEngine;
using System.Collections.Generic;

public class PathfindingGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2 gridWorldSize;
    public float nodeRadius;

    [Header("Layers and Obstacles")]
    public LayerMask groundMask;
    public LayerMask unwalkableMask;
    [Tooltip("Layer exclusively for buildings, so tanks bypass them with a margin")]
    public LayerMask buildingMask; 
    public float waterLevel = 87f;

    [Header("Safety Settings (Tank)")]
    public float safetyRadius = 4.0f;
    public float maxHeightDifference = 1.5f;

    [Header("Debugger Settings")]
    public bool drawGizmos = true;
    public float gizmoDrawDistance = 40f;

    [HideInInspector] public int gridSizeX, gridSizeY;
    Node[,] grid;
    float nodeDiameter;

    void Awake()
    {
        CreateGrid();
    }

    public Node[,] GetGrid() { return grid; }

    [ContextMenu("Generate Grid (Update)")]
    public void CreateGrid()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 flatPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                
                float surfaceHeight = 0f;
                bool isOnBridge = false;

                if (Physics.Raycast(new Vector3(flatPoint.x, 1000f, flatPoint.z), Vector3.down, out RaycastHit hit, Mathf.Infinity, groundMask))
                {
                    surfaceHeight = hit.point.y;
                    isOnBridge = !(hit.collider is TerrainCollider);
                }

                Vector3 worldPoint = new Vector3(flatPoint.x, surfaceHeight, flatPoint.z);
                bool isWalkable = true;

                if (surfaceHeight < waterLevel && !isOnBridge)
                {
                    isWalkable = false;
                }
                else if (!isOnBridge && safetyRadius > 0)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4f;
                        Vector3 checkPos = flatPoint + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * safetyRadius;
                        float neighborHeight = surfaceHeight;

                        if (Physics.Raycast(new Vector3(checkPos.x, 1000f, checkPos.z), Vector3.down, out RaycastHit nHit, Mathf.Infinity, groundMask))
                        {
                            neighborHeight = nHit.point.y;
                        }

                        if (neighborHeight < waterLevel || Mathf.Abs(surfaceHeight - neighborHeight) > maxHeightDifference)
                        {
                            isWalkable = false;
                            break;
                        }
                    }
                }

                if (isWalkable)
                {
                    isWalkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                }

                if (isWalkable)
                {
                    isWalkable = !Physics.CheckSphere(worldPoint, safetyRadius, buildingMask);
                }

                grid[x, y] = new Node(isWalkable, worldPoint, x, y);
            }
        }
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = Mathf.Clamp01((worldPosition.x - transform.position.x + gridWorldSize.x / 2) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPosition.z - transform.position.z + gridWorldSize.y / 2) / gridWorldSize.y);
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid == null && drawGizmos) CreateGrid();

        if (grid != null && drawGizmos)
        {
            foreach (Node n in grid)
            {
                if (Vector3.Distance(n.worldPosition, transform.position) < gizmoDrawDistance)
                {
                    Gizmos.color = n.isWalkable ? Color.white : Color.red;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            }
        }
    }
}