using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid; // now refs terrain gen by default
    // target Transform removed — replaced by a wandering goal

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float waypointReachDistance = 0.1f;

    [Header("Path Settings")]
    [SerializeField] private Heuristic heuristic = Heuristic.OCTILE;
    [SerializeField] private float weight = 1.0f;
    [SerializeField] private bool smoothing = true;
    [SerializeField] private bool rubberBanding = true;
    [SerializeField] private bool singleStep = false;
    [SerializeField] private bool debugColoring = false;

    [Header("Wander Settings")]
    [SerializeField] private bool autoWander = true;
    [SerializeField] private int maxPlacementAttempts = 30;

    // Terrain Gen GO needed for displacing person upwards
    //public TerrainGen terrainGen;

    private Astar astar;
    private PathRequest request;

    private readonly List<Vector3> currentPath = new List<Vector3>();
    private int currentWaypointIndex;

    private void Awake()
    {
        if (grid == null)
            grid = Grid.Instance;

        astar = new Astar(grid);

        request = new PathRequest
        {
            path = new List<Vector3>(),
            heuristic = heuristic,
            weight = weight,
            smoothing = smoothing,
            rubberBanding = rubberBanding,
            singleStep = singleStep,
            debugColoring = debugColoring,
            newRequest = true
        };
    }

    private void Update()
    {
        if (grid == null) return;

        if (autoWander && currentPath.Count == 0)
        {
            RequestWanderPath();
        }

        FollowPath();
    }

    private void RequestWanderPath()
    {
        GridPos startGridPos = grid.Get_GridPos(transform.position);
        GridPos goalGridPos;

        if (!TryGetRandomWalkableTile(out goalGridPos))
            return; // grid is entirely walls, or we got unlucky — try again next frame

        request.start = transform.position;
        request.goal = grid.Get_WorldPos(goalGridPos);

        request.heuristic = heuristic;
        request.weight = weight;
        request.smoothing = smoothing;
        request.rubberBanding = rubberBanding;
        request.singleStep = singleStep;
        request.debugColoring = debugColoring;
        request.newRequest = true;

        PathResult result = astar.ComputePath(ref request);

        if (result == PathResult.COMPLETE)
        {
            currentPath.Clear();
            currentPath.AddRange(request.path);

            currentWaypointIndex = 0;
            request.newRequest = false;
        }
        // IMPOSSIBLE just means we picked an unreachable tile —
        // currentPath stays empty, so we'll try a new random tile next frame
    }

    private bool TryGetRandomWalkableTile(out GridPos result)
    {
        UnityEngine.Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAA");
        for (int i = 0; i < maxPlacementAttempts; i++)
        {
            int row = UnityEngine.Random.Range(0, grid.max_row);
            int col = UnityEngine.Random.Range(0, grid.max_col);

            GridPos candidate = new GridPos(row, col);

            if (grid.IsValid(candidate) && !grid.IsWall(candidate))
            {
                result = candidate;
                return true;
            }
        }

        UnityEngine.Debug.Log("ABBABABABBABBAB"); // IT'S NOT EVEN BEING CALLED?

        result = default;
        return false;
    }

    private void MoveTargetTowards(Vector3 currentPos, Vector3 targetPos, float speed) { 
        // so basically its a movetowards BUT factoring in the y displacement due to the heightmap
    }

    private void FollowPath()
    {
        if (currentWaypointIndex >= currentPath.Count) { 
            currentPath.Clear(); return; 
        }

        // so......what's up. why does the flat not flat

        // need to flatten y here
        Vector3 waypoint = currentPath[currentWaypointIndex];
        float waypointY = grid.terrainGen.GetY(waypoint.x, waypoint.z) + 0.5f;

        Vector3 flatCurrent = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(waypoint.x, 0f, waypoint.z);

        Vector3 flatMoved = Vector3.MoveTowards(flatCurrent, flatTarget, moveSpeed * Time.deltaTime);

        transform.position = new Vector3(flatMoved.x, waypointY, flatMoved.z);

        // track xz movement only, y irrelevant
        float flatDistance = Vector3.Distance(flatMoved, flatTarget);

        if (flatDistance <= waypointReachDistance)
        {
            currentWaypointIndex++;
        }

        //Vector3 waypoint = currentPath[currentWaypointIndex];

        // i don't think i can use their innate moretowwards here? i think i need a custom movetowards for y disp
        //transform.position = Vector3.MoveTowards(transform.position, waypoint, moveSpeed * Time.deltaTime);

        //if (Vector3.Distance(transform.position, waypoint) <= waypointReachDistance)
        //{
        //    currentWaypointIndex++;
        //}
    }

    private void OnDrawGizmos()
    {
        if (currentPath == null || currentPath.Count == 0) return;

        Gizmos.color = Color.green;

        // lol i tried, it's not the smoothest, how do i make it smooth then
        for (int i = 0; i < currentPath.Count; i++)
        {
            float i_y = grid.terrainGen.GetY(currentPath[i].x, currentPath[i].z) + 0.5f;
            Vector3 pathYDisp = new Vector3(currentPath[i].x, i_y, currentPath[i].z);

            Gizmos.DrawSphere(pathYDisp, 0.2f);

            if (i + 1 < currentPath.Count)
            {
                float i1_y = grid.terrainGen.GetY(currentPath[i + 1].x, currentPath[i + 1].z) + 0.5f;
                Vector3 path1YDisp = new Vector3(currentPath[i + 1].x, i1_y, currentPath[i + 1].z);

                Gizmos.DrawLine(pathYDisp, path1YDisp);
            }
        }
    }
}