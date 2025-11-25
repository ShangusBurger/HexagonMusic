using System;
using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class SourceTower : Tower
{
    private bool isPerpetual = false;
    internal override void Start()
    {
        base.Start();
        if ((!_audioSources[0].isPlaying || !_audioSources[1].isPlaying) && isPerpetual)
        {
            PlayScheduledClip();
        }
        PlayButtonController.OnTriggerPlay += OnPlayButtonPressed;
    }

    void OnPlayButtonPressed()
    {
        OnPulseReceived(null);
    }

    internal override void Update()
    {
        base.Update();
        //schedules next beat once the instrument has sounded
        if (AudioSettings.dspTime > goalTime && isPerpetual)
        {
            TriggerPulses();
            goalTime += TempoHandler.barLength;
            PlayScheduledClip();
        }
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        if (!isPerpetual)
        {
            TriggerPulses();
        }

    }

    internal override void PlayScheduledClip()
    {
        if (!isPerpetual)
        {
            goalTime = TempoHandler.nextBeatTime;
        }
        base.PlayScheduledClip();
    }

    void TriggerPulses()
    {
        tile.SchedulePulse(new Pulse(0, source: true));
        tile.SchedulePulse(new Pulse(2, source: true));   
        tile.SchedulePulse(new Pulse(4, source: true));
    }

    

  
}
