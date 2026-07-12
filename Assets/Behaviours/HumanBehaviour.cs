using UnityEngine;
using System.Collections.Generic;

public class HumanBehaviour : MonoBehaviour
{
    [SerializeField] public Pathfinding pathfinding;
    [SerializeField] public Grid grid;
    [SerializeField] private int maxPlacementAttempts = 30;

    private BTNode root;

    // actions:
    // wander, seek shelter, run to safety, look for food, kill passive mob, fall asleep, die

    public float maxHunger = 100f;
    public float maxEnergy = 100f;
    public float maxSafety = 100f;
    public float hunger;
    public float energy;
    public float safety;

    bool isDead = false;

    public float safetyPriorityThreshold = 20.0f;
    public float hungerHuntThreshold = 40.0f;
    public float hungerDecayRate = 0.5f; // per tick?
    public float nightZombieSafetyMultiplier = 2.0f;
    public float shelterEnergyRegen = 3.0f;
    public float shelterSafetyRegen = 3.0f;
    public float nightSafetyDecay = 1.0f;
    public float sleepEnergyRegen = 1.5f;
    public float hungerRestoreAmount = 20.0f;

    public float repathTimer = 10.0f;
    public float repathInterval = 10.0f;

    private GameObject huntTarget; // passive mob
    public float eatDistance = 5.0f;

    public float energyDecayRate = 1.0f; // perhaps can add a multiplier when theres other stuff gg on

    bool placeHuman = false;

    // TODO: clean up all these later omg
    // also how to make it be linked to sim time rather than just time deltatime

    public enum HumanState { Wandering, Hunting, SeekingShelter, Sheltering, Sleeping }
    private HumanState currentState = HumanState.Wandering;

    //[SerializeField] private LayerMask shelterLayer;
    //[SerializeField] private float shelterSearchRadius = 200f; // effectively "whole map" if shelters are sparse

    private Vector3 currentShelterTarget;

    public string GetHumanState() {
        switch (currentState)
        {
            case HumanState.Wandering: return "Wandering";
            case HumanState.Hunting: return "Hunting";
            case HumanState.SeekingShelter: return "Seeking Shelter";
            case HumanState.Sheltering: return "Sheltering";
            case HumanState.Sleeping: return "Sleeping";
            default: return "Unknown";
        }
    }

    private bool TryFindNearestShelter(out Vector3 shelter)
    {
        // get all safe spaces in the map
        List<Vector3> shelters = Grid.Instance.terrainGen.GetAllSafe();

        shelter = new Vector3(-1, -1, -1);
        float closestDist = float.MaxValue;

        foreach (var hit in shelters)
        {
            // take into account the FLAT distance here for both
            Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 flatShelter = new Vector3(hit.x, 0.0f, hit.z);

            float dist = Vector3.Distance(flatHuman, flatShelter);
            if (dist < closestDist)
            {
                closestDist = dist;
                shelter = hit; // use absolute
            }
        }

        return shelter != new Vector3(-1, -1, -1);
    }

    private void Awake()
    {
        // reset these per iteration
        hunger = maxHunger;
        energy = maxEnergy;
        safety = maxSafety;
        repathTimer = 0.0f;
        currentShelterTarget = new Vector3(-1, -1, -1);

        // bt stuff here

        // by default, the human wanders
        // if its hunger drops below a certain threshold it should find the nearest passive mob and route towards it
        // upon getting within a certain radius, refill hunger and mark passive mob as dead

        // if nighttime, human prioritises shelter unless hungry. shelter refills energy and safety but still decreases hunger.
        // if human in shelter, it can wake up when hunger is below a certain threshold and go hunt. shelter human is immune to
        // safety decrease even if a zombie is lingering about.
        // nighttime decreases safety by default but being in radius of a zombie multiplies the loss.

        // if human energy drops to 0, it stays still and sleeps until energy is halfway full.

        // if human hunger drops to 0, human dies, restart sim

        // how do i use the human states to do this instead?

        root = new Selector(
            new Sequence(new ConditionNode(() => (currentState == HumanState.Sleeping)), new ActionNode(Sleep)),

            // low safety: shelter takes priority over hunger - EvaluateAndAct??
            new Sequence(
                new ConditionNode(() => ((safety <= safetyPriorityThreshold) && (SimulationManager.Instance.GetDNC().IsNight()))),
                new ActionNode(Shelter)
            ),

            // otherwise: normal hunger-first priority
            new Sequence(new ConditionNode(() => hunger < hungerHuntThreshold), new ActionNode(Hunt)),
            new Sequence(new ConditionNode(() => SimulationManager.Instance.GetDNC().IsNight()), new ActionNode(Shelter)),

            new ActionNode(Wander)
        );
    }

    private void Start()
    {
        if (grid == null) grid = Grid.Instance;
        placeHuman = true;
    }

