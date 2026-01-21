using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class SwitcherTower : Tower
{
    bool outputtingLeft;

    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {

        base.OnPulseReceived(incomingPulse);
        directions.Clear();

        if (outputtingLeft)
        {
            directions.Add((incomingPulse.direction + 1) % 6);
        }
        else
        {
            directions.Add((incomingPulse.direction + 5) % 6);
        }

        outputtingLeft = !outputtingLeft;

        // Create a new pulse in the redirect direction
        if (directions.Count > 0)
        {
            Pulse redirectedPulse;
            foreach (int direction in directions)
            {
                redirectedPulse = new Pulse(direction, source: true);
                tile.SchedulePulse(redirectedPulse);
            }
        }
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();
    }

    public override void SetSelfUI()
    {
        towerUI.SetDropdown("Shaker");
        towerUI.OnSampleSelected("Shaker");
    }
}