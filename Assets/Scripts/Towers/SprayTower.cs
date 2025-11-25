using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class SprayTower : Tower
{
    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        base.OnPulseReceived(incomingPulse);
        
        // Calculate the opposite direction (add 3 to get 180 degrees in hex directions)
        int incomingDir = incomingPulse.direction;
        
        // Get the 3 directions: opposite, and one to each side of opposite
        int leftOfIncoming = (incomingDir + 5) % 6;  // -1 wrapped
        int rightOfIncoming = (incomingDir + 1) % 6; // +1 wrapped
        
        // Create 3 pulses with life of 1 (will only travel 1 tile)
        Pulse sprayLeft = new Pulse(leftOfIncoming, source: true, life: 1);
        Pulse sprayCenter = new Pulse(incomingDir, source: true, life: 1);
        Pulse sprayRight = new Pulse(rightOfIncoming, source: true, life: 1);
        
        tile.SchedulePulse(sprayLeft);
        tile.SchedulePulse(sprayCenter);
        tile.SchedulePulse(sprayRight);
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();
        
        //towerAlreadyActivatedThisBeat = true;
        // if (!isMuted)
        //     pdInstance.SendFloat("trigger", (float)(TempoHandler.nextBeatTime - AudioSettings.dspTime));
    }
}
