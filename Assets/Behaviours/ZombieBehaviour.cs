using UnityEngine;
using static PassiveMobBehaviour;

public class ZombieBehaviour : MonoBehaviour
{
    [SerializeField] public Pathfinding pathfinding;
    [SerializeField] public Grid grid;
    [SerializeField] private int maxPlacementAttempts = 30;

    private BTNode root;

    public float idleTime = 5.0f;
    public float maxIdleTime = 5.0f;

    public float zombieChaseTimer = 5.0f;
    public float zombieChaseDuration = 5.0f;

    public float zombieChaseCooldown = 4.0f;
    public float zombieChaseCurrentCooldown = 4.0f;

    public float repathTimer = 10.0f;
    public float repathInterval = 10.0f;

    // bool markedForDeath = false; // unused field for now

    // actions: wander, idle, chase(?)
    public enum ZombieState { None, Wandering, Idling, Chasing }
    [SerializeField] private ZombieState currentState;

    public GameObject humanTarget;
    public float humanDetectionRadius = 55.0f;

    private void Awake()
    {
        //root = new ActionNode(() => Wander());

        // the zombie will wander, and stay still, looking around at times
        // unless a human is within a certain radius of it, in which it will then give chase for about 5s
        // give zombie a timeout for chasing

        // zombie has no markedfordeath or cancull because system will automatically remove every zombie upon daytime
        root = new Selector(
            // zombie priorities
            // 1. only chase human if it is near one and duration is more than 0.0f.
            new Sequence(
                new ConditionNode(() => {
                    bool ready = zombieChaseCurrentCooldown <= 0.0f && IsNearHuman() && zombieChaseDuration >= 0.0f;
                    Debug.Log("chase condition: " + ready);
                    return ready;
                }),
                new ActionNode(ChaseHuman)
            ),

            // 2. otherwise, choose to idle or wander
            new RandomSelector(
                new ActionNode(Wander),
                new ActionNode(Idle)
            )
        );
    }

    private void Start()
    {
        if (grid == null) grid = Grid.Instance;
    }

    private void Update()
    {
        if (currentState == ZombieState.Idling) idleTime -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (currentState == ZombieState.Chasing) zombieChaseDuration -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (zombieChaseCurrentCooldown >= 0.0f) zombieChaseCurrentCooldown -= SimulationManager.Instance.GetDNC().SimDeltaTime;

        root.Tick();
        pathfinding.Tick();
    }

    private bool IsNearHuman() {
        // pure condition no state
        Vector3 humanPosFlat = new Vector3(humanTarget.transform.position.x, 0.0f, humanTarget.transform.position.z);
        Vector3 thisPosFlat = new Vector3(transform.position.x, 0.0f, transform.position.z);
        float dist = Vector3.Distance(humanPosFlat, thisPosFlat);
        return dist <= humanDetectionRadius;
    }

    private NodeResult ChaseHuman()
    {
        if (currentState != ZombieState.Chasing)
        {
            currentState = ZombieState.Chasing;
            zombieChaseDuration = zombieChaseTimer; // start the timer fresh
            pathfinding.SetGoal(humanTarget.transform.position); // TODO logic replaced
        }

        if (zombieChaseDuration <= 0.0f)
        {
            zombieChaseCurrentCooldown = zombieChaseCooldown; // arm cooldown only when chase actually ends
            return NodeResult.Success;
        }

        // TODO: repath periodically toward humanTarget, like Hunt()/Shelter() do with repathTimer
        repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (repathTimer <= 0f)
        {
            pathfinding.SetGoal(humanTarget.transform.position);
            repathTimer = repathInterval;
        }
        return NodeResult.Running;
    }

    private NodeResult Wander()
    {
        // if not already wandering, set and find goal
        if (currentState != ZombieState.Wandering)
        {
            currentState = ZombieState.Wandering;
            if (TryGetRandomWalkableTile(out GridPos goalPos))
                pathfinding.SetGoal(grid.Get_WorldPos(goalPos));
        }

        if (!pathfinding.HasArrived)
        {
            return NodeResult.Running;
        }
        else
        {
            currentState = ZombieState.None;
            return NodeResult.Success;
        }
    }

    private NodeResult Idle()
    {
        if (currentState != ZombieState.Idling)
        {
            currentState = ZombieState.Idling;
            idleTime = UnityEngine.Random.Range(1.0f, 5.0f);
        }

        if (idleTime <= 0.0f)
        { // return success if idling done
            currentState = ZombieState.None;
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