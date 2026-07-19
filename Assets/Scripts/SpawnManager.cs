/*
* @file      SpawnManager.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file contains the implementation of the Spawn Manager.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
 */

using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // Zombie and Mob cap, and respawn times
    [SerializeField] public int maxMobs = 10;
    [SerializeField] public int maxZombies = 10;

    [SerializeField] public float passiveMobRespawnTimer = 5.0f;
    [SerializeField] public float zombieRespawnTimer = 10.0f;

    // Cooldown
    public float pmRespawn = 0.0f;
    public float zbRespawn = 0.0f;

    // container of passive mobs (spawns throughout day)
    public List<GameObject> passiveMobs;

    // container of zombies (only active if IsNight == true)
    public List<GameObject> zombies;

    // Reference to prefbas for instantiation
    public GameObject passiveMobPrefab;
    public GameObject zombiePrefab;

    // Reference to human object (needed by zombie)
    public GameObject human;

    // Singleton pattern
    public static SpawnManager Instance;

    // Spawn passive mob function
    public void SpawnPassiveMob() {
        if (passiveMobs.Count < maxMobs) {
            // roll a spawn point
            Vector3 spawnPt = Grid.Instance.TryGetValidSpawn();
            GameObject mob = Instantiate(passiveMobPrefab, spawnPt, Quaternion.identity);

            // set script specific fields sobs
            mob.GetComponent<PassiveMobBehaviour>().pathfinding = mob.GetComponent<Pathfinding>();
            mob.GetComponent<Pathfinding>().SetGrid(Grid.Instance);

            passiveMobs.Add(mob);
        }
    }

    // Spawn zombie function
    public void SpawnZombie() {
        if (zombies.Count < maxZombies)
        {
            // roll a spawn point
            Vector3 spawnPt = Grid.Instance.TryGetValidSpawn();
            GameObject mob = Instantiate(zombiePrefab, spawnPt, Quaternion.identity);

            // set script specific fields
            mob.GetComponent<ZombieBehaviour>().pathfinding = mob.GetComponent<Pathfinding>();
            mob.GetComponent<ZombieBehaviour>().humanTarget = human; // set human as target so that it can detect if human is nearbby
            mob.GetComponent<Pathfinding>().SetGrid(Grid.Instance);

            zombies.Add(mob);
        }
    }

    // Kill all zombies function
    public void KillAllZombies() {
        foreach (GameObject go in zombies) {
            Destroy(go);
        }

        zombies.Clear();
    }

    private void Awake()
    {
        if (Instance == null) // set instance to this if not null
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Give cooldown for zombie and PM respawn
        zbRespawn = 2.0f;
        pmRespawn = 2.0f;
    }

    private void Update()
    {
        // Decrement respawn cooldowns by simDeltaTime
        zbRespawn -= SimulationManager.Instance.GetDNC().SimDeltaTime;
        pmRespawn -= SimulationManager.Instance.GetDNC().SimDeltaTime;

        // No prior condition to respawn passive mob; capacity check handled in fn
        if (pmRespawn <= 0.0f)
        {
            SpawnPassiveMob();
            pmRespawn = passiveMobRespawnTimer;
        }

        // Zombie checks
        // Check 1: check if it's night
        // If it is nighttime, allwo zombies to spawn at an interval
        // Otherwise if it is daytime
        // Check to see if the size of the zombie vector is > 0. if it is, kill all zombies
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

        // Check if a passive mob has been marked for death by player
        // Despawn the passive mob if needed
        // Because of possible ordering issues, query the list and add to the
        // deadMobs list first.
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
