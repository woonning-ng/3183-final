using UnityEngine;

public class HumanBehaviour : MonoBehaviour
{
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Grid grid;
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

    private void Awake()
    {
        // reset these per iteration
        hunger = maxHunger;
        energy = maxEnergy;
        safety = maxSafety;

        // bt stuff here
        root = new ActionNode(() => Wander());

        // by default, the human wanders
        // if its hunger drops below a certain threshold it should find the nearest passive mob and route towards it
        // upon getting within a certain radius, refill hunger and mark passive mob as dead

        // if nighttime, human prioritises shelter unless hungry. shelter refills energy and safety but still decreases hunger.
        // if human in shelter, it can wake up when hunger is below a certain threshold and go hunt. shelter human is immune to
        // safety decrease even if a zombie is lingering about.
        // nighttime decreases safety by default but being in radius of a zombie multiplies the loss.

        // if human energy drops to 0, it stays still and sleeps until energy is halfway full.

        // if human hunger drops to 0, human dies, restart sim
    }

    private void Start()
    {
        if (grid == null) grid = Grid.Instance;
    }

    private void Update()
    {
        root.Tick();
        pathfinding.Tick();

        if (hunger <= 0) {
            isDead = true;
        }
    }

    private NodeResult Wander()
    {
        if (!pathfinding.HasArrived)
            return NodeResult.Running;

        if (TryGetRandomWalkableTile(out GridPos goalPos))
            pathfinding.SetGoal(grid.Get_WorldPos(goalPos));

        return NodeResult.Running; // never "succeeds" — wandering is a loop, not a goal state
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