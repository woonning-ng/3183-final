/*
* @file      DayNightCycle.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file contains the Day Night cycle of the simulation.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

// resources:
// https://medium.com/codex/creating-a-basic-day-and-night-cycle-in-unity-c-dff942c1690d
// --> since we are using a 3d light unlike the tutorial's 2d light, there are changes that
//     have to be made.
// --> our simulation time can also vary so this should be tied into it too

// add in:
// onChange for cycle time change, or a normalised ver to speed up sim
// lerping for day night cycle

public class DayNightCycle : MonoBehaviour
{
    // at this time of day, what should the light attributes be?
    [System.Serializable]
    public struct DayNightMark {
        public string timeOfDay; // for notes
        public float timeRatio; // normalised in [0, 1] - [00:00, 23:59]
        public ColorParameter colour;
        public float intensity;
        public float xRotation; // needed for 3d (0 for 6am)
    }

    // list of marks for certain times
    [SerializeField]
    private DayNightMark[] marks;

    // in seconds; this should be adjustable via the simulation later
    [SerializeField]
    private float cycleLength = 24;

    private float defaultCycleLength = 24;

    private float currentCycleTime;
    private float currentCycleTimeRatio; // keep track of this instead?
    private int currentMarkIndex, nextMarkIndex;
    private float currentMarkTime, nextMarkTime;

    private const float TIME_CHECK_EPSILON = 0.1f;

    // settable light obj (in our case, the global light)
    [SerializeField] private Light lightObj;

    // setters and getters for cycle
    public void SetCycleTime(float newTime) {
        cycleLength = newTime;

        RecalibrateTime();
    }

    public float GetCycleTime() { return cycleLength; }

    // getter for current speed
    public float CurrentSpeed() {
        return (defaultCycleLength / cycleLength);
    }

    private void RecalibrateTime() {
        // TODO: RECALIBRATE CURRENTCYCLETIME HERE FOR COHESION

        currentMarkTime = marks[currentMarkIndex].timeRatio * cycleLength;
        nextMarkTime = marks[nextMarkIndex].timeRatio * cycleLength;
    }

    private void CycleMarks() { 
        currentMarkIndex = (currentMarkIndex + 1) % marks.Length;
        nextMarkIndex = (nextMarkIndex + 1) % marks.Length;

        currentMarkTime = marks[currentMarkIndex].timeRatio * cycleLength;
        nextMarkTime = marks[nextMarkIndex].timeRatio * cycleLength;
    }

    private void RotateLight() {
        // TODO: FIGURE OUT WHY THE LERP DOES NOT LERP
        // insert angery pepe meme
        // the issue comes when next < curr soooooooo
        // WHY DOES THE EULER NOT EULER UNITY REEEEEEEEEEEEEEEEE
        // doing this project has reminded me of why i hate unity so much tbh
        float currentRot = marks[currentMarkIndex].xRotation;
        float nextRot = marks[nextMarkIndex].xRotation;

        float lerpedRotation = Mathf.Lerp(
            currentRot,
            nextRot,
            (nextMarkTime - currentCycleTime) / nextMarkTime
        );
        //UnityEngine.Debug.Log(currentMarkTime);
        //UnityEngine.Debug.Log(lerpedRotation);

        if (lerpedRotation >= 360.0f)
        {
            lerpedRotation = lerpedRotation - 360.0f;
        }

        lightObj.transform.eulerAngles = new Vector3(lerpedRotation, 0.0f, 0.0f);

        UnityEngine.Debug.Log("current euler angle: " + lightObj.transform.eulerAngles.x);
        if (lightObj.transform.eulerAngles.x >= 360.0f) { 
            UnityEngine.Debug.Log("Greater than 360.0f: " + lightObj.transform.eulerAngles.x);
        }
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
        currentCycleTime = (currentCycleTime + Time.deltaTime) % cycleLength;

        // passed a mark?
        // for now we just use absolute rotations; the actual dynamic cycling
        // comes later OTL
        if (Mathf.Abs(currentCycleTime - nextMarkTime) < TIME_CHECK_EPSILON)
        {
            DayNightMark next = marks[nextMarkIndex];
            //GetComponent<Light>().colour = next.colour;
            //GetComponent<Light>().intensity = next.intensity;

            CycleMarks();
        }

        // perform lerping for time
        RotateLight();
    }
}
