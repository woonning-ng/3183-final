/*
* @file      SpeedDisplay.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file handles the displaying of global variables on the scene
*   canvas.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
 */

using UnityEngine;
using TMPro;

public class GlobalParamsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject globalManager;

    // items to update
    [SerializeField] private TextMeshProUGUI simSpeedText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        DayNightCycle dnc = globalManager.GetComponent<DayNightCycle>();
        if (dnc != null)
        {
            simSpeedText.text = dnc.CurrentSpeed().ToString("f2") + "x";
        }
    }
}
