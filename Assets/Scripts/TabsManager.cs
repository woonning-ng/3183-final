/*
* @file      TabsManager.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file contains the implementation of the TabsManager, used for the
*   Behaviour Tree Editor in the demo's UI.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
 */

// Resource: https://www.youtube.com/watch?v=9ey4isouocQ

using UnityEngine;
using TMPro;

public class TabsManager : MonoBehaviour
{
    // Array of Tabs and their Text
    public GameObject[] Tabs;
    public TextMeshProUGUI[] TabText;

    // Active and Inactive Tab Colours
    public Color InactiveTabTextColour, ActiveTabTextColour; // using color instead
    //public Vector2 InactiveTabButtonSize, ActiveTabButtonSize;

    public void Start()
    {
        // Default is Tab number 0
        TabText[0].color = ActiveTabTextColour;

        foreach (GameObject go in Tabs)
        {
            go.SetActive(false);
        }
        Tabs[0].SetActive(true);
    }

    // Called by the Buttons' OnClick events
    public void SwitchToTab(int TabID) {
        foreach (GameObject go in Tabs) {
            go.SetActive(false);
        }
        Tabs[TabID].SetActive(true);

        foreach (TextMeshProUGUI tmp in TabText) {
            tmp.color = InactiveTabTextColour;
            //im.color = InactiveTabBG;
            //im.rectTransform.sizeDelta = InactiveTabButtonSize;
        }
        TabText[TabID].color = ActiveTabTextColour;
        //TabButtons[TabID].rectTransform.sizeDelta = InactiveTabButtonSize;
    }
}