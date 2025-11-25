using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayButtonController : MonoBehaviour
{
    [SerializeField] private Button playButton;
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
        
        playButton.onClick.AddListener(TriggerPlay);
    }
    
    public void TriggerPlay()
    {
        // Notify all Source Towers to trigger their pulses
        OnTriggerPlay?.Invoke();
        isPlaying = true;
        playButton.GetComponentInChildren<Image>().color = Color.red;
        playButton.GetComponentInChildren<TMP_Text>().text = "STOP";
        playButton.onClick.RemoveListener(TriggerPlay);
        playButton.onClick.AddListener(TriggerStop);
    }

    public void TriggerStop()
    {
        OnTriggerStop?.Invoke();
        isPlaying = false;
        playButton.GetComponentInChildren<Image>().color = Color.green;
        playButton.GetComponentInChildren<TMP_Text>().text = "PLAY";
        playButton.onClick.RemoveListener(TriggerStop);
        playButton.onClick.AddListener(TriggerPlay);
    }
}