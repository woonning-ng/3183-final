using System;
using UnityEngine;
using UnityEngine.UI;       // for Slider
using TMPro;                // for Slider's min and max (and current)

// wei shen me
// https://discussions.unity.com/t/get-an-objects-name/424256
// https://docs.unity3d.com/ScriptReference/UI.Slider-onValueChanged.html
// https://nagachiang.github.io/tutorial-pass-parameter-with-sliders-onvaluechanged-in-unity/

public class SliderBinder : MonoBehaviour
{
    public GlobalParamManager paramManager;

    // HumanBTAttributes sliders
    public Slider MaxHungerSlider;
    public Slider MaxEnergySlider;
    public Slider MaxSafetySlider;
    public Slider SafetyPriorityThresholdSlider;
    public Slider HungerHuntThresholdSlider;
    public Slider HungerDecayRateSlider;
    public Slider NightSafetyDecaySlider;
    public Slider EnergyDecayRateSlider;
    public Slider ShelterEnergyRegenSlider;
    public Slider ShelterSafetyRegenSlider;
    public Slider SleepEnergyRegenSlider;
    public Slider SafetyRegenSlider;
    public Slider NightZombieSafetyMultiplierSlider;
    public Slider HungerRestoreAmountSlider;
    public Slider ZombieDetectionRadiusSlider;
    public Slider RepathIntervalSlider;
    public Slider EatDistanceSlider;
    public Slider MoveSpeedSlider;

    // ZombieBTAttributes sliders
    public Slider ZombieMaxIdleTimeSlider;
    public Slider ZombieDefaultChaseTimeSlider;
    public Slider ZombieDefaultChaseCooldownSlider;
    public Slider ZombieRepathIntervalSlider;
    public Slider ZombieHumanDetectionRadiusSlider;
    public Slider ZombieMoveSpeedSlider;

    // PassiveMobBTAttributes sliders
    public Slider PassiveMobMaxIdleTimeSlider;
    public Slider PassiveMobMoveSpeedSlider;

    // Min and Max ranges given by slider to be displayed in the texts

    void Bind(Slider slider, Func<float> getter, Action<float> setter, string floatingPt = "f0")
    {
        // cache the label references ONCE, at bind time
        string sliderName = GetNameOfSliderMinusSlider(slider);
        TextMeshProUGUI minText = GameObject.Find(sliderName + "MIN").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI maxText = GameObject.Find(sliderName + "MAX").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI currText = GameObject.Find(sliderName + "CURR").GetComponent<TextMeshProUGUI>();

        minText.text = slider.minValue.ToString(floatingPt);
        maxText.text = slider.maxValue.ToString(floatingPt);
        currText.text = getter().ToString(floatingPt);

        slider.value = getter();
        slider.onValueChanged.AddListener(v =>
        {
            setter(v);
            currText.text = v.ToString(floatingPt); // only updates on actual change
        });
    }

    private string GetNameOfSliderMinusSlider(Slider sliderObj) {
        string sliderName = sliderObj.name;
        return sliderName.Replace("Slider", "");
    }

    void Start()
    {
        // UI event listener and closures capturing ref...
        // i'm not doing this bye i can just bind the min max curr to the same names

        Bind(MaxHungerSlider, () => paramManager.m_human_bt_atts.MaxHunger,
                           v => paramManager.m_human_bt_atts.MaxHunger = v);

        Bind(MaxEnergySlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);

        Bind(MaxSafetySlider, () => paramManager.m_human_bt_atts.MaxSafety,
                                v => paramManager.m_human_bt_atts.MaxSafety = v);

        Bind(SafetyPriorityThresholdSlider, () => paramManager.m_human_bt_atts.SafetyPriorityThreshold,
                                v => paramManager.m_human_bt_atts.SafetyPriorityThreshold = v);

        Bind(HungerHuntThresholdSlider, () => paramManager.m_human_bt_atts.HungerHuntThreshold,
                                v => paramManager.m_human_bt_atts.HungerHuntThreshold = v);

        Bind(HungerDecayRateSlider, () => paramManager.m_human_bt_atts.HungerDecayRate,
                                v => paramManager.m_human_bt_atts.HungerDecayRate = v, "f1");

        Bind(NightSafetyDecaySlider, () => paramManager.m_human_bt_atts.NightSafetyDecay,
                                v => paramManager.m_human_bt_atts.NightSafetyDecay = v, "f1");

        Bind(EnergyDecayRateSlider, () => paramManager.m_human_bt_atts.EnergyDecayRate,
                                v => paramManager.m_human_bt_atts.EnergyDecayRate = v, "f1");

        Bind(ShelterEnergyRegenSlider, () => paramManager.m_human_bt_atts.ShelterEnergyRegen,
                                v => paramManager.m_human_bt_atts.ShelterEnergyRegen = v, "f1");

        Bind(ShelterSafetyRegenSlider, () => paramManager.m_human_bt_atts.ShelterSafetyRegen,
                                v => paramManager.m_human_bt_atts.ShelterSafetyRegen = v, "f1");

        Bind(SleepEnergyRegenSlider, () => paramManager.m_human_bt_atts.SleepEnergyRegen,
                                v => paramManager.m_human_bt_atts.SleepEnergyRegen = v, "f1");

        Bind(SafetyRegenSlider, () => paramManager.m_human_bt_atts.SafetyRegen,
                                v => paramManager.m_human_bt_atts.SafetyRegen = v, "f1");

        Bind(NightZombieSafetyMultiplierSlider, () => paramManager.m_human_bt_atts.NightZombieSafetyMultiplier,
                                v => paramManager.m_human_bt_atts.NightZombieSafetyMultiplier = v);

        Bind(HungerRestoreAmountSlider, () => paramManager.m_human_bt_atts.HungerRestoreAmount,
                                v => paramManager.m_human_bt_atts.HungerRestoreAmount = v);

        Bind(ZombieDetectionRadiusSlider, () => paramManager.m_human_bt_atts.ZombieDetectionRadius,
                                v => paramManager.m_human_bt_atts.ZombieDetectionRadius = v);

        Bind(RepathIntervalSlider, () => paramManager.m_human_bt_atts.RepathInterval,
                                v => paramManager.m_human_bt_atts.RepathInterval = v, "f1");

        Bind(EatDistanceSlider, () => paramManager.m_human_bt_atts.EatDistance,
                                v => paramManager.m_human_bt_atts.EatDistance = v, "f1");

        Bind(MoveSpeedSlider, () => paramManager.m_human_bt_atts.MoveSpeed,
                                v => paramManager.m_human_bt_atts.MoveSpeed = v);


        Bind(ZombieMaxIdleTimeSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v, "f1");

        Bind(ZombieDefaultChaseTimeSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v, "f1");

        Bind(ZombieDefaultChaseCooldownSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v, "f1");

        Bind(ZombieRepathIntervalSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v, "f1");

        Bind(ZombieHumanDetectionRadiusSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);

        Bind(ZombieMoveSpeedSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);


        Bind(PassiveMobMaxIdleTimeSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v, "f1");

        Bind(PassiveMobMoveSpeedSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);
    }
}