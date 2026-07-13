using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // this needs to be tied to the sim manager/astar right

    [SerializeField] public int maxMobs = 10;
    [SerializeField] public int maxZombies = 10;

    [SerializeField] public float passiveMobRespawnTimer = 5.0f;
    [SerializeField] public float zombieRespawnTimer = 10.0f;

    public float pmRespawn = 0.0f;
    public float zbRespawn = 0.0f;

    // container of passive mobs (spawns throughout day)
    public List<GameObject> passiveMobs;

    // container of zombies (only active if IsNight == true)
    public List<GameObject> zombies;

    public GameObject passiveMobPrefab;
    public GameObject zombiePrefab;

    public GameObject human;

    public static SpawnManager Instance;

    // spawn passive mob fn
    public void SpawnPassiveMob() {
        if (passiveMobs.Count < maxMobs) {
            // spawn it at 0 0 0 for now
            Vector3 spawnPt = Grid.Instance.TryGetValidSpawn();
            GameObject mob = Instantiate(passiveMobPrefab, spawnPt, Quaternion.identity);

            // set script specific fields sobs
            mob.GetComponent<PassiveMobBehaviour>().pathfinding = mob.GetComponent<Pathfinding>();
            mob.GetComponent<Pathfinding>().SetGrid(Grid.Instance);

            passiveMobs.Add(mob);
        }
    }

    // spawn zombie fn
    public void SpawnZombie() {
        if (zombies.Count < maxZombies)
        {
            // spawn it at 0 0 0 for now
            Vector3 spawnPt = Grid.Instance.TryGetValidSpawn();
            GameObject mob = Instantiate(zombiePrefab, spawnPt, Quaternion.identity);

            // set script specific fields sobs
            mob.GetComponent<ZombieBehaviour>().pathfinding = mob.GetComponent<Pathfinding>();
            mob.GetComponent<ZombieBehaviour>().humanTarget = human; // set human as target so that it can detect if human is nearbby
            mob.GetComponent<Pathfinding>().SetGrid(Grid.Instance);

            zombies.Add(mob);
        }
    }

    // kill all zombies fn
    public void KillAllZombies() {
        foreach (GameObject go in zombies) {
            Destroy(go);
        }

        zombies.Clear();
    }

    private void Awake()
    {
        if (Instance == null) // what if i kill myself right now
        {
            Instance = this;
        }
    }

    private void Start()
    {
        zbRespawn = 2.0f;
        pmRespawn = 2.0f;
    }

    private void Update()
    {
        // should be managing entities here
        zbRespawn -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        pmRespawn -= SimulationManager.Instance.GetDNC().SimDeltaTime;


        // check if a passive mob has been marked for death by player
        // despawn the passive mob if needed

        if (pmRespawn <= 0.0f)
        {
            SpawnPassiveMob();
            pmRespawn = passiveMobRespawnTimer;
        }

        // zombie checks
        // check 1: check if it's night
        // if it is nighttime, allwo zombies to spawn at an interval
        // otherwise if it is daytime
        // check to see if the size of the zombie vector is > 0. if it is, kill all zombies

        if (SimulationManager.Instance.GetDNC().IsNight())
        {
            if (zbRespawn <= 0.0f) {
                SpawnZombie();
                zbRespawn = zombieRespawnTimer;
            }
        }
        else {
            if (zombies.Count > 0) {
                KillAllZombies();
            }
        }

        // flag dead ones here first
        List<int> deadMobs = new List<int>();

        for (int i = 0; i < passiveMobs.Count; i++) {
            if (passiveMobs[i].GetComponent<PassiveMobBehaviour>().IsDead()) {
                deadMobs.Add(i);
            }
        }

        // THEN remove the dead ones here
        foreach (int i in deadMobs) {
            Destroy(passiveMobs[i]);
            passiveMobs.Remove(passiveMobs[i]);
        }
    }
}
