using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.PlayerSettings;

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

    // spawn passive mob fn
    public void SpawnPassiveMob() {
        if (passiveMobs.Capacity < maxMobs) {
            // spawn it at 0 0 0 for now
            GameObject mob = Instantiate(passiveMobPrefab, new Vector3(0, 0, 0), Quaternion.identity);

            // set script specific fields sobs
            mob.GetComponent<PassiveMobBehaviour>().pathfinding = mob.GetComponent<Pathfinding>();
            mob.GetComponent<Pathfinding>().SetGrid(GameObject.Find("Grid").GetComponent<Grid>());

            passiveMobs.Add(mob);
        }
    }

    // spawn zombie fn
    public void SpawnZombie() {
        if (zombies.Capacity < maxZombies)
        {
            // spawn it at 0 0 0 for now
            GameObject mob = Instantiate(zombiePrefab, new Vector3(0, 0, 0), Quaternion.identity);

            // set script specific fields sobs
            mob.GetComponent<PassiveMobBehaviour>().pathfinding = mob.GetComponent<Pathfinding>();
            mob.GetComponent<Pathfinding>().SetGrid(GameObject.Find("Grid").GetComponent<Grid>());

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

    private void Start()
    {
        zbRespawn = 2.0f;
        pmRespawn = 2.0f;
    }

    private void Update()
    {
        // should be managing entities here
        zbRespawn -= Time.deltaTime; // need to be sim time. how to do
        pmRespawn -= Time.deltaTime;


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
    }
}
