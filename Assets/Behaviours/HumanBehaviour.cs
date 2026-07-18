using System.Collections.Generic;
using UnityEngine;

public class HumanBehaviour : MonoBehaviour
{
    // Pathfinding and grid items
    [SerializeField] public Pathfinding pathfinding;         // Pathfinding GO needed for pathing
    [SerializeField] public Grid grid;                       // Grid GO needed for querying the current step
    [SerializeField] private int maxPlacementAttempts = 30;  // How many times to try and place human at spawn

    // Human BT related items
    private BTNode root;

    // Human's state enums and current state
    public enum HumanState { None, Wandering, Hunting, SeekingShelter, Sheltering, Sleeping } // Idling
    [SerializeField] private HumanState currentState = HumanState.None;

    // Human's stats -- Stat itself and Max
    public float maxHunger = 100f;
    public float maxEnergy = 100f;
    public float maxSafety = 100f;
    public float hunger;
    public float energy;
    public float safety;

    // Is Human Dead (Sim-ending condition)
    bool isDead = false;

    // Human Idle Time - Unused
    //public float idleTime = 0.5f;
    //public float maxIdleTime = 0.5f;

    // Human's thresholds for performing actions
    public float safetyPriorityThreshold = 20.0f; // at what value should it begin to seek safety
    public float hungerHuntThreshold = 60.0f;     // at what value should it begin to hunt for food

    // Human's stat decays
    public float hungerDecayRate = 0.5f;          // constant hunger decay rate
    public float nightSafetyDecay = 1.0f;         // base safety decay rate (at night)
    public float energyDecayRate = 1.0f;          // base energy decay rate

    // Human's stat regens
    public float shelterEnergyRegen = 3.0f;       // energy regen rate in shelter
    public float shelterSafetyRegen = 3.0f;       // safety regen rate in shelter
    public float sleepEnergyRegen = 1.5f;         // sleep regen rate when asleep outside
    public float safetyRegen = 1.0f;              // safety regen rate in the day

    // Human's stat multipliers and flat numbers
    public float nightZombieSafetyMultiplier = 2.0f; // safety decrease multiplier when near zombie
    public float hungerRestoreAmount = 20.0f;        // how much hunger consuming food recovers
    public float zombieDetectionRadius = 20.0f;      // zombie detection radius for safety decrease multiplier

    // Human's repathing attribtues
    public float repathTimer = 0.0f;            // current repath time
    public float repathInterval = 0.5f;         // base repath interval timing

    // Human's hunting-related attributes
    private GameObject huntTarget;                // GO to hunt for
    public float eatDistance = 5.0f;              // how close should the human be for valid hunt
    private int huntPathFailures = 0;             // how many times human has failed to reach hunt target in time
    private const int maxHuntPathFailures = 3;    // how many hunt pathing failures before returning Failure

    // Human's shelter-seeking related attributes
    private Vector3 currentShelterTarget;                                   // GO to move towards for Shelter
    private static readonly Vector3 NoShelter = new Vector3(-1, -1, -1);    // special case: Shelter is invalid

    // one-time call to place the human at a suitable location AND set stats from global params manager
    bool firstTimeInit = false;

    // Human's cached IsNight() and IsHungry()
    // cached once per frame so every branch of the tree + TickStats agree on the same snapshot
    private bool cachedIsNight;
    private bool cachedIsHungry;

