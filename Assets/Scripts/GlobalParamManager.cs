using UnityEngine;
using UnityEngine.UI;

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

public static class GlobalParam
{
    public static bool m_random;


}


public class GlobalParamManager : MonoBehaviour
{
    public Toggle m_random_toggle;

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
    }
}



