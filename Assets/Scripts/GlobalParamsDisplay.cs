/*
* @file      SpeedDisplay.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file handles the displaying of global variables on the scene
*   canvas.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
 */

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalParamsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject globalManager;
    [SerializeField] private CameraSystem cameraSys;

    // items to update
    [SerializeField] private TextMeshProUGUI simSpeedText;
    [SerializeField] private TextMeshProUGUI cameraText;

    public void ReloadScene() {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // screen resolution setting
        Screen.SetResolution(1920, 1080, false);
    }

    // Update is called once per frame
    void Update()
    {
        DayNightCycle dnc = globalManager.GetComponent<DayNightCycle>();
        if (dnc != null)
        {
            simSpeedText.text = dnc.CurrentSpeed().ToString("f2") + "x";
        }

        if (cameraSys != null) {
            cameraText.text = cameraSys.GetMode();
        }
    }
}
