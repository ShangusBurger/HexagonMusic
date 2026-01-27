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
    private bool pulseFoundThisBeat;
    
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        stopButton.gameObject.SetActive(false);
        GroundTile.PulseExistsNotif += PulseFound;
    }
    
    public void TriggerPlay()
    {
        // Notify all Source Towers to trigger their pulses
        OnTriggerPlay?.Invoke();
        isPlaying = true;
        playButton.gameObject.SetActive(false);
        stopButton.gameObject.SetActive(true);

        pulseFoundThisBeat = true;
    }

    public void TriggerStop()
    {
        OnTriggerStop?.Invoke();
        isPlaying = false;
        stopButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!pulseFoundThisBeat)
        {
            TriggerStop();
        }
        pulseFoundThisBeat = false;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPlaying)
            {
                TriggerStop();
            }
            else TriggerPlay();
            
        }
    }

    void PulseFound()
    {
        pulseFoundThisBeat = true;
    }
    


}