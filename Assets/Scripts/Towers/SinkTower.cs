using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class SinkTower : Tower
{   
    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        base.OnPulseReceived(incomingPulse);
        //creates pulse that shows up on self only, does not propigate
        Pulse redirectedPulse = new Pulse(0, source: true, continuous: false, life: 0);
        tile.SchedulePulse(redirectedPulse);
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
       
        base.PlayScheduledClip();
        towerAlreadyActivatedThisBeat = true;
    }

    public override void SetSelfUI()
    {
        towerUI.SetDropdown(SampleType.Snap);
        towerUI.OnSampleSelected(SampleType.Snap);
    }
}
