using UnityEngine;
using UnityEngine.UI;

public class MusicSound : MonoBehaviour
{
    public Slider slider_music;
    public Slider slider_sound;
    public AudioSource[] backgroundAudio;
    public AudioSource[] soundEffectsAudio;
    private bool initialized;

    void Start() { InitializeSettings(); }

    public void InitializeSettings()
    {
        if (initialized) return;
        slider_music.SetValueWithoutNotify(PlayerPrefs.GetFloat("BackgroundPref", 1f));
        slider_sound.SetValueWithoutNotify(PlayerPrefs.GetFloat("SoundEffectsPref", 1f));
        initialized = true;
        UpdateSound();
    }

    public void SaveSoundSettings()
    {
        if (!initialized) return;
        PlayerPrefs.SetFloat("BackgroundPref", slider_music.value);
        PlayerPrefs.SetFloat("SoundEffectsPref", slider_sound.value);
        PlayerPrefs.Save();
    }

    void OnApplicationFocus(bool inFocus) { if (!inFocus) SaveSoundSettings(); }
    void OnDisable() { SaveSoundSettings(); }
    public void UpdateSound()
    {
        if (!initialized) return;
        foreach (AudioSource source in backgroundAudio) source.volume = slider_music.value;
        foreach (AudioSource source in soundEffectsAudio) source.volume = slider_sound.value;
        SaveSoundSettings();
    }
}
