using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class MirrorTower : Tower
{
    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        base.OnPulseReceived(incomingPulse);
        directions.Clear();

        // Two directions nearest to the reverse of the incoming direction
        // If incoming is D, reverse is D+3, neighbors of reverse are D+2 and D+4
        directions.Add((incomingPulse.direction + 2) % 6);
        directions.Add((incomingPulse.direction + 4) % 6);

        foreach (int direction in directions)
        {
            Pulse redirectedPulse = new Pulse(direction, source: true);
            tile.SchedulePulse(redirectedPulse);
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