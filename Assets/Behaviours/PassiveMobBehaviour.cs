using UnityEngine;

public class PassiveMobBehaviour : MonoBehaviour
{
    [SerializeField] public Pathfinding pathfinding;
    [SerializeField] public Grid grid;
    [SerializeField] private int maxPlacementAttempts = 30;

    private BTNode root;

    // not recommended but
    bool markedForDeath = false; // conditional, for bt
    bool canCull = false;        // for system to cull it

    // actions: wander, idle, die
    public enum MobState { Wandering, Idling, Dying }
    [SerializeField] private MobState currentState;

    public float idleTime = 5.0f;
    public float maxIdleTime = 5.0f;

    private void Awake()
    {
        root = new RandomSelector(
                new ActionNode(Wander),
                new ActionNode(Idle)
            );

        // the passive mob will wander, and stay still, looking around at times. its behaviour is very simple
        // if it is marked for death, make it play the dead behaviour and then let the system despawn it

    }

    public void MarkDead() {
        markedForDeath = true;
    }

    private void Start()
    {
        if (grid == null) grid = Grid.Instance;
    }

    private void Update()
    {
        if (markedForDeath) return;
        if (currentState == MobState.Idling) idleTime -= SimulationManager.Instance.GetDNC().SimDeltaTime;

        root.Tick();
        pathfinding.Tick();
    }

    private NodeResult Wander()
    {
        // if not already wandering, set and find goal
        if (currentState != MobState.Wandering)
        {
            currentState = MobState.Wandering;
            if (TryGetRandomWalkableTile(out GridPos goalPos))
                pathfinding.SetGoal(grid.Get_WorldPos(goalPos));
        }

        if (!pathfinding.HasArrived)
        {
            return NodeResult.Running;
        }
        else {
            return NodeResult.Success;
        }
    }

    private NodeResult Idle() {
        if (currentState != MobState.Idling)
        {
            currentState = MobState.Idling;
            idleTime = UnityEngine.Random.Range(1.0f, 5.0f);
        }

        if (idleTime <= 0.0f) { // return success if idling done
            return NodeResult.Success;
        }

        return NodeResult.Running;
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