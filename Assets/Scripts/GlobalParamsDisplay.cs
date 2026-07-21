/*
* @file      GlobalParamsDisplay.cs
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
using UnityEngine.UI;

public class GlobalParamsDisplay : MonoBehaviour
{
    // GOs the display needs to reference
    [SerializeField] private GameObject globalManager; 
    [SerializeField] private CameraSystem cameraSys;   
    [SerializeField] private HumanBehaviour human;

    // UI Elements to update
    [SerializeField] private TextMeshProUGUI simSpeedText;
    [SerializeField] private TextMeshProUGUI cameraText;
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI isNightText;

    [SerializeField] private TextMeshProUGUI humanHunger;
    [SerializeField] private TextMeshProUGUI humanEnergy;
    [SerializeField] private TextMeshProUGUI humanSafety;
    [SerializeField] private TextMeshProUGUI humanState;
    [SerializeField] private TextMeshProUGUI daysElapsedForRun;
    [SerializeField] private TextMeshProUGUI daysElapsedForRunScreen;

    [SerializeField] private GameObject humanDiedScreen;

    // Reload Scene function
    public void ReloadScene() { 
        // can probably look into how to save data across scenes (singleton/instance? - a gameobj)
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Force 1920 x 1080
        Screen.SetResolution(1920, 1080, false);
    }

    // Update is called once per frame
    void Update()
    {
        // Update Day Night Cycle related UI elements
        DayNightCycle dnc = globalManager.GetComponent<DayNightCycle>();
        if (dnc != null)
        {
            simSpeedText.text = dnc.CurrentSpeed().ToString("f2") + "x";

            isNightText.text = dnc.IsNight().ToString();

            Vector3 currentTime = dnc.GetTime();

            currentTimeText.text = currentTime.x.ToString("00") + ":" + currentTime.y.ToString("00") + ":" + currentTime.z.ToString("00");
        }

        // Update Camera System related UI elements
        if (cameraSys != null) {
            cameraText.text = cameraSys.GetMode();
        }

        // Update Human related UI Elements
        if (human != null) {
            humanHunger.text = human.hunger.ToString("f2") + "/" + human.maxHunger.ToString("f2");
            humanEnergy.text = human.energy.ToString("f2") + "/" + human.maxEnergy.ToString("f2");
            humanSafety.text = human.safety.ToString("f2") + "/" + human.maxSafety.ToString("f2");
            humanState.text = human.GetHumanState();

            // Human is dead screen
            humanDiedScreen.SetActive(human.IsHumanDead());
            if (human.IsHumanDead())
            {
                // Pause the sim and display the number of days the human survived
                SimulationManager.Instance.SetPaused(true);
                daysElapsedForRun.text = SimulationManager.Instance.GetTimeElapsedRunInDays().ToString("f2");
            }
        }

        daysElapsedForRunScreen.text = SimulationManager.Instance.GetTimeElapsedRunInDays().ToString("f2");

        // nvm this caused auto reloads when it was not needed
        //if (humanDiedScreen.GetComponentInChildren<Toggle>().isOn) { ReloadScene(); } // auto reload if checked
    }
}
