/*
* @file      SimulationManager.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file contains the implementation of our Simulation Manager.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
 */

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

    // -------- How many Runs has it been --------
    public int SimulatedRunNo = 1;

    // -- stats related stuff here for information --
    private bool paused = false;
    private float timeElapsedRun = 0.0f;

    // --- unused --
    // private int currentDay = 0;

    public void FinishRun() {
        SimulatedRunNo++;
    }

    public void StartNewSimulation() { 
        // reset various stuff here
    }
    // --- unused --

    // These are just the functions that were used...
    public void SetPaused(bool p) { paused = p; }
    public float GetTimeElapsedRun() { return timeElapsedRun; }
    public float GetTimeElapsedRunInDays() { return timeElapsedRun / 24.0f; }

    // access the DNC object
    public DayNightCycle GetDNC() { return dayNightCycle.GetComponent<DayNightCycle>(); }

    // set the cycle length of the sim
    public void SetCycleLength(float newLength)
    {
        currentCycleLength = newLength;
        OnSpeedChanged?.Invoke(SpeedMultiplier);
    }

    private void Awake()
    {
        Instance = this;
        currentCycleLength = baseCycleLength;
        timeElapsedRun = 0.0f;
        paused = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (paused) return;
        timeElapsedRun += GetDNC().SimDeltaTime; // increase timeElapsed for this run
    }
}
