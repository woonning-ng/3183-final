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
    public GameObject humanTarget;                // GO to target (move towards)
    public float humanDetectionRadius = 55.0f;    // hw close should the human be for chase

    // one-time call to set stats from global params manager
    bool firstTimeInit = false;

    // ---------------------------- ACTIONS ----------------------------
    // Helper function to determine if Zombie is currently near the Human
    private bool IsNearHuman()
    {
        // Flat position checking to see if the human is near the zombie.
        Vector3 humanPosFlat = new Vector3(humanTarget.transform.position.x, 0.0f, humanTarget.transform.position.z);
        Vector3 thisPosFlat = new Vector3(transform.position.x, 0.0f, transform.position.z);

        return Vector3.Distance(humanPosFlat, thisPosFlat) <= humanDetectionRadius;
    }

    // Zombie's ChaseHuman action node
    private NodeResult ChaseHuman()
    {
        // If the Zombie is not already chasing, set and refresh timer, then set goal to human's location
        if (currentState != ZombieState.Chasing)
        {
            currentState = ZombieState.Chasing;
            zombieChaseCurrentDuration = zombieChaseTimer; // start the timer fresh
            pathfinding.SetGoal(humanTarget.transform.position);
        }

        // If the zombie's chase duration is up, set its current cooldown to the max cooldown
        // and return Success (zombie has finished chasing human)
        if (zombieChaseCurrentDuration <= 0.0f)
        {
            zombieChaseCurrentCooldown = zombieChaseCooldown; // arm cooldown only when chase actually ends
            return NodeResult.Success;
        }

        // Repath periodically toward humanTarget, like Human's Hunt()/Shelter() do with repathTimer
        repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (repathTimer <= 0f)
        {
            pathfinding.SetGoal(humanTarget.transform.position);
            repathTimer = repathInterval;
        }
        return NodeResult.Running;
    }

    // Helper function to pick a random tile for zombie to wander towards
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

    // Zombie's Wander action node
    private NodeResult Wander()
    {
        // If the Mob is not already wandering, set and find goal.
        // If a valid walktable tile is found, set the Mob's Pathfinding component's
        // goal to the obtained goal position.
        if (currentState != ZombieState.Wandering)
        {
            currentState = ZombieState.Wandering;
            if (TryGetRandomWalkableTile(out GridPos goalPos))
                pathfinding.SetGoal(grid.Get_WorldPos(goalPos));
        }

        // Query the Pathfinding component and see if the Zombie has arrived at its destination.
        // If no, still in progress, so return Running. Otherwise, return Success.
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

    // Zombie's Idle action node
    private NodeResult Idle()
    {
        // If the Zombie isn't already Idling when this Node is executed,
        // Set its state to Idling and roll valid IdleTime.
        if (currentState != ZombieState.Idling)
        {
            currentState = ZombieState.Idling;
            idleTime = UnityEngine.Random.Range(1.0f, maxIdleTime);
        }

        // If its IdleTime <= 0, it has finished Idling. Return Success.
        if (idleTime <= 0.0f)
        { // return success if idling done
            currentState = ZombieState.None;
            return NodeResult.Success;
        }

        // Otherwise, its Idling action is still firing, so return Running.
        return NodeResult.Running;
    }


    // ------------ AWAKE, START, UPDATE ------------
    private void Awake()
    {
        // zombie has no markedfordeath or cancull because system will automatically remove every zombie upon daytime
        root = new Selector(
            // zombie priorities
            // 1. only chase human if it is near one & chase duration is more than 0.0f & chase cooldown <= 0.
            new Sequence(
                new ConditionNode(() => {
                    bool ready = zombieChaseCurrentCooldown <= 0.0f && IsNearHuman() && zombieChaseCurrentDuration >= 0.0f;
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
        // Get a reference to the Grid Instance (needed for pathfinding)
        if (grid == null) grid = Grid.Instance;
        firstTimeInit = true;
    }

    private void Update()
    {
        // First Time Initialisation - for global param stats
        if (firstTimeInit)
        {
            firstTimeInit = false;
            //UnityEngine.Debug.Log("calling first time instantiate for a chungy");

            // initialise global param stats here
        }

        // Simplified TickStats() for Zombie
        // Decrease IdleTime if the Zombie is currently in Idling
        if (currentState == ZombieState.Idling) idleTime -= SimulationManager.Instance.GetDNC().SimDeltaTime;

        // Decrease Current Chase Duration if Zombie is currently in Chasing
        if (currentState == ZombieState.Chasing) zombieChaseCurrentDuration -= SimulationManager.Instance.GetDNC().SimDeltaTime;

        // If the cooldown for Chase is not 0, decrease it
        if (zombieChaseCurrentCooldown >= 0.0f) zombieChaseCurrentCooldown -= SimulationManager.Instance.GetDNC().SimDeltaTime;

        root.Tick();        // call Tick() on Root
        pathfinding.Tick(); // call Tick() on Pathfinding, in case zombie is currently pathfinding
    }
}