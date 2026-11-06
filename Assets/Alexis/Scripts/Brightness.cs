using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Brightness : MonoBehaviour
{
    public Slider slider;
    public Volume globalVolume;
    private ColorAdjustments colorAdjustments;
    private bool initialized;

    void Start() { InitializeSettings(); }

    public void InitializeSettings()
    {
        if (initialized) return;
        if (!globalVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("Brightness requires ColorAdjustments in the assigned Volume.", this);
            return;
        }
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat("ExposurePref", 0.15f));
        initialized = true;
        UpdateExposure();
    }

    public void SaveExposureSettings()
    {
        if (!initialized) return;
        PlayerPrefs.SetFloat("ExposurePref", slider.value);
        PlayerPrefs.Save();
    }

    void OnApplicationFocus(bool inFocus) { if (!inFocus) SaveExposureSettings(); }
    void OnDisable() { SaveExposureSettings(); }
    public void UpdateExposure()
    {
        if (!initialized) return;
        colorAdjustments.postExposure.value = slider.value;
        SaveExposureSettings();
    }
}
