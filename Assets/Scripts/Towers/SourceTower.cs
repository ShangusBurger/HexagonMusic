using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class SourceTower : Tower
{
    //[SerializeField] private int sourceTowerReach = 12;
    internal override void Start()
    {
        base.Start();
        if (!_audioSources[0].isPlaying || !_audioSources[1].isPlaying)
        {
            PlayScheduledClip();
        }
    }
    
    internal override void Update()
    {
        base.Update();
        //schedules next beat once the instrument has sounded
        if (AudioSettings.dspTime > goalTime)
        {
            TriggerPulses();
            goalTime += TempoHandler.barLength;
            PlayScheduledClip();
        }
    }

    void TriggerPulses()
    {
        tile.SchedulePulse(new Pulse(0, source: true));
        tile.SchedulePulse(new Pulse(2, source: true));   
        tile.SchedulePulse(new Pulse(4, source: true));
    }
}