    // ---------------------------- GLOBAL PARAM INITIALISATION ----------------------------
    public void InitialiseVariablesFromGlobalParams() {
        // first initialise the items THIS script needs
        maxHunger = GlobalParam.human_bt_globals.MaxHunger;
        maxEnergy = GlobalParam.human_bt_globals.MaxEnergy;
        maxSafety = GlobalParam.human_bt_globals.MaxSafety;

        safetyPriorityThreshold = GlobalParam.human_bt_globals.SafetyPriorityThreshold;
        hungerHuntThreshold = GlobalParam.human_bt_globals.HungerHuntThreshold;

        hungerDecayRate = GlobalParam.human_bt_globals.HungerDecayRate;
        nightSafetyDecay = GlobalParam.human_bt_globals.NightSafetyDecay;
        energyDecayRate = GlobalParam.human_bt_globals.EnergyDecayRate;

        shelterEnergyRegen = GlobalParam.human_bt_globals.ShelterEnergyRegen;
        shelterSafetyRegen = GlobalParam.human_bt_globals.ShelterSafetyRegen;

        sleepEnergyRegen = GlobalParam.human_bt_globals.SleepEnergyRegen;
        safetyRegen = GlobalParam.human_bt_globals.SafetyRegen;

        nightZombieSafetyMultiplier = GlobalParam.human_bt_globals.NightZombieSafetyMultiplier;
        hungerRestoreAmount = GlobalParam.human_bt_globals.HungerRestoreAmount;
        zombieDetectionRadius = GlobalParam.human_bt_globals.ZombieDetectionRadius;

        repathInterval = GlobalParam.human_bt_globals.RepathInterval;
        eatDistance = GlobalParam.human_bt_globals.EatDistance;

        // then initialise movement speed for the pathfinding obj
        GetComponent<Pathfinding>().SetMovementSpeed(GlobalParam.human_bt_globals.MoveSpeed);
    }

    // ---------------------------- QUERIES ----------------------------
    // Query if human is dead
    public bool IsHumanDead() { return isDead; }

    // Query string representation of current Human State
    public string GetHumanState()
    {
        switch (currentState)
        {
            case HumanState.Wandering: return "Wandering";
            //case HumanState.Idling: return "Idling";
            case HumanState.Hunting: return "Hunting";
            case HumanState.SeekingShelter: return "Seeking Shelter";
            case HumanState.Sheltering: return "Sheltering";
            case HumanState.Sleeping: return "Sleeping";
            case HumanState.None: return "None";
            default: return "Unknown";
        }
    }

    // ---------------------------- ACTIONS ----------------------------
    // Helper function to pick a random shelter for human to pathfind towards
    private bool TryFindNearestShelter(out Vector3 shelter)
    {
        // Get a list of all safe tiles from the Grid instance's Terrain Gen reference
        List<Vector3> shelters = Grid.Instance.terrainGen.GetAllSafe();

        // Reset shelter and set closest distance to maximum
        shelter = NoShelter;
        float closestDist = float.MaxValue;

        // Iterate through the list of shelters
        foreach (var hit in shelters)
        {
            // Compare the flat distance between the human and the shelter
            Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 flatShelter = new Vector3(hit.x, 0.0f, hit.z);

            // update shelter and the closest distance if a shelter was found (and is nearer)
            float dist = Vector3.Distance(flatHuman, flatShelter);
            if (dist < closestDist)
            {
                closestDist = dist;
                shelter = hit;
            }
        }

        // return bool denoting if shelter was found
        return shelter != NoShelter;
    }

    // Helper function to determine if human is currently near a shelter
    private bool AtShelter(Vector3 shelter, float arriveDistance = 1.5f) // default radius: 1.5f
    {
        // Flat position checking to see if the human is near te shelter
        Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 flatShelter = new Vector3(shelter.x, 0.0f, shelter.z);

        return Vector3.Distance(flatHuman, flatShelter) <= arriveDistance;
    }

    // Human's Shelter action node
    private NodeResult Shelter()
    {
        // if there is currently no shelter target, try to find the nearest target shelter for the human
        if (currentShelterTarget == NoShelter)
        {
            // try to get current shelter target. if it fails, by the BT's setup,
            // it falls through to Wander this same tick. Return Failure if so
            if (!TryFindNearestShelter(out currentShelterTarget))
                return NodeResult.Failure;

            // set current state to seekingshelter
            currentState = HumanState.SeekingShelter;
        }

        // if the human is not already at the current shelter target
        if (!AtShelter(currentShelterTarget))
        {
            // set its state to seeking shelter again, and repath periodically towards the shelter
            currentState = HumanState.SeekingShelter;

            repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
            if (repathTimer <= 0f)
            {
                pathfinding.SetGoal(currentShelterTarget);
                repathTimer = repathInterval;
            }
            return NodeResult.Running;
        }

        // if we reached here it means that the human managed to find shelter, so set the
        // proper state and return Success.
        currentState = HumanState.Sheltering;
        return NodeResult.Success;
    }

    // Helper function to pick a random target for human to pathfind towards to hunt
    private bool TryFindHuntTarget(out GameObject target)
    {
        // Reset closest dist and target
        float closestDist = float.MaxValue;
        target = null;

        if (SpawnManager.Instance.passiveMobs.Count <= 0) return false; // skip if no passive mobs at all

        // Iterate through SpawnMAnager instance's passive mob list
        foreach (var mob in SpawnManager.Instance.passiveMobs)
        {
            // Compare flat distance between human and passive mob
            Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 flatMob = new Vector3(mob.transform.position.x, 0.0f, mob.transform.position.z);

            // update target adn closest distance if a mob was fond (and is nearer)
            float dist = Vector3.Distance(flatHuman, flatMob);
            if (dist < closestDist)
            {
                closestDist = dist;
                target = mob;
            }
        }

        // return bool denoting if target was found
        return target != null;
    }

    // Human's Hunt action node
    private NodeResult Hunt()
    {
        // if there is currently no hunt target, try to find one. if fail, return Failure and let
        // the human's BT fall to the next node in the list.
        if (huntTarget == null && !TryFindHuntTarget(out huntTarget))
            return NodeResult.Failure;

        // otherwise, set the human's current state to Hunting and invalidate its shelter target
        currentState = HumanState.Hunting;
        currentShelterTarget = NoShelter;

        // calculate the flat distance between the human and the found target
        Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 flatMob = new Vector3(huntTarget.transform.position.x, 0.0f, huntTarget.transform.position.z);
        float dist = Vector3.Distance(flatHuman, flatMob);

        // if the human is within range of the mob
        if (dist <= eatDistance)
        {
            // restore the human.s hunger, mark the mob as dead, invalidate the human's hunt target
            // and reset huntPathFailures to 0 for the next execution, and return Success
            hunger = Mathf.Min(hunger + hungerRestoreAmount, maxHunger);
            huntTarget.GetComponent<PassiveMobBehaviour>().MarkDead();
            huntTarget = null;
            huntPathFailures = 0;
            return NodeResult.Success;
        }

        // periodically repath towards the mob
        repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (repathTimer <= 0f)
        {
            // sometimes the target might become unreachable, so there is a need to time out
            bool reachable = pathfinding.TrySetGoal(huntTarget.transform.position);
            repathTimer = repathInterval;

            if (!reachable)
            {
                // if cannot reach, increment huntPathFailures. and if the maximum has been
                // reached, invalidate the hunt target, set huntPathFailures to 0 and return Failure.
                huntPathFailures++;
                if (huntPathFailures >= maxHuntPathFailures)
                {
                    huntTarget = null;
                    huntPathFailures = 0;
                    return NodeResult.Failure; // give up, fall through to Wander this tick
                }
            }
            else
            {
                // reset huntPathFailures so that the human can continue properly pahtfinding twds mob
                huntPathFailures = 0;
            }
        }

        // return Running - still in progress
        return NodeResult.Running;
    }

    // Helper function to determine if a Zombie is currently near the Human.
    private bool ZombieNearby()
    {
        // set closest distance to max
        float closestDist = float.MaxValue;

        // if there are no zombies present then return false
        if (SpawnManager.Instance.zombies.Count <= 0) return false;

        // iterate throught list of zombies in SpawnMAnager's Zombie list
        foreach (var mob in SpawnManager.Instance.zombies)
        {
            // Flat position checking to see if the human is near any zombie
            Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 flatZombie = new Vector3(mob.transform.position.x, 0.0f, mob.transform.position.z);

            float dist = Vector3.Distance(flatHuman, flatZombie);

            // update dist if zombie was closer
            if (dist < closestDist) closestDist = dist;
        }

        // return boolean denoting if the closest distance found is smaller than the detection radius
        // aka a zombie is in range of the human
        return closestDist <= zombieDetectionRadius;
    }

    // Mark human as dead
    private void Die()
    {
        isDead = true;
    }

    // Human's Sleep action node
    private NodeResult Sleep()
    {
        float dt = SimulationManager.Instance.GetDNC().SimDeltaTime;

        // regenerate human's energy, set its state to sleeping and invalidate shelter and hunt targets again
        energy = Mathf.Min(energy + sleepEnergyRegen * dt, maxEnergy);

        currentState = HumanState.Sleeping;
        currentShelterTarget = NoShelter; huntTarget = null;

        // Only wake up when FULLY rested
        if (energy >= maxEnergy)
        {
            currentState = HumanState.None; // let the tree pick fresh next tick instead of assuming Wandering
            return NodeResult.Success;
        }

        return NodeResult.Running;
    }

    // Helper function to pick a random tile for human to wander towards
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

    // Helper function to determine the energy decrease in the present moment based on terrain difficulty
    private float GetTerrainTraversalDifficulty() {
        Terrain_cus terrainType = GlobalParam.terrain_map[(int)transform.position.x, (int)transform.position.z];

        // uh i will update this to the proper vars later so using temp vals for now
        return terrainType == Terrain_cus.GRASS ? GlobalParam.m_grassWeight :
            terrainType == Terrain_cus.SAND ? GlobalParam.m_sandWeight :
            terrainType == Terrain_cus.SNOW ? GlobalParam.m_snowWeight :
            1.0f;
    }

    // Human's Wander action node
    private NodeResult Wander()
    {
        // If the Human is not already wandering, set and find goal.
        // If a valid walktable tile is found, set the Human's Pathfinding component's
        // goal to the obtained goal position.
        if (currentState != HumanState.Wandering)
        {
            currentState = HumanState.Wandering;
            if (TryGetRandomWalkableTile(out GridPos goalPos))
                pathfinding.SetGoal(grid.Get_WorldPos(goalPos));
        }

        // Query the Pathfinding component and see if the Human has arrived at its destination.
        // If no, still in progress, so return Running. Otherwise, return Success.
        if (!pathfinding.HasArrived)
        {
            return NodeResult.Running;
        }
        else
        {
            currentState = HumanState.None; // set human state back to none for fresh reroll
            return NodeResult.Success;
        }
    }

    // ------------ AWAKE, START, UPDATE, TICKSTATS ------------
    private void Awake()
    {
        //Debug.Log("HUMANBEHAVIOUR AWAKE - VERSION CHECK X");

        hunger = maxHunger;
        energy = maxEnergy;
        safety = maxSafety;
        repathTimer = 0.0f;
        currentShelterTarget = NoShelter;

        // Priority order, top to bottom:
        // 1. Emergency sleep — set by TickStats BEFORE this tree ticks, so it always wins the same frame energy hits 0.
        // 2. Shelter at night, UNLESS in danger (low safety) — always overrides wandering/hunting
        // 3. Hunt when hungry.
        // 4. Wander otherwise.

        // it behaves normally in day save for hunting, but at night it just doesnt do anything. what
        root = new Selector(
            new Sequence(new ConditionNode(() => { return currentState == HumanState.Sleeping; }), new ActionNode(Sleep)),

            new Sequence(
                new ConditionNode(() => { return (cachedIsNight && safety < safetyPriorityThreshold) || (currentState == HumanState.Sheltering && !cachedIsNight); }),
                new ActionNode(Shelter)
            ),

            new Sequence(new ConditionNode(() => { return (cachedIsHungry);
            }), new ActionNode(Hunt)),

            //new RandomSelector(
            new ActionNode(Wander)
            //    new ActionNode(Idle)
            //)
        );
    }

    private void Start()
    {
        if (grid == null) grid = Grid.Instance;
        firstTimeInit = true;
    }

    private void Update()
    {
        // First Time initialisation - for spawn position and global param stats
        if (firstTimeInit)
        {
            transform.position = grid.TryGetValidSpawn();
            firstTimeInit = false;

            // also initialise the stats from global params here
            InitialiseVariablesFromGlobalParams();
        }

        // Ignore Update() if human is dead
        if (isDead) return;

        // Snapshot the world once, so TickStats and every BT condition this frame see the same values
        cachedIsNight = SimulationManager.Instance.GetDNC().IsNight();
        cachedIsHungry = hunger < hungerHuntThreshold;

        // Human's TickStats()
        TickStats();

        // Second IsDead check here due to TickStats -- human may have died of starvation this frame
        if (isDead) return;

        root.Tick();        // call Tick() on Root
        pathfinding.Tick(); // call Tick() on Pathfinding, in case Human is currently pathfinding
    }

    // Human's TickStats()
    private void TickStats()
    {
        // Get the simulations's current delta time -- make same for all actions below that rely on timer
        float dt = SimulationManager.Instance.GetDNC().SimDeltaTime;

        // Constant decrease for Hunger
        hunger -= hungerDecayRate * dt;
        hunger = Mathf.Max(hunger, 0f);

        // The instant Hunger hits 0, make Human die of starvation
        if (hunger <= 0f)
        {
            Die();
            return;
        }

        // Energy floor check FIRST — so isSafe below reflects the state
        // human is's BT actually about to act on this frame
        // If the Human's energy has hit 0 and it is not currently already Sleeping
        if (energy <= 0.0f && currentState != HumanState.Sleeping)
        {
            currentState = HumanState.Sleeping; // force human to sleep
            huntTarget = null;                  // clear so human won't resume a stale hunt on wake
            currentShelterTarget = NoShelter;   // clear so human won't resume a stale shelter path on wake
        }

        // IsSafe = human is already in shelter
        bool isSafe = currentState == HumanState.Sheltering;

        // if Human isn't sleeping OR in the shelter, initiate decrease for Energy
        if (currentState != HumanState.Sleeping && !isSafe)
        {
            // apply a modifier for the terrain tile the human is currently on
            energy -= energyDecayRate * GetTerrainTraversalDifficulty() * dt;
            //UnityEngine.Debug.Log("current terrain traversal diff " + GetTerrainTraversalDifficulty());
            energy = Mathf.Max(energy, 0f);
        }

        // Apply energy regen ONLY while sleeping
        if (currentState == HumanState.Sleeping)
        {
            energy = Mathf.Min(energy + sleepEnergyRegen * dt, maxEnergy);
        }

        // If it is not night time, allow human to slowly regenerate its safety stat
        if (!cachedIsNight) {
            safety += nightSafetyDecay * safetyRegen * dt;
            safety = Mathf.Min(maxSafety, safety);
        }

        // If the human is safe, regernate its energy and safety.
        if (isSafe)
        {
            energy = Mathf.Min(energy + shelterEnergyRegen * dt, maxEnergy);
            safety = Mathf.Min(safety + shelterSafetyRegen * dt, maxSafety);
        }
        // Otherwise, ONLY if it is nighttime, decrease its safety.
        else if (cachedIsNight)
        {
            // Base safety loss, with a multiplier if the zombie is nearby
            float lossMultiplier = ZombieNearby() ? nightZombieSafetyMultiplier : 1f;
            safety -= nightSafetyDecay * lossMultiplier * dt;
            safety = Mathf.Max(safety, 0f);
        }
    }
}

//private NodeResult Idle()
//{
//    //UnityEngine.Debug.Log("human is trying to idle");
//    if (currentState != HumanState.Idling)
//    {
//        currentState = HumanState.Idling;
//        idleTime = UnityEngine.Random.Range(1.0f, 5.0f);
//    }

//    if (idleTime <= 0.0f)
//    { // return success if idling done
//        //UnityEngine.Debug.Log("human has idled long enough");
//        currentState = HumanState.None;
//        return NodeResult.Success;
//    }

//    return NodeResult.Running;
//}