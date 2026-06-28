using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// resources:
// https://medium.com/codex/creating-a-basic-day-and-night-cycle-in-unity-c-dff942c1690d
// --> since we are using a 3d light unlike the tutorial's 2d light, there are changes that
//     have to be made.
// --> our simulation time can also vary so this should be tied into it too

public class DayNightCycle : MonoBehaviour
{
    // at this time of day, what should the light attributes be?
    [System.Serializable]
    public struct DayNightMark {
        public float timeRatio; // normalised in [0, 1] - [00:00, 23:59]
        public Color colour;
        public float intensity;
    }

    // list of marks for certain times
    [SerializeField]
    private DayNightMark[] marks;

    // in seconds; this should be adjustable via the simulation later
    [SerializeField]
    private float cycleLength = 24;

    private float currentCycleTime;
    private int currentMarkIndex, nextMarkIndex;
    private float currentMarkTime, nextMarkTime;

    private const float TIME_CHECK_EPSILON = 0.1f;

    // settable light obj (in our case, the global light)
    [SerializeField] private Light lightObj;

    // setters and getters for cycle
    public void SetCycle(float cycle) { cycleLength = cycle; }
    public float GetCycle() { return cycleLength; }

    private void CycleMarks() { 
        currentMarkIndex = (currentMarkIndex + 1) % marks.Length;
        nextMarkIndex = (nextMarkIndex + 1) % marks.Length;

        currentMarkTime = marks[currentMarkIndex].timeRatio * cycleLength;
        nextMarkTime = marks[nextMarkIndex].timeRatio * cycleLength;
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
        if (Mathf.Abs(currentCycleTime - nextMarkTime) < TIME_CHECK_EPSILON)
        {
        }
    }
}
