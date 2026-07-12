using UnityEngine;

public class ZombieBehaviour : MonoBehaviour
{
    [SerializeField] public Pathfinding pathfinding;
    [SerializeField] public Grid grid;
    [SerializeField] private int maxPlacementAttempts = 30;

    private BTNode root;

    public float zombieChaseTimer = 5.0f;
    public float zomdieChaseDuration = 5.0f;

    // bool markedForDeath = false; // unused field for now

    // actions: wander, idle, chase(?)
    public enum ZombieState { Wandering, Idling, Chasing }
    private ZombieState currentState;

    public GameObject humanTarget;

    private void Awake()
    {
        root = new ActionNode(() => Wander());

        // the zombie will wander, and stay still, looking around at times
        // unless a human is within a certain radius of it, in which it will then give chase for about 5s
        // give zombie a timeout for chasing

        // zombie has no markedfordeath or cancull because system will automatically remove every zombie upon daytime
        root = new Selector(
            // zombie priorities
            // 1. only chase human if it is near one and duration is more than 0.0f.
            new Sequence(new ConditionNode(() => IsNearHuman() && zomdieChaseDuration >= 0.0f), new ActionNode(ChaseHuman)),

            // 2. otherwise, choose to idle or 

            new ActionNode(Wander)
        );
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