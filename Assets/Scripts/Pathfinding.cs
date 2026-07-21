using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditorInternal;

public class Pathfinding : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid; // now refs terrain gen by default
    // target Transform removed — replaced by a wandering goal

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float waypointReachDistance = 0.1f;

    [Header("Path Settings")]
    //[SerializeField]
    public Heuristic heuristic = Heuristic.OCTILE;
    [SerializeField] private float weight = 1.0f;
    [SerializeField] private bool smoothing = true;
    [SerializeField] private bool rubberBanding = true;
    [SerializeField] private bool singleStep = false;

    [Header("Wander Settings")]
    [SerializeField] private bool autoWander = true;
    [SerializeField] private int maxPlacementAttempts = 30;

    [Header("Heuristic Dropdown")]
    [SerializeField] private TMP_Dropdown heuristicDrop;


    // Terrain Gen GO needed for displacing person upwards
    //public TerrainGen terrainGen;

    private Astar astar;
    private PathRequest request;

    private readonly List<Vector3> currentPath = new List<Vector3>();
    private int currentWaypointIndex;

    public bool HasArrived => currentWaypointIndex >= currentPath.Count;

    public void SetMovementSpeed(float speed) { moveSpeed = speed; }

    private void Awake()
    {
        if (grid == null)
        {
            grid = Grid.Instance;
        }

        astar = new Astar(grid);
        //used for the UI dropdown
        if (heuristicDrop == null)
        {
            heuristicDrop = GameObject.Find("HeuristicSelection").GetComponent<TMP_Dropdown>();
        }

        heuristic = heuristicDrop.GetComponent<HeuristicSelection>().heuristicVal;
        
        request = new PathRequest
        {
            path = new List<Vector3>(),
            heuristic = heuristic,
            weight = weight,
            smoothing = smoothing,
            rubberBanding = rubberBanding,
            singleStep = singleStep,
            newRequest = true
        };
    }

    public void SetGrid(Grid grid) {  this.grid = grid; }

    private void Start()
    {
        if (grid == null)
        {
            grid = Grid.Instance;
        }
        astar = new Astar(grid);
    }

    public bool TrySetGoal(Vector3 goal)
    {
        //update heuristic value based on dropdown
        heuristic = heuristicDrop.GetComponent<HeuristicSelection>().heuristicVal;
        //create new path
        PathRequest request = new PathRequest
        {
            start = transform.position,
            goal = goal,
            path = new List<Vector3>(),
            heuristic = heuristic,
            weight = weight,
            smoothing = smoothing,
            rubberBanding = rubberBanding,
            singleStep = false,
            newRequest = true
        };

        if (astar.ComputePath(ref request) == PathResult.COMPLETE)
        {
            currentPath.Clear();
            foreach (Vector3 p in request.path)
                currentPath.Add(new Vector3(p.x, grid.terrainGen.GetY(p.x, p.z), p.z));
            currentWaypointIndex = 0;
            return true;
        }

        return false; // caller now knows it failed, instead of guessing from HasArrived
    }

    public void SetGoal(Vector3 goal) => TrySetGoal(goal); // keep the old void signature working elsewhere

    public void Tick() => FollowPath();

    private void Update()
    {
        if (grid == null) return;
        heuristic = heuristicDrop.GetComponent<HeuristicSelection>().heuristicVal;
        
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

    // add in a move target here? but how do you consider IF the target moves? (for chasing down passive mobs)

    private bool TryGetRandomWalkableTile(out GridPos result)
    {
        //UnityEngine.Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAA");
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


        result = default;
        return false;
    }

    private void FollowPath()
    {
        if (currentWaypointIndex >= currentPath.Count) { 
            currentPath.Clear(); return; 
        }


        // need to flatten y here
        Vector3 waypoint = currentPath[currentWaypointIndex];
        float waypointY = grid.terrainGen.GetY(waypoint.x, waypoint.z) + 0.5f;

        Vector3 flatCurrent = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(waypoint.x, 0f, waypoint.z);

        Vector3 flatMoved = Vector3.MoveTowards(flatCurrent, flatTarget, moveSpeed * SimulationManager.Instance.GetDNC().SimDeltaTime);

        transform.position = new Vector3(flatMoved.x, waypointY, flatMoved.z);

        // look at target spot
        Vector3 moveDir = flatTarget - flatCurrent;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Vector3 lookDir = new Vector3(moveDir.x, 0f, moveDir.z); // keep upright, no pitch
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        // track xz movement only, y irrelevant
        float flatDistance = Vector3.Distance(flatMoved, flatTarget);

        if (flatDistance <= waypointReachDistance)
        {
            currentWaypointIndex++;
        }

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