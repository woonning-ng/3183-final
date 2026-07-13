using System.Collections.Generic;
using UnityEngine;
using static PassiveMobBehaviour;

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
    public float hungerHuntThreshold = 60.0f;
    public float hungerDecayRate = 0.5f; // per tick?
    public float nightZombieSafetyMultiplier = 2.0f;
    public float shelterEnergyRegen = 3.0f;
    public float shelterSafetyRegen = 3.0f;
    public float nightSafetyDecay = 1.0f;
    public float sleepEnergyRegen = 1.5f;
    public float hungerRestoreAmount = 20.0f;

    public float repathTimer = 1.0f;
    public float repathInterval = 1.0f;

    private GameObject huntTarget; // passive mob
    public float eatDistance = 5.0f;

    public float energyDecayRate = 1.0f; // perhaps can add a multiplier when theres other stuff gg on

    bool placeHuman = false;
    public float zombieDetectionRadius = 20.0f;

    // TODO: clean up all these later omg
    // also how to make it be linked to sim time rather than just time deltatime

    public enum HumanState { None, Wandering, Hunting, SeekingShelter, Sheltering, Sleeping }
    [SerializeField] private HumanState currentState = HumanState.None;

    //[SerializeField] private LayerMask shelterLayer;
    //[SerializeField] private float shelterSearchRadius = 200f; // effectively "whole map" if shelters are sparse

    private Vector3 currentShelterTarget;

    public bool IsHumanDead() { return isDead; }
    public string GetHumanState() {
        switch (currentState)
        {
            case HumanState.Wandering: return "Wandering";
            case HumanState.Hunting: return "Hunting";
            case HumanState.SeekingShelter: return "Seeking Shelter";
            case HumanState.Sheltering: return "Sheltering";
            case HumanState.Sleeping: return "Sleeping";
            case HumanState.None: return "Sleeping";
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

        // bro wtaf i dont get it why is it NOT doing this shit properly im just going to yeet the whole bt atp 

        root = new Selector(
            // forced sleep sequence if human runs out of energy
            new Sequence(new ConditionNode(() => (currentState == HumanState.Sleeping)), new ActionNode(Sleep)), // why

            new Sequence(
                new ConditionNode(() => {
                    bool night = SimulationManager.Instance.GetDNC().IsNight();
                    bool humanHungry = (hunger < hungerHuntThreshold);
                    Debug.Log("isNight check: " + night);
                    return night && !humanHungry;
                }),
                new ActionNode(Shelter)
            ),

            // otherwise: normal hunger-first priority
            new Sequence(new ConditionNode(() => hunger < hungerHuntThreshold), new ActionNode(Hunt)), // why hooman not hunting
            //new Sequence(new ConditionNode(() => SimulationManager.Instance.GetDNC().IsNight()), new ActionNode(Shelter)),

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

        bool isSafe = (currentState == HumanState.Sheltering); // arrived only, not SeekingShelter
        bool isNight = SimulationManager.Instance.GetDNC().IsNight();

        if (energy <= 0.0f)
        {
            currentState = HumanState.Sleeping;
        }

        if ((currentState != HumanState.Sleeping) && (!isSafe))
        {
            energy -= energyDecayRate * SimulationManager.Instance.GetDNC().SimDeltaTime;
            energy = Mathf.Max(energy, 0f);
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
            Debug.Log($"safety decaying: {safety}, state={currentState}, isNight={isNight}");
        }
    }

    private bool ZombieNearby() {
        float closestDist = float.MaxValue;

        foreach (var mob in SpawnManager.Instance.zombies)
        {
            // take into account the FLAT distance here for both
            Vector3 flatHuman = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 flatZombie = new Vector3(mob.transform.position.x, 0.0f, mob.transform.position.z);

            float dist = Vector3.Distance(flatHuman, flatZombie);
            if (dist < closestDist)
            {
                closestDist = dist;
            }
        }

        return (closestDist <= zombieDetectionRadius);
    }

    private bool TryFindHuntTarget(out GameObject target) {
        float closestDist = float.MaxValue;
        target = null;

        foreach (var mob in SpawnManager.Instance.passiveMobs)
        {
            // take into account the FLAT distance here for both
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
            currentState = HumanState.None; // reset it
            return NodeResult.Success;
        }
    }

    private NodeResult Sleep()
    { 
        // if it is for some reason NOT sleeping. why do you do this to me.
        if (currentState != HumanState.Sleeping) // first entry only
        {
            huntTarget = null;
            currentShelterTarget = new Vector3(-1, -1, -1);
        }
        currentState = HumanState.Sleeping;

        if (energy >= maxEnergy * 0.5f)
        {
            currentState = HumanState.Wandering;
            return NodeResult.Success;
        }

        return NodeResult.Running;
    }

    private NodeResult Hunt()
    {
        currentShelterTarget = new Vector3(-1, -1, -1); // invalidate stale shelter target on interrupt

        if (huntTarget == null && !TryFindHuntTarget(out huntTarget))
        {
            Debug.Log("Hunt failed: no target found (passiveMobs count: " + SpawnManager.Instance.passiveMobs.Count + ")");
            return NodeResult.Failure; // no mobs in range — fall through to shelter/wander
        }

        currentState = HumanState.Hunting; // only set if hunt target found

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