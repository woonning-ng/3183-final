// from: https://www.youtube.com/watch?v=9ey4isouocQ

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabsManager : MonoBehaviour
{
    public GameObject[] Tabs;
    public TextMeshProUGUI[] TabText;
    public Color InactiveTabTextColour, ActiveTabTextColour; // using color instead
    //public Vector2 InactiveTabButtonSize, ActiveTabButtonSize;

    public void Start()
    {
        // default
        TabText[0].color = ActiveTabTextColour;
    }

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