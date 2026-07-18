using UnityEngine;

public class ZombieBehaviour : MonoBehaviour
{
    // Pathfinding and grid items
    [SerializeField] public Pathfinding pathfinding;         // Pathfinding GO needed for pathing
    [SerializeField] public Grid grid;                       // Grid GO needed for querying the current step
    [SerializeField] private int maxPlacementAttempts = 30;  // How many times to try and place zombie at spawn

    // Zombie BT related items
    private BTNode root;

    // Zombie's state enums and current state
    public enum ZombieState { None, Wandering, Idling, Chasing }
    [SerializeField] private ZombieState currentState;

    // Zombie Idle Time
    public float idleTime = 0.0f;    // current idle time
    public float maxIdleTime = 5.0f; // how long should zombie idle for

    // Zombie Chase Time
    public float zombieChaseTimer = 5.0f;           // current chase time
    public float zombieChaseCurrentDuration = 5.0f; // how long should zombie chase human for

    // Zombie Chase Cooldown
    public float zombieChaseCooldown = 4.0f;        // current chase cooldown
    public float zombieChaseCurrentCooldown = 4.0f; // how long should zombie's chase cooldown for

    // Zombie's repathing attribtues
    public float repathTimer = 0.0f;      // current repath time
    public float repathInterval = 10.0f;  // base repath interval timing

    // Zombie's human targeting related attributes
    public GameObject humanTarget;
    public float humanDetectionRadius = 55.0f;

    // ---------------------------- ACTIONS ----------------------------
    private bool IsNearHuman()
    {
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
            zombieChaseCurrentDuration = zombieChaseTimer; // start the timer fresh
            pathfinding.SetGoal(humanTarget.transform.position); // TODO logic replaced
        }

        if (zombieChaseCurrentDuration <= 0.0f)
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


    // ------------ AWAKE, START, UPDATE ------------
    private void Awake()
    {
        // zombie has no markedfordeath or cancull because system will automatically remove every zombie upon daytime
        root = new Selector(
            // zombie priorities
            // 1. only chase human if it is near one and duration is more than 0.0f.
            new Sequence(
                new ConditionNode(() => {
                    bool ready = zombieChaseCurrentCooldown <= 0.0f && IsNearHuman() && zombieChaseCurrentDuration >= 0.0f;
                    //Debug.Log("chase condition: " + ready);
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
        if (currentState == ZombieState.Chasing) zombieChaseCurrentDuration -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (zombieChaseCurrentCooldown >= 0.0f) zombieChaseCurrentCooldown -= SimulationManager.Instance.GetDNC().SimDeltaTime;

        root.Tick();
        pathfinding.Tick();
    }
}