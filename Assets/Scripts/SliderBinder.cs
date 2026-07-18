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

    void Bind(Slider slider, Func<float> getter, Action<float> setter)
    {
        slider.value = getter();
        slider.onValueChanged.AddListener(v => setter(v));
    }

    private string GetNameOfSliderMinusSlider(Slider sliderObj) {
        string sliderName = sliderObj.name;
        return sliderName.Replace("Slider", "");
    }

    private void ReplaceAttributesOfSliderInScene(Slider sliderObj, string floatingPt = "f0") {
        string sliderName = GetNameOfSliderMinusSlider(sliderObj);

        // get min, max, current values (text)
        GameObject minObj = GameObject.Find(sliderName + "MIN");
        minObj.GetComponent<TextMeshProUGUI>().text = sliderObj.minValue.ToString(floatingPt);

        GameObject maxObj = GameObject.Find(sliderName + "MAX");
        maxObj.GetComponent<TextMeshProUGUI>().text = sliderObj.maxValue.ToString(floatingPt);

        GameObject currObj = GameObject.Find(sliderName + "CURR");
        currObj.GetComponent<TextMeshProUGUI>().text = sliderObj.value.ToString(floatingPt);
    }

    void Start()
    {
        // UI event listener and closures capturing ref...
        // i'm not doing this bye i can just bind the min max curr to the same names

        Bind(MaxHungerSlider, () => paramManager.m_human_bt_atts.MaxHunger,
                           v => paramManager.m_human_bt_atts.MaxHunger = v);
        ReplaceAttributesOfSliderInScene(MaxHungerSlider);

        Bind(MaxEnergySlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);
        ReplaceAttributesOfSliderInScene(MaxEnergySlider);

        Bind(MaxSafetySlider, () => paramManager.m_human_bt_atts.MaxSafety,
                                v => paramManager.m_human_bt_atts.MaxSafety = v);
        ReplaceAttributesOfSliderInScene(MaxSafetySlider);

        Bind(SafetyPriorityThresholdSlider, () => paramManager.m_human_bt_atts.SafetyPriorityThreshold,
                                v => paramManager.m_human_bt_atts.SafetyPriorityThreshold = v);
        ReplaceAttributesOfSliderInScene(SafetyPriorityThresholdSlider);

        Bind(HungerHuntThresholdSlider, () => paramManager.m_human_bt_atts.HungerHuntThreshold,
                                v => paramManager.m_human_bt_atts.HungerHuntThreshold = v);
        ReplaceAttributesOfSliderInScene(HungerHuntThresholdSlider);

        Bind(HungerDecayRateSlider, () => paramManager.m_human_bt_atts.HungerDecayRate,
                                v => paramManager.m_human_bt_atts.HungerDecayRate = v);
        ReplaceAttributesOfSliderInScene(HungerDecayRateSlider);

        Bind(NightSafetyDecaySlider, () => paramManager.m_human_bt_atts.NightSafetyDecay,
                                v => paramManager.m_human_bt_atts.NightSafetyDecay = v);
        ReplaceAttributesOfSliderInScene(NightSafetyDecaySlider);

        Bind(EnergyDecayRateSlider, () => paramManager.m_human_bt_atts.EnergyDecayRate,
                                v => paramManager.m_human_bt_atts.EnergyDecayRate = v);
        ReplaceAttributesOfSliderInScene(EnergyDecayRateSlider);

        Bind(ShelterEnergyRegenSlider, () => paramManager.m_human_bt_atts.ShelterEnergyRegen,
                                v => paramManager.m_human_bt_atts.ShelterEnergyRegen = v);
        ReplaceAttributesOfSliderInScene(ShelterEnergyRegenSlider);

        Bind(ShelterSafetyRegenSlider, () => paramManager.m_human_bt_atts.ShelterSafetyRegen,
                                v => paramManager.m_human_bt_atts.ShelterSafetyRegen = v);
        ReplaceAttributesOfSliderInScene(ShelterSafetyRegenSlider);

        Bind(SleepEnergyRegenSlider, () => paramManager.m_human_bt_atts.SleepEnergyRegen,
                                v => paramManager.m_human_bt_atts.SleepEnergyRegen = v);
        ReplaceAttributesOfSliderInScene(SleepEnergyRegenSlider);

        Bind(SafetyRegenSlider, () => paramManager.m_human_bt_atts.SafetyRegen,
                                v => paramManager.m_human_bt_atts.SafetyRegen = v);
        ReplaceAttributesOfSliderInScene(SafetyRegenSlider);

        Bind(NightZombieSafetyMultiplierSlider, () => paramManager.m_human_bt_atts.NightZombieSafetyMultiplier,
                                v => paramManager.m_human_bt_atts.NightZombieSafetyMultiplier = v);
        ReplaceAttributesOfSliderInScene(NightZombieSafetyMultiplierSlider);

        Bind(HungerRestoreAmountSlider, () => paramManager.m_human_bt_atts.HungerRestoreAmount,
                                v => paramManager.m_human_bt_atts.HungerRestoreAmount = v);
        ReplaceAttributesOfSliderInScene(HungerRestoreAmountSlider);

        Bind(ZombieDetectionRadiusSlider, () => paramManager.m_human_bt_atts.ZombieDetectionRadius,
                                v => paramManager.m_human_bt_atts.ZombieDetectionRadius = v);
        ReplaceAttributesOfSliderInScene(ZombieDetectionRadiusSlider);

        Bind(RepathIntervalSlider, () => paramManager.m_human_bt_atts.RepathInterval,
                                v => paramManager.m_human_bt_atts.RepathInterval = v);
        ReplaceAttributesOfSliderInScene(RepathIntervalSlider);

        Bind(EatDistanceSlider, () => paramManager.m_human_bt_atts.EatDistance,
                                v => paramManager.m_human_bt_atts.EatDistance = v);
        ReplaceAttributesOfSliderInScene(EatDistanceSlider);

        Bind(MoveSpeedSlider, () => paramManager.m_human_bt_atts.MoveSpeed,
                                v => paramManager.m_human_bt_atts.MoveSpeed = v);
        ReplaceAttributesOfSliderInScene(MoveSpeedSlider);



        Bind(ZombieMaxIdleTimeSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);

        Bind(ZombieDefaultChaseTimeSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);

        Bind(ZombieDefaultChaseCooldownSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);

        Bind(ZombieRepathIntervalSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);

        Bind(ZombieHumanDetectionRadiusSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);

        Bind(ZombieMoveSpeedSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);


        Bind(PassiveMobMaxIdleTimeSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);

        Bind(PassiveMobMoveSpeedSlider, () => paramManager.m_human_bt_atts.MaxEnergy,
                                v => paramManager.m_human_bt_atts.MaxEnergy = v);
    }

    void Update() {
        // update slider attribute values. cosntantly
        ReplaceAttributesOfSliderInScene(MaxHungerSlider);
        ReplaceAttributesOfSliderInScene(MaxEnergySlider);
        ReplaceAttributesOfSliderInScene(MaxSafetySlider);
        ReplaceAttributesOfSliderInScene(SafetyPriorityThresholdSlider);
        ReplaceAttributesOfSliderInScene(HungerHuntThresholdSlider);
        ReplaceAttributesOfSliderInScene(HungerDecayRateSlider);
        ReplaceAttributesOfSliderInScene(NightSafetyDecaySlider);
        ReplaceAttributesOfSliderInScene(EnergyDecayRateSlider);
        ReplaceAttributesOfSliderInScene(ShelterEnergyRegenSlider);
        ReplaceAttributesOfSliderInScene(ShelterSafetyRegenSlider);
        ReplaceAttributesOfSliderInScene(SleepEnergyRegenSlider);
        ReplaceAttributesOfSliderInScene(SafetyRegenSlider);
        ReplaceAttributesOfSliderInScene(NightZombieSafetyMultiplierSlider);
        ReplaceAttributesOfSliderInScene(HungerRestoreAmountSlider);
        ReplaceAttributesOfSliderInScene(ZombieDetectionRadiusSlider);
        ReplaceAttributesOfSliderInScene(RepathIntervalSlider);
        ReplaceAttributesOfSliderInScene(EatDistanceSlider);
        ReplaceAttributesOfSliderInScene(MoveSpeedSlider);
    }
}