using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private string exposedParameter = "MasterVolume";

    void Start()
    {
        // Initialize slider to current mixer value
        if (audioMixer.GetFloat(exposedParameter, out float currentValue))
        {
            volumeSlider.value = Mathf.Pow(10f, currentValue / 20f);
        }

        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float sliderValue)
    {
        // Convert linear (0-1) to decibels (-80dB to 0dB)
        float dB = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20f : -80f;
        audioMixer.SetFloat(exposedParameter, dB);
    }

    void OnDestroy()
    {
        volumeSlider.onValueChanged.RemoveListener(SetVolume);
    }
}