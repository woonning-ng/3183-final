// from: https://www.youtube.com/watch?v=9ey4isouocQ

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabsManager : MonoBehaviour
{
    public GameObject[] Tabs;
    public Image[] TabButtons;
    public Color InactiveTabBG, ActiveTabBG; // using color instead
    public Vector2 InactiveTabButtonSize, ActiveTabButtonSize;

    public void SwitchToTab(int TabID) {
        foreach (GameObject go in Tabs) {
            go.SetActive(false);
        }
        Tabs[TabID].SetActive(true);

        foreach (Image im in TabButtons) {
            im.color = InactiveTabBG;
            im.rectTransform.sizeDelta = InactiveTabButtonSize;
        }
        TabButtons[TabID].color = ActiveTabBG;
        TabButtons[TabID].rectTransform.sizeDelta = InactiveTabButtonSize;
    }
}