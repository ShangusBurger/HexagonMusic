using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class SplitterTower : Tower
{
    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {

        base.OnPulseReceived(incomingPulse);
        directions.Clear();
        directions.Add((incomingPulse.direction + 1) % 6);
        directions.Add((incomingPulse.direction + 5) % 6);

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
        towerUI.SetDropdown("Snare");
        towerUI.OnSampleSelected("Snare");
    }

    public override void AnimatePulse(int direction)
    {
        if (GetComponent<Animator>() != null)
        {
            Animator anim = GetComponent<Animator>();
            anim.SetInteger("direction", direction % 2);
            anim.SetTrigger("Pulse");
        }
    }
}