using System.Collections.Generic;
using UnityEngine;



public class Pathfinding : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private Transform target;

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

    [Header("Input")]
    [SerializeField] private KeyCode requestPathKey = KeyCode.Space;
    [SerializeField] private bool autoRequestPath = false;

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
        if (target == null || grid == null) return;

        if (Input.GetKeyDown(requestPathKey) || (autoRequestPath && currentPath.Count == 0)) { 
            RequestPath(); 
        }

       
        FollowPath();
    }

    private void RequestPath()
    {
        request.start = transform.position;
        request.goal = target.position;

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
    }

    private void FollowPath()
    {
        if (currentWaypointIndex >= currentPath.Count) return;

        Vector3 waypoint = currentPath[currentWaypointIndex];

        transform.position = Vector3.MoveTowards(transform.position,waypoint, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, waypoint) <= waypointReachDistance)
        {
            currentWaypointIndex++;
        }
    }

    private void OnDrawGizmos()
    {
        if (currentPath == null || currentPath.Count == 0) return;

        Gizmos.color = Color.green;

        for (int i = 0; i < currentPath.Count; i++)
        {
            Gizmos.DrawSphere(currentPath[i], 0.2f);

            if (i + 1 < currentPath.Count)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
        }
    }
}