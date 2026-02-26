using UnityEngine;

/// <summary>
/// Passer tower: plays a sound sample when a pulse arrives,
/// then lets the signal continue in the same direction unaltered.
/// </summary>
public class PasserTower : Tower
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

        // Re-emit the pulse in the exact same direction — signal passes through
        Pulse passThroughPulse = new Pulse(incomingPulse.direction, source: true, delay: incomingPulse.delay);
        tile.SchedulePulse(passThroughPulse);
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();
    }

    public override void SetSelfUI()
    {
        towerUI.SetDropdown("Clap");
        towerUI.OnSampleSelected("Clap");
    }
}