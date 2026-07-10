using UnityEngine;

public class PassiveMobBehaviour : MonoBehaviour
{
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Grid grid;
    [SerializeField] private int maxPlacementAttempts = 30;

    private BTNode root;

    // not recommended but
    bool markedForDeath = false; // conditional, for bt
    bool canCull = false;        // for system to cull it

    // actions: wander, idle, die

    private void Awake()
    {
        root = new ActionNode(() => Wander());

        // the passive mob will wander, and stay still, looking around at times. its behaviour is very simple
        // if it is marked for death, make it play the dead behaviour and then let the system despawn it
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