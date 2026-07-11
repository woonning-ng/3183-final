using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

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
    public float nightSafetyDecay = 1.0f;
    public float sleepEnergyRegen = 1.5f;
    public float hungerRestoreAmount = 20.0f;

    public float repathTimer = 10.0f;
    public float repathInterval = 10.0f;

    private GameObject huntTarget; // passive mob
    public float eatDistance = 5.0f;

    public float energyDecayRate = 1.0f; // perhaps can add a multiplier when theres other stuff gg on

    // TODO: clean up all these later omg
    // also how to make it be linked to sim time rather than just time deltatime

    public enum HumanState { Wandering, Hunting, SeekingShelter, Sheltering, Sleeping }
    private HumanState currentState;

    [SerializeField] private LayerMask shelterLayer;
    [SerializeField] private float shelterSearchRadius = 200f; // effectively "whole map" if shelters are sparse

    private Transform currentShelterTarget;

    private bool TryFindNearestShelter(out Transform shelter)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, shelterSearchRadius, shelterLayer);

        shelter = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                shelter = hit.transform;
            }
        }

        return shelter != null;
    }

    private void Awake()
    {
        // reset these per iteration
        hunger = maxHunger;
        energy = maxEnergy;
        safety = maxSafety;

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

        root = new Selector(
            new Sequence(new ConditionNode(() => (currentState == HumanState.Sleeping)), new ActionNode(Sleep)),

            // low safety: shelter takes priority over hunger - EvaluateAndAct??
            new Sequence(
                new ConditionNode(() => safety <= safetyPriorityThreshold && SimulationManager.Instance.GetDNC().IsNight()),
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
    }

    private void Update()
    {
        if (isDead) return;

        TickStats();

        if (isDead) return; // hunger may have just killed us this frame

        root.Tick();
        pathfinding.Tick();
    }

    private void TickStats()
    {
        hunger -= hungerDecayRate * Time.deltaTime;
        hunger = Mathf.Max(hunger, 0f);

        if (hunger <= 0f)
        {
            Die();
            return;
        }

        bool isSafe = currentState == HumanState.Sheltering; // arrived only, not SeekingShelter
        bool isNight = SimulationManager.Instance.GetDNC().IsNight();

        if (currentState != HumanState.Sleeping || !isSafe) {
            energy -= energyDecayRate * Time.deltaTime;
        }

        if (energy <= 0.0f) {
            currentState = HumanState.Sleeping;
        }

        if (isSafe)
        {
            energy = Mathf.Min(energy + shelterEnergyRegen * Time.deltaTime, maxEnergy);
        }
        else if (isNight)
        {
            float lossMultiplier = ZombieNearby() ? nightZombieSafetyMultiplier : 1f;
            safety -= nightSafetyDecay * lossMultiplier * Time.deltaTime;
            safety = Mathf.Max(safety, 0f);
        }
    }

    private bool ZombieNearby() {
        // TODO: where zombo
        return false;
    }

    private bool TryFindHuntTarget(out GameObject target) {
        // TODO: how find
        target = null;
        return false;
    }

    private NodeResult Wander()
    {
        if (!pathfinding.HasArrived)
            return NodeResult.Running;

        if (TryGetRandomWalkableTile(out GridPos goalPos))
            pathfinding.SetGoal(grid.Get_WorldPos(goalPos));

        return NodeResult.Running; // never "succeeds" — wandering is a loop, not a goal state
    }

    private NodeResult Sleep()
    {
        currentState = HumanState.Sleeping;
        energy += sleepEnergyRegen * Time.deltaTime;

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
        currentShelterTarget = null; // invalidate stale shelter target on interrupt
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

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            pathfinding.SetGoal(huntTarget.transform.position);
            repathTimer = repathInterval;
        }

        return NodeResult.Running;
    }

    private bool AtShelter(Transform shelter, float arriveDistance = 1.5f)
    {
        return Vector3.Distance(transform.position, shelter.position) <= arriveDistance;
    }

    private NodeResult Shelter()
    {
        if (currentShelterTarget == null)
        {
            currentState = HumanState.SeekingShelter; // exposed, no target yet
            if (!TryFindNearestShelter(out currentShelterTarget))
                return NodeResult.Failure;
        }

        if (!AtShelter(currentShelterTarget))
        {
            currentState = HumanState.SeekingShelter; // still travelling, still exposed

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                pathfinding.SetGoal(currentShelterTarget.position);
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