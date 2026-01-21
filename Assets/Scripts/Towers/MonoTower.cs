using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class MonoTower : Tower
{
    internal override void Start()
    {
        base.Start();
    }
    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        base.OnPulseReceived(incomingPulse);

        // Create a new pulse in the redirect direction
        if (directions.Count > 0)
        {
            Pulse redirectedPulse = new Pulse(directions[0], source: true, delay: incomingPulse.delay);
            tile.SchedulePulse(redirectedPulse);
        }
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();
        // towerAlreadyActivatedThisBeat = true;
        // if (!isMuted)
        //     pdInstance.SendFloat("trigger", (float)(TempoHandler.nextBeatTime - AudioSettings.dspTime));
    }

    public override void SetSelfUI()
    {
        towerUI.SetDropdown("Hi-Hat");
        towerUI.OnSampleSelected("Hi-Hat");
    }
}
