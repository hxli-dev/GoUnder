using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class MouseLook : MonoBehaviour
{
    public Slider slider_x;
    public Slider slider_y;
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    private bool initialized;

    void Start() { InitializeSettings(); }

    public void InitializeSettings()
    {
        if (initialized) return;
        slider_x.SetValueWithoutNotify(PlayerPrefs.GetFloat("xAxis", 500f));
        slider_y.SetValueWithoutNotify(PlayerPrefs.GetFloat("yAxis", 500f));
        initialized = true;
        UpdateControl();
    }

    public void SaveControlSettings()
    {
        if (!initialized) return;
        PlayerPrefs.SetFloat("xAxis", slider_x.value);
        PlayerPrefs.SetFloat("yAxis", slider_y.value);
        PlayerPrefs.Save();
    }

    void OnApplicationFocus(bool inFocus) { if (!inFocus) SaveControlSettings(); }
    void OnDisable() { SaveControlSettings(); }

    public void UpdateControl()
    {
        if (!initialized) return;
        SaveControlSettings();
        CinemachinePOV pov = cinemachineVirtualCamera.GetCinemachineComponent<CinemachinePOV>();
        pov.m_HorizontalAxis.m_MaxSpeed = slider_x.value;
        pov.m_VerticalAxis.m_MaxSpeed = slider_y.value;
    }
}
