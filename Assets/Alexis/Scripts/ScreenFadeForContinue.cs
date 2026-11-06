using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ScreenFadeForContinue : MonoBehaviour
{
    public Button button;
    public float fadeTime = 1f;
    public Image fadeImage;
    public AudioSource[] audioSources;
    public AudioSource buttonAudioSource;
    private bool loading;

    void Start() { button.onClick.AddListener(StartFade); }
    public void PlayButtonAudio() { if (buttonAudioSource != null) buttonAudioSource.Play(); }

    public void StartFade()
    {
        if (loading) return;
        loading = true;
        button.interactable = false;
        Time.timeScale = 1f;
        PlayButtonAudio();
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        Color initialColor = fadeImage.color;
        float[] volumes = new float[audioSources.Length];
        for (int i = 0; i < audioSources.Length; i++)
            if (audioSources[i] != null) volumes[i] = audioSources[i].volume;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeTime <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeTime);
            fadeImage.color = Color.Lerp(initialColor, Color.black, t);
            for (int i = 0; i < audioSources.Length; i++)
                if (audioSources[i] != null) audioSources[i].volume = Mathf.Lerp(volumes[i], 0f, t);
            yield return null;
        }
        fadeImage.color = Color.black;
        string scene = GameProgress.PrepareContinue();
        yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
    }
}