    private void Update()
    {
        if (placeHuman) // no choice but to drop it here ig
        {
            // spawn human at a valid spot
            transform.position = grid.TryGetValidSpawn();
            placeHuman = false;
        }

        if (isDead) return;

        TickStats();

        if (isDead) return; // hunger may have just killed us this frame

        root.Tick();
        pathfinding.Tick();
    }

    private void TickStats()
    {
        hunger -= hungerDecayRate * SimulationManager.Instance.GetDNC().SimDeltaTime;
        hunger = Mathf.Max(hunger, 0f);

        if (hunger <= 0f)
        {
            Die();
            return;
        }

        bool isSafe = currentState == HumanState.Sheltering; // arrived only, not SeekingShelter
        bool isNight = SimulationManager.Instance.GetDNC().IsNight();

        if (currentState != HumanState.Sleeping || !isSafe) {
            energy -= energyDecayRate * SimulationManager.Instance.GetDNC().SimDeltaTime;
        }

        if (energy <= 0.0f) {
            currentState = HumanState.Sleeping;
        }

        if (isSafe)
        {
            energy = Mathf.Min(energy + shelterEnergyRegen * SimulationManager.Instance.GetDNC().SimDeltaTime, maxEnergy);
            safety = Mathf.Min(safety + shelterSafetyRegen * SimulationManager.Instance.GetDNC().SimDeltaTime, maxSafety);
        }
        else if (isNight)
        {
            float lossMultiplier = ZombieNearby() ? nightZombieSafetyMultiplier : 1f;
            safety -= nightSafetyDecay * lossMultiplier * SimulationManager.Instance.GetDNC().SimDeltaTime;
            safety = Mathf.Max(safety, 0f);
        }
    }

    private bool ZombieNearby() {
        // TODO: where zombo
        return false;
    }

    private bool TryFindHuntTarget(out GameObject target) {
        //currentState = HumanState.Hunting;

        // TODO: how find
        target = null;
        return false;
    }

    private NodeResult Wander()
    {
        currentState = HumanState.Wandering;

        if (!pathfinding.HasArrived)
            return NodeResult.Running;

        if (TryGetRandomWalkableTile(out GridPos goalPos))
            pathfinding.SetGoal(grid.Get_WorldPos(goalPos));

        return NodeResult.Running; // never "succeeds" — wandering is a loop, not a goal state
    }

    private NodeResult Sleep()
    {
        currentState = HumanState.Sleeping;
        energy += sleepEnergyRegen * SimulationManager.Instance.GetDNC().SimDeltaTime;

        UnityEngine.Debug.Log("hooman is sleeping");

        if (energy >= maxEnergy * 0.5f)
        {
            currentState = HumanState.Wandering; // remove it from sleep mode
            return NodeResult.Success; // "wakes up when energy is halfway full"
        }

        return NodeResult.Running;
    }

    private NodeResult Hunt()
    {
        currentShelterTarget = new Vector3(-1, -1, -1); // invalidate stale shelter target on interrupt
        currentState = HumanState.Hunting;

        if (huntTarget == null && !TryFindHuntTarget(out huntTarget))
            return NodeResult.Failure; // no mobs in range — fall through to shelter/wander

        float dist = Vector3.Distance(transform.position, huntTarget.transform.position);

        if (dist <= eatDistance)
        {
            hunger = Mathf.Min(hunger + hungerRestoreAmount, maxHunger);
            huntTarget.GetComponent<PassiveMobBehaviour>().MarkDead();
            huntTarget = null;
            return NodeResult.Success;
        }

        repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        if (repathTimer <= 0f)
        {
            pathfinding.SetGoal(huntTarget.transform.position);
            repathTimer = repathInterval;
        }

        return NodeResult.Running;
    }

    private bool AtShelter(Vector3 shelter, float arriveDistance = 1.5f)
    {
        currentState = HumanState.Sheltering;

        // once again, flat dist
        Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 flatShelter = new Vector3(shelter.x, 0.0f, shelter.z);

        return Vector3.Distance(flatHuman, flatShelter) <= arriveDistance;
    }

    private NodeResult Shelter()
    {
        if (currentShelterTarget == new Vector3(-1, -1, -1))
        {
            currentState = HumanState.SeekingShelter; // exposed, no target yet
            if (!TryFindNearestShelter(out currentShelterTarget))
                return NodeResult.Failure;
        }

        if (!AtShelter(currentShelterTarget))
        {
            currentState = HumanState.SeekingShelter; // still travelling, still exposed

            repathTimer -= SimulationManager.Instance.GetDNC().SimDeltaTime;
            if (repathTimer <= 0f)
            {
                pathfinding.SetGoal(currentShelterTarget);
                repathTimer = repathInterval;
            }
            return NodeResult.Running;
        }

        currentState = HumanState.Sheltering; // arrived — now safe
        return NodeResult.Running;
    }

    private void Die()
    {
        isDead = true;
        // trigger restart-sim logic here — whatever that hook is
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