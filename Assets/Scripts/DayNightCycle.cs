/*
* @file      DayNightCycle.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file contains the Day Night cycle of the simulation.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
 */

using System;
using UnityEngine;
using UnityEngine.Rendering;

// resources:
// https://medium.com/codex/creating-a-basic-day-and-night-cycle-in-unity-c-dff942c1690d
// --> since we are using a 3d light unlike the tutorial's 2d light, there are changes that
//     have to be made.
// --> our simulation time can also vary so this should be tied into it too

public class DayNightCycle : MonoBehaviour
{
    // At this time of day, what should the light attributes be?
    [System.Serializable]
    public struct DayNightMark {
        public string timeOfDay; // for notes
        public float timeRatio;  // normalised in [0, 1] - [00:00, 23:59]
        public float xRotation;  // needed for 3d (0 for 6am)
    }

    // List of marks for certain times
    [SerializeField]
    private DayNightMark[] marks;

    // In seconds; this should be adjustable via the simulation later
    [SerializeField]
    private float cycleLength = 24;
    private float defaultCycleLength = 24;

    // Current Cycle + Mark time variables
    private float currentCycleTime;
    //private float currentCycleTimeRatio; // keep track of this instead?
    private int currentMarkIndex, nextMarkIndex;
    private float currentMarkTime, nextMarkTime;

    bool paused = false; // unused

    // Settable light obj (in our case, the global light)
    [SerializeField] private Light lightObj;

    // Simulation's delta time
    public float SimDeltaTime => Time.deltaTime * CurrentSpeed();

    // Is it Night Time? (Needed for BTs for human behaviour)
    public bool IsNight() {
        // nighttime defined as: 6pm to 6am
        // normalised in [0, 1] - [00:00, 23:59]
        float currentRatio = currentCycleTime / cycleLength;

        return (currentRatio <= (6.0f / 24.0f) || currentRatio >= (18.0f / 24.0f));
    }

    // Convert the current ratio to HH:MM:SS format
    public Vector3 GetTime() {
        float currentRatio = (currentCycleTime / cycleLength) * 24.0f;

        // hours
        float hour = Mathf.Floor(currentRatio);
        currentRatio -= hour;

        // mins
        currentRatio *= 60.0f;
        float minute = Mathf.Floor(currentRatio);
        currentRatio -= minute;

        // seconds
        float second = currentRatio * 60.0f;

        return new Vector3(hour, minute, second);
    }

    // Setters and Getters for cycle
    public void SetCycleTime(float newTime) {
        float currentRatio = currentCycleTime / cycleLength;
        cycleLength = newTime;
        currentCycleTime = currentRatio * cycleLength;

        // indices are still valid (ratio preserved = same segment)
        // just rescale the times to match new cycleLength
        currentMarkTime = marks[currentMarkIndex].timeRatio * cycleLength;
        nextMarkTime = marks[nextMarkIndex].timeRatio * cycleLength;
    }

    public float GetCycleTime() { return cycleLength; }

    // Unused function
    public void SetTimeFromSim(float simTimeRatio)
    {
        currentCycleTime = simTimeRatio * cycleLength;
        if (currentCycleTime >= GetAdjustedNextTime())
        {
            CycleMarks();
        }
        RotateLight();
    }

    // Getter for current speed
    public float CurrentSpeed() {
        return (defaultCycleLength / cycleLength);
    }

    // Because the next mark can be behind us in time, it is not safe to
    // assume that the time associated with it is always greater than the
    // current time (due to this being a cycle)
    private float GetAdjustedNextTime()
    {
        // if next mark is behind us in time, it's in the next cycle
        return nextMarkTime < currentMarkTime
            ? nextMarkTime + cycleLength
            : nextMarkTime;
    }

    // Cycle through the marks, for reasons same as above
    private void CycleMarks() { 
        currentMarkIndex = (currentMarkIndex + 1) % marks.Length;
        nextMarkIndex = (nextMarkIndex + 1) % marks.Length;

        currentMarkTime = marks[currentMarkIndex].timeRatio * cycleLength;
        nextMarkTime = marks[nextMarkIndex].timeRatio * cycleLength;
    }

    // Rotate Light
    private void RotateLight() {
        // Get the current and next rotation of the light
        float currentRot = marks[currentMarkIndex].xRotation;
        float nextRot = marks[nextMarkIndex].xRotation;

        // Then the adjusted next time to get a value of t, used
        // in lerping below
        float adjustedNextTime = GetAdjustedNextTime();

        float t = (currentCycleTime - currentMarkTime)
            / (adjustedNextTime - currentMarkTime);

        // If the delta is less than -180 degrees or greater than 180 degrees
        // add or minus 360 degrees to keep it in the [0, 360] range
        float delta = nextRot - currentRot;
        if (delta < -180f) nextRot += 360f;
        if (delta > 180f) nextRot -= 360f;

        // lerp rotation and appky it to the light obj
        float lerpedRotation = Mathf.Lerp(currentRot, nextRot, t) % 360f;

        lightObj.transform.eulerAngles = new Vector3(lerpedRotation, 0.0f, 0.0f);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentMarkIndex = -1;
        CycleMarks();
    }

    // Update is called once per frame
    void Update()
    {
        if (!paused)
        {
            // increment cycle time, cycle marks and rotate the light
            currentCycleTime += Time.deltaTime;

            // check BEFORE modulo resets it
            if (currentCycleTime >= GetAdjustedNextTime())
            {
                CycleMarks();
            }

            currentCycleTime %= cycleLength; // now safe to wrap

            RotateLight();
        }
    }
}
