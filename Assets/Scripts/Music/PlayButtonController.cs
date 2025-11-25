using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayButtonController : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button stopButton;
    public static PlayButtonController Instance;
    
    // Event that Source Towers can subscribe to
    public static event System.Action OnTriggerPlay;
    public static event System.Action OnTriggerStop;

    // Play/Stop Images
    [SerializeField] private Sprite playImage;
    [SerializeField] private Sprite stopImage;

    private bool isPlaying = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        stopButton.gameObject.SetActive(false);
    }
    
    public void TriggerPlay()
    {
        // Notify all Source Towers to trigger their pulses
        OnTriggerPlay?.Invoke();
        isPlaying = true;
        playButton.gameObject.SetActive(false);
        stopButton.gameObject.SetActive(true);
    }

    public void TriggerStop()
    {
        OnTriggerStop?.Invoke();
        isPlaying = false;
        stopButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(true);
    }
}