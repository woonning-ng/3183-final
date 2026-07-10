using UnityEngine;

public class ZombieBehaviour : MonoBehaviour
{
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Grid grid;
    [SerializeField] private int maxPlacementAttempts = 30;

    private BTNode root;

    bool markedForDeath = false; // unused field for now

    // actions: wander, idle, chase(?)

    private void Awake()
    {
        root = new ActionNode(() => Wander());
    }

    private void Start()
    {
        if (grid == null) grid = Grid.Instance;
    }

    private void Update()
    {
        root.Tick();
        pathfinding.Tick();
    }

    private NodeResult Wander()
    {
        if (!pathfinding.HasArrived)
            return NodeResult.Running;

        if (TryGetRandomWalkableTile(out GridPos goalPos))
            pathfinding.SetGoal(grid.Get_WorldPos(goalPos));

        return NodeResult.Running; // never "succeeds" — wandering is a loop, not a goal state
    }

    private bool TryGetRandomWalkableTile(out GridPos result)
    {
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
}