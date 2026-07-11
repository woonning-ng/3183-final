using System;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    // -------- handling sim time stuff here --------
    public static SimulationManager Instance;
    public GameObject dayNightCycle;

    [SerializeField] private float baseCycleLength = 24f;
    private float currentCycleLength;

    public float SpeedMultiplier => baseCycleLength / currentCycleLength;

    public event Action<float> OnSpeedChanged;
    // -------- handling sim time stuff here --------

    public int SimulatedRunNo = 1;

    // stats related stuff here for information?
    private bool paused = false;

    private int currentDay = 0;
    private float timeElapsedRun = 0.0f;
    private float totalTimeElapsed = 0.0f;

    // need to get the other managers and relevant stuff for pausing

    public DayNightCycle GetDNC() { return dayNightCycle.GetComponent<DayNightCycle>(); }

    public void FinishRun() {
        SimulatedRunNo++;
    }

    public void StartNewSimulation() { 
        // reset various stuff here
    }


    private void Awake()
    {
        Instance = this;
        currentCycleLength = baseCycleLength;
    }

    public void SetCycleLength(float newLength)
    {
        currentCycleLength = newLength;
        OnSpeedChanged?.Invoke(SpeedMultiplier);
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
