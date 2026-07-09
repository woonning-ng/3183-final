using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    // this needs to be tied to the sim manager/astar right

    // container of passive mobs (spawns throughout day)
    public List<GameObject> passiveMobs;

    // container of zombies (only active if IsNight == true)
    public List<GameObject> zombies;

    // spawn passive mob fn

    // spawn zombie fn
    public void SpawnZombie() { 
        // spawn prefab, add to list to keep track of
    }

    // kill all zombies fn
    public void KillAllZombies() {
        foreach (GameObject go in zombies) {
            Destroy(go);
            zombies.Clear();
        }
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        // should be managing entities here
    }
}
