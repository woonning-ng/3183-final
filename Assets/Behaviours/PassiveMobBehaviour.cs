using UnityEngine;

public class PassiveMobBehaviour : MonoBehaviour
{
    // Pathfinding and grid items
    [SerializeField] public Pathfinding pathfinding;         // Pathfinding GO needed for pathing
    [SerializeField] public Grid grid;                       // Grid GO needed for querying the current step
    [SerializeField] private int maxPlacementAttempts = 30;  // How many times to try and place PM at spawn

    // Human BT related items
    private BTNode root;

    // PM's state enums and current state
    public enum MobState { None, Wandering, Idling }
    [SerializeField] private MobState currentState;

    // PM Death Marker (for culling)
    bool markedForDeath = false; // conditional, for bt

    // PM Idle Time
    public float idleTime = 0.0f;    // current idle time
    public float maxIdleTime = 5.0f; // how long should PM idle for

    // one-time call to set stats from global params manager
    bool firstTimeInit = false;

    // ---------------------------- GLOBAL PARAM INITIALISATION ----------------------------
    public void InitialiseVariablesFromGlobalParams()
    {
        // first initialise the items THIS script needs
        maxIdleTime = GlobalParam.passivemob_bt_globals.MaxIdleTime;

        // then initialise movement speed for the pathfinding obj
        GetComponent<Pathfinding>().SetMovementSpeed(GlobalParam.passivemob_bt_globals.MoveSpeed);
    }

    // ---------------------------- QUERIES ----------------------------
    // Query if mob is dead (should it be culled)
    public bool IsDead() { return markedForDeath; }

    // ---------------------------- ACTIONS ----------------------------
    // Mark mob for culling
    public void MarkDead() {
        markedForDeath = true;
    }

    // Helper function to pick a random tile for mob to wander towards
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

    // Mob's Wander action node
    private NodeResult Wander()
    {
        // If the Mob is not already wandering, set and find goal.
        // If a valid walktable tile is found, set the Mob's Pathfinding component's
        // goal to the obtained goal position.
        if (currentState != MobState.Wandering)
        {
            currentState = MobState.Wandering;
            if (TryGetRandomWalkableTile(out GridPos goalPos))
                pathfinding.SetGoal(grid.Get_WorldPos(goalPos));
        }

        // Query the Pathfinding component and see if the Mob has arrived at its destination.
        // If no, still in progress, so return Running. Otherwise, return Success.
        if (!pathfinding.HasArrived)
        {
            return NodeResult.Running;
        }
        else
        {
            return NodeResult.Success;
        }
    }

    // Mob's Idle action node
    private NodeResult Idle()
    {
        // If the Mob isn't already Idling when this Node is executed,
        // Set its state to Idling and roll valid IdleTime.
        if (currentState != MobState.Idling)
        {
            currentState = MobState.Idling;
            idleTime = UnityEngine.Random.Range(1.0f, maxIdleTime); // [1, maxIdleTime]
        }

        // If its IdleTime <= 0, it has finished Idling. Return Success.
        if (idleTime <= 0.0f)
        { // return success if idling done
            return NodeResult.Success;
        }

        // Otherwise, its Idling action is still firing, so return Running.
        return NodeResult.Running;
    }

    // ------------ AWAKE, START, UPDATE, TICKSTATS ------------
    private void Awake()
    {
        // Initialise the BT of the PassiveMob
        // 1. chose to idle or wander
        root = new RandomSelector(
                new ActionNode(Wander),
                new ActionNode(Idle)
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
        if (firstTimeInit) {
            firstTimeInit = false;

            // initialise global param stats here
            InitialiseVariablesFromGlobalParams();
        }

        // Ignore Update() if mob is Dead
        if (markedForDeath) return;

        // Simplified TickStats() for PM
        // Decrease IdleTime if the Mob is currently in Idling
        if (currentState == MobState.Idling) idleTime -= SimulationManager.Instance.GetDNC().SimDeltaTime;

        root.Tick();        // call Tick() on Root
        pathfinding.Tick(); // call Tick() on Pathfinding, in case mob is currently pathfinding
    }
}