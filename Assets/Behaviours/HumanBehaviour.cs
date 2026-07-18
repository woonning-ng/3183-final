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

    // one-time call to place the human at a suitable location
    bool placeHuman = false;

    // Human's cached IsNight() and IsHungry()
    // cached once per frame so every branch of the tree + TickStats agree on the same snapshot
    private bool cachedIsNight;
    private bool cachedIsHungry;

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

    private bool TryFindNearestShelter(out Vector3 shelter)
    {
        List<Vector3> shelters = Grid.Instance.terrainGen.GetAllSafe();

        shelter = NoShelter;
        float closestDist = float.MaxValue;

        foreach (var hit in shelters)
        {
            Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 flatShelter = new Vector3(hit.x, 0.0f, hit.z);

            float dist = Vector3.Distance(flatHuman, flatShelter);
            if (dist < closestDist)
            {
                closestDist = dist;
                shelter = hit;
            }
        }

        UnityEngine.Debug.Log("human found shelter");
        return shelter != NoShelter;
    }

    private bool AtShelter(Vector3 shelter, float arriveDistance = 1.5f)
    {
        // pure query now — no side effects on currentState
        Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 flatShelter = new Vector3(shelter.x, 0.0f, shelter.z);
        return Vector3.Distance(flatHuman, flatShelter) <= arriveDistance;
    }

    private NodeResult Shelter()
    {
        UnityEngine.Debug.Log("human is sheltering");
        if (currentShelterTarget == NoShelter)
        {
            if (!TryFindNearestShelter(out currentShelterTarget))
                return NodeResult.Failure; // falls through to Wander this same tick, cleanly

            currentState = HumanState.SeekingShelter;
        }

        if (!AtShelter(currentShelterTarget))
        {
            currentState = HumanState.SeekingShelter;

            repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
            if (repathTimer <= 0f)
            {
                pathfinding.SetGoal(currentShelterTarget);
                repathTimer = repathInterval;
            }
            return NodeResult.Running;
        }

        currentState = HumanState.Sheltering;
        return NodeResult.Success;
    }

    private bool TryFindHuntTarget(out GameObject target)
    {
        float closestDist = float.MaxValue;
        target = null;

        if (SpawnManager.Instance.passiveMobs.Count <= 0) return false; // skip if no passive mobs at all
        foreach (var mob in SpawnManager.Instance.passiveMobs)
        {
            Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 flatMob = new Vector3(mob.transform.position.x, 0.0f, mob.transform.position.z);

            float dist = Vector3.Distance(flatHuman, flatMob);
            if (dist < closestDist)
            {
                closestDist = dist;
                target = mob;
            }
        }

        UnityEngine.Debug.Log("found a target? " + target == null);
        return target != null;
    }

    private NodeResult Hunt()
    {
        if (huntTarget == null && !TryFindHuntTarget(out huntTarget))
            return NodeResult.Failure;

        currentState = HumanState.Hunting;
        currentShelterTarget = NoShelter;

        Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 flatMob = new Vector3(huntTarget.transform.position.x, 0.0f, huntTarget.transform.position.z);
        float dist = Vector3.Distance(flatHuman, flatMob);

        if (dist <= eatDistance)
        {
            hunger = Mathf.Min(hunger + hungerRestoreAmount, maxHunger);
            huntTarget.GetComponent<PassiveMobBehaviour>().MarkDead();
            huntTarget = null;
            huntPathFailures = 0;
            return NodeResult.Success;
        }

        repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (repathTimer <= 0f)
        {
            bool reachable = pathfinding.TrySetGoal(huntTarget.transform.position);
            repathTimer = repathInterval;

            if (!reachable)
            {
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
                huntPathFailures = 0;
            }
        }

        return NodeResult.Running;
    }

    private bool ZombieNearby()
    {
        float closestDist = float.MaxValue;

        if (SpawnManager.Instance.zombies.Count <= 0) return false; // BRO
        foreach (var mob in SpawnManager.Instance.zombies)
        {
            Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 flatZombie = new Vector3(mob.transform.position.x, 0.0f, mob.transform.position.z);

            float dist = Vector3.Distance(flatHuman, flatZombie);
            if (dist < closestDist) closestDist = dist;
        }

        return closestDist <= zombieDetectionRadius;
    }

    private void Die()
    {
        isDead = true;
    }

    private NodeResult Sleep()
    {
        // currentState is already Sleeping by the time this runs (set in TickStats)
        float dt = SimulationManager.Instance.GetDNC().SimDeltaTime;
        bool night = SimulationManager.Instance.GetDNC().IsNight();
        float before = energy;
        energy = Mathf.Min(energy + sleepEnergyRegen * dt, maxEnergy);
        UnityEngine.Debug.Log("the dumbass human is sleeping");
        //Debug.Log($"[Sleep] night={night} dt={dt} before={before} after={energy}");

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
        UnityEngine.Debug.Log("human is trying to wander");
        // if not already wandering, set and find goal
        if (currentState != HumanState.Wandering)
        {
            currentState = HumanState.Wandering;
            if (TryGetRandomWalkableTile(out GridPos goalPos))
            {
                pathfinding.SetGoal(grid.Get_WorldPos(goalPos));
            }
        }

        if (!pathfinding.HasArrived)
        {
            return NodeResult.Running;
        }
        else
        {
            //UnityEngine.Debug.Log("human has reached");
            currentState = HumanState.None;
            return NodeResult.Success;
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
        placeHuman = true;
    }

    private void Update()
    {
        if (placeHuman)
        {
            transform.position = grid.TryGetValidSpawn();
            placeHuman = false;

            // also initialise the stats from global params here
        }

        if (isDead) return;

        // snapshot the world once, so TickStats and every BT condition this frame see the same values
        cachedIsNight = SimulationManager.Instance.GetDNC().IsNight();
        cachedIsHungry = hunger < hungerHuntThreshold;

        TickStats();

        if (isDead) return; // hunger may have just killed us this frame

        root.Tick();
        pathfinding.Tick();
    }

    private void TickStats()
    {
        float dt = SimulationManager.Instance.GetDNC().SimDeltaTime;

        hunger -= hungerDecayRate * dt;
        hunger = Mathf.Max(hunger, 0f);

        if (hunger <= 0f)
        {
            Die();
            return;
        }

        // energy floor check FIRST — so isSafe below reflects the state we're actually about to act on this frame
        if (energy <= 0.0f && currentState != HumanState.Sleeping)
        {
            UnityEngine.Debug.Log("force human to sleep");
            currentState = HumanState.Sleeping;
            huntTarget = null;              // clear so we don't resume a stale hunt on wake
            currentShelterTarget = NoShelter; // clear so we don't resume a stale shelter path on wake
        }

        bool isSafe = currentState == HumanState.Sheltering;

        if (currentState != HumanState.Sleeping && !isSafe)
        {
            energy -= energyDecayRate * dt;
            energy = Mathf.Max(energy, 0f);
        }

        // Apply energy regen ONLY while sleeping
        if (currentState == HumanState.Sleeping)
        {
            energy = Mathf.Min(energy + sleepEnergyRegen * dt, maxEnergy);
        }

        // help this dumbass regain safety
        if (!cachedIsNight) {
            safety += nightSafetyDecay * safetyRegen * dt;
            safety = Mathf.Min(maxSafety, safety);
        }

        if (isSafe)
        {
            energy = Mathf.Min(energy + shelterEnergyRegen * dt, maxEnergy);
            safety = Mathf.Min(safety + shelterSafetyRegen * dt, maxSafety);
        }
        else if (cachedIsNight)
        {
            float lossMultiplier = ZombieNearby() ? nightZombieSafetyMultiplier : 1f;
            safety -= nightSafetyDecay * lossMultiplier * dt;
            safety = Mathf.Max(safety, 0f);
        }
    }

}