using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public int SimulatedRunNo = 1;

    // stats related stuff here for information?
    private bool paused = false;

    private int currentDay = 0;
    private float timeElapsedRun = 0.0f;
    private float totalTimeElapsed = 0.0f;

    // need to get the other managers and relevant stuff for pausing

    public void FinishRun() {
        SimulatedRunNo++;
    }

    public void StartNewSimulation() { 
        // reset various stuff here
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
