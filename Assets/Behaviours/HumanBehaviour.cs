using System.Collections.Generic;
using UnityEngine;
using static ZombieBehaviour;

public class HumanBehaviour : MonoBehaviour
{
    [SerializeField] public Pathfinding pathfinding;
    [SerializeField] public Grid grid;
    [SerializeField] private int maxPlacementAttempts = 30;

    private BTNode root;

    public float maxHunger = 100f;
    public float maxEnergy = 100f;
    public float maxSafety = 100f;
    public float hunger;
    public float energy;
    public float safety;

    bool isDead = false;

    public float idleTime = 0.5f;
    public float maxIdleTime = 0.5f;

    public float safetyPriorityThreshold = 20.0f;
    public float hungerHuntThreshold = 60.0f;
    public float hungerDecayRate = 0.5f;
    public float nightZombieSafetyMultiplier = 2.0f;
    public float shelterEnergyRegen = 3.0f;
    public float shelterSafetyRegen = 3.0f;
    public float nightSafetyDecay = 1.0f;
    public float sleepEnergyRegen = 1.5f;
    public float hungerRestoreAmount = 20.0f;

    public float repathTimer = 0.0f;
    public float repathInterval = 1.0f;

    private GameObject huntTarget;
    public float eatDistance = 5.0f;

    public float energyDecayRate = 1.0f;

    bool placeHuman = false;
    public float zombieDetectionRadius = 20.0f;

    public enum HumanState { None, Wandering, Hunting, SeekingShelter, Sheltering, Sleeping, Idling }
    [SerializeField] private HumanState currentState = HumanState.None;

    private Vector3 currentShelterTarget;
    private static readonly Vector3 NoShelter = new Vector3(-1, -1, -1);

    // cached once per frame so every branch of the tree + TickStats agree on the same snapshot
    private bool cachedIsNight;
    private bool cachedIsHungry;

    public bool IsHumanDead() { return isDead; }

    public string GetHumanState()
    {
        switch (currentState)
        {
            case HumanState.Wandering: return "Wandering";
            case HumanState.Idling: return "Idling";
            case HumanState.Hunting: return "Hunting";
            case HumanState.SeekingShelter: return "Seeking Shelter";
            case HumanState.Sheltering: return "Sheltering";
            case HumanState.Sleeping: return "Sleeping";
            case HumanState.None: return "None";
            default: return "Unknown";
        }
    }

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

        return shelter != NoShelter;
    }

    private void Awake()
    {
        Debug.Log("HUMANBEHAVIOUR AWAKE - VERSION CHECK X");

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
                new ConditionNode(() => { return (cachedIsNight && safety < safetyPriorityThreshold); }),
                new ActionNode(Shelter)
            ),

            new Sequence(new ConditionNode(() => { return (cachedIsHungry);
            }), new ActionNode(Hunt)),

            new RandomSelector(
                new ActionNode(Wander),
                new ActionNode(Idle)
            )
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

    private bool TryFindHuntTarget(out GameObject target)
    {
        float closestDist = float.MaxValue;
        target = null;

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

        return target != null;
    }

    private NodeResult Wander()
    {
        UnityEngine.Debug.Log("human is trying to wander");
        // if not already wandering, set and find goal
        if (currentState != HumanState.Wandering)
        {
            currentState = HumanState.Wandering;
            if (TryGetRandomWalkableTile(out GridPos goalPos))
                pathfinding.SetGoal(grid.Get_WorldPos(goalPos));
        }

        if (!pathfinding.HasArrived)
        {
            return NodeResult.Running;
        }
        else
        {
            UnityEngine.Debug.Log("human has reached");
            currentState = HumanState.None;
            return NodeResult.Success;
        }
    }

    private NodeResult Idle()
    {
        UnityEngine.Debug.Log("human is trying to idle");
        if (currentState != HumanState.Idling)
        {
            currentState = HumanState.Idling;
            idleTime = UnityEngine.Random.Range(1.0f, 5.0f);
        }

        if (idleTime <= 0.0f)
        { // return success if idling done
            UnityEngine.Debug.Log("human has idled long enough");
            currentState = HumanState.None;
            return NodeResult.Success;
        }

        return NodeResult.Running;
    }

    private NodeResult Sleep()
    {
        // currentState is already Sleeping by the time this runs (set in TickStats)
        float dt = SimulationManager.Instance.GetDNC().SimDeltaTime;
        bool night = SimulationManager.Instance.GetDNC().IsNight();
        float before = energy;
        energy = Mathf.Min(energy + sleepEnergyRegen * dt, maxEnergy);
        Debug.Log($"[Sleep] night={night} dt={dt} before={before} after={energy}");

        // Only wake up when FULLY rested
        if (energy >= maxEnergy)
        {
            currentState = HumanState.None; // let the tree pick fresh next tick instead of assuming Wandering
            return NodeResult.Success;
        }

        return NodeResult.Running;
    }

    private NodeResult Hunt()
    {
        UnityEngine.Debug.Log("hunting for tgt");
        // don't touch currentState yet — only commit once we know there's a real target
        if (huntTarget == null && !TryFindHuntTarget(out huntTarget))
            return NodeResult.Failure; // falls through to Wander this same tick, cleanly

        currentState = HumanState.Hunting;
        currentShelterTarget = NoShelter; // invalidate stale shelter target on interrupt

        Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 flatMob = new Vector3(huntTarget.transform.position.x, 0.0f, huntTarget.transform.position.z);

        float dist = Vector3.Distance(flatHuman, flatMob);

        if (dist <= eatDistance)
        {
            UnityEngine.Debug.Log("did the human eat at all");
            hunger = Mathf.Min(hunger + hungerRestoreAmount, maxHunger);
            huntTarget.GetComponent<PassiveMobBehaviour>().MarkDead();
            huntTarget = null;
            return NodeResult.Success;
        }

        repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (repathTimer <= 0f)
        {
            UnityEngine.Debug.Log("repathing to find tgt");
            pathfinding.SetGoal(huntTarget.transform.position);
            repathTimer = repathInterval;
        }

        return NodeResult.Running;
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
        return NodeResult.Running;
    }

    private void Die()
    {
        isDead = true;
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