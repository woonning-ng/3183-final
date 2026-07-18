using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using static TerrainGen;
using UnityEngine.Assertions.Must;

//There are a few ways that we can do this

//1. static class (current)
//2. singleton + DontDestroyOnLoad
//3. scriptable object (not sure how to do)
//4. PlayerPref (unity class, but very limited choices)
// Reference: https://umistudioblog.com/en/%E3%80%90unity-beginners-guide%E3%80%913-ways-to-share-data-across-scenes-without-losing-it%E3%80%90complete-guide-for-beginners%E3%80%91/

//This is a bit of a manual work, maybe there is a better way to do this
//Basically have to manually update and check on both sides
//GlobalParam will not be resetted when scene is changed

//GlobalParamManager will just update the GameObjects at Awake
//On update for the manager, it will update the GlobalParams directly

//If we want, the GlobalParamManager can be converted to singleton and use DontDestroyOnLoad

[System.Serializable]
public class HumanBTAttributes {
    public float MaxHunger = 100.0f; 
    public float MaxEnergy = 100.0f; 
    public float MaxSafety = 100.0f;
    public float SafetyPriorityThreshold = 20.0f;
    public float HungerHuntThreshold = 60.0f;

    public float HungerDecayRate = 0.5f;
    public float NightSafetyDecay = 1.0f;
    public float EnergyDecayRate = 1.0f;

    public float ShelterEnergyRegen = 3.0f;
    public float ShelterSafetyRegen = 3.0f;

    public float SleepEnergyRegen = 1.5f;
    public float SafetyRegen = 1.0f;

    public float NightZombieSafetyMultiplier = 2.0f;
    public float HungerRestoreAmount = 20.0f;
    public float ZombieDetectionRadius = 20.0f;

    public float RepathInterval = 0.5f;
    public float EatDistance = 5.0f;

    public float MoveSpeed = 20.0f;

}

[System.Serializable]
public class ZombieBTAttributes {
    public float MaxIdleTime = 5.0f;
    public float DefaultChaseTime = 5.0f;
    public float DefaultChaseCooldown = 4.0f;
    public float RepathInterval = 5.0f;
    public float HumanDetectionRadius = 55.0f;
    public float MoveSpeed = 15.0f;
}

[System.Serializable]
public class PassiveMobBTAttributes {
    public float MaxIdleTime = 5.0f;
    public float MoveSpeed = 10.0f;
}


public static class GlobalParam
{
    public static bool m_random = true;
    public static bool m_frequency = true;
    public static bool m_octave = true;
    public static bool m_ridge = true;
    
    public static float m_grassWeight = 1.0f;
    public static float m_sandWeight = 2.0f;
    public static float m_snowWeight = 3.0f;


    public static int max_width = 128;
    public static int max_height = 128;

    public static Terrain_cus[,] terrain_map = new Terrain_cus[max_width, max_height];
    public static Terrain_obj[,] terrain_objs = new Terrain_obj[max_width, max_height];

    public static int simulatedRuns = 0;

    public static int cameraMode = 0; // top down is default
    public static bool cameraBypassDefault = false;
    public static bool showDeadPopup = false;
    public static Vector3 cameraPos = new Vector3(64.0f, 120.0f, 64.0f);
    public static Vector3 cameraDir = new Vector3(90.0f, 0.0f, 0.0f);

    // BT related items here
    public static HumanBTAttributes human_bt_globals = new HumanBTAttributes();
    public static ZombieBTAttributes zombie_bt_globals = new ZombieBTAttributes();
    public static PassiveMobBTAttributes passivemob_bt_globals = new PassiveMobBTAttributes();
}


public class GlobalParamManager : MonoBehaviour
{
    public Toggle m_random_toggle;
    public Toggle m_freq_toggle;
    public Toggle m_octave_toggle;
    public Toggle m_ridge_toggle;
    public Toggle m_cam_bypass_toggle;
    public Toggle m_dead_popup_toggle;

    public Camera m_camera;

    [SerializeField] private TextMeshProUGUI SimRunsText;

    //Values can be edited in editor
    [SerializeField] private float grassWeight = 1.0f;
    [SerializeField] private float sandWeight = 2.0f;
    [SerializeField] private float snowWeight = 3.0f;

    [SerializeField] public HumanBTAttributes m_human_bt_atts = new HumanBTAttributes();
    [SerializeField] public ZombieBTAttributes m_zombie_bt_atts = new ZombieBTAttributes();
    [SerializeField] public PassiveMobBTAttributes m_passivemob_bt_atts = new PassiveMobBTAttributes();

    public int GetSimRuns() { return GlobalParam.simulatedRuns; }

    void Awake()
    {
        //update toggle depending on param on start
        if (GlobalParam.m_random)
        {
            m_random_toggle.isOn = true;
        }
        else
        {
            m_random_toggle.isOn = false;
        }

        if (GlobalParam.m_frequency)
        {
            m_freq_toggle.isOn = true;
        }
        else
        {
            m_freq_toggle.isOn = false;
        }

        if (GlobalParam.m_octave)
        {
            m_octave_toggle.isOn = true;
        }
        else
        {
            m_octave_toggle.isOn = false;
        }

        if (GlobalParam.m_ridge)
        {
            m_ridge_toggle.isOn = true;
        }
        else
        {
            m_ridge_toggle.isOn = false;
        }

        if (m_cam_bypass_toggle.isOn)
        {
            GlobalParam.cameraBypassDefault = true;
        }
        else
        {
            GlobalParam.cameraBypassDefault = false;
        }

        if (m_dead_popup_toggle.isOn)
        {
            GlobalParam.showDeadPopup = true;
        }
        else
        {
            GlobalParam.showDeadPopup = false;
        }

        grassWeight = GlobalParam.m_grassWeight;
        sandWeight = GlobalParam.m_sandWeight;
        snowWeight = GlobalParam.m_snowWeight;

        m_human_bt_atts = GlobalParam.human_bt_globals;
        m_zombie_bt_atts = GlobalParam.zombie_bt_globals;
        m_passivemob_bt_atts = GlobalParam.passivemob_bt_globals;

        GlobalParam.simulatedRuns++; // just testing

        m_camera.GetComponent<CameraSystem>().SetMode(GlobalParam.cameraMode);
        m_camera.GetComponent<CameraSystem>().bypassModeDefaults = GlobalParam.cameraBypassDefault;
        m_camera.GetComponent<Transform>().transform.position = GlobalParam.cameraPos;
        if (GlobalParam.cameraMode != 2) m_camera.GetComponent<Transform>().transform.eulerAngles = GlobalParam.cameraDir;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Empty for now
    }

    // Update is called once per frame
    void Update()
    {
        //Check if random toggle is toggled and update the params
        if (m_random_toggle.isOn)
        {
            GlobalParam.m_random = true;
        }
        else
        {
            GlobalParam.m_random = false;
        }

        if (m_freq_toggle.isOn)
        {
            GlobalParam.m_frequency = true;
        }
        else
        {
            GlobalParam.m_frequency = false;
        }

        if (m_octave_toggle.isOn)
        {
            GlobalParam.m_octave = true;
        }
        else
        {
            GlobalParam.m_octave = false;
        }

        if (m_ridge_toggle.isOn)
        {
            GlobalParam.m_ridge = true;
        }
        else
        {
            GlobalParam.m_ridge = false;
        }

        if (m_cam_bypass_toggle.isOn)
        {
            GlobalParam.cameraBypassDefault = true;
        }
        else
        {
            GlobalParam.cameraBypassDefault = false;
        }

        if (m_dead_popup_toggle.isOn)
        {
            GlobalParam.showDeadPopup = true;
        }
        else
        {
            GlobalParam.showDeadPopup = false;
        }
        
       
        GlobalParam.m_grassWeight = grassWeight;
        GlobalParam.m_sandWeight = sandWeight;
        GlobalParam.m_snowWeight = snowWeight;

        // constantly update this one
        GlobalParam.human_bt_globals = m_human_bt_atts;
        GlobalParam.zombie_bt_globals = m_zombie_bt_atts;
        GlobalParam.passivemob_bt_globals = m_passivemob_bt_atts;

        SimRunsText.text = GetSimRuns().ToString();

        GlobalParam.cameraMode = m_camera.GetComponent<CameraSystem>().GetModeInt();
        GlobalParam.cameraPos = m_camera.GetComponent<Transform>().transform.position;
        GlobalParam.cameraDir = m_camera.GetComponent<Transform>().transform.eulerAngles;

        m_camera.GetComponent<CameraSystem>().bypassModeDefaults = GlobalParam.cameraBypassDefault;
    }
}



