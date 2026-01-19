using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class LobberTower : Tower
{
    public int minLobDistance = 2;
    public int maxLobDistance = 6;
    public int lobDelay = 2;
    public int lobDistance = -1;

    public GameObject lobProjectilePrefab;
    

    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        base.OnPulseReceived(incomingPulse);

        // Lob the pulse in the same direction it came from, at the set distance
        Coordinate targetCoord = Coordinates.Instance.GetNeighbor(tile.tileCoordinate, incomingPulse.direction, lobDistance);
        
        if (targetCoord != null && lobDistance > 0)
        {
            GroundTile targetGroundTile = targetCoord.go.GetComponent<GroundTile>();
            // Create a pulse that will continue in the same direction
            Pulse lobbedPulse = new Pulse(incomingPulse.direction, continuous: true, source: false, delay: lobDelay);
            targetGroundTile.SchedulePulse(lobbedPulse);

            // Spawn and launch the projectile
            LaunchProjectile(targetGroundTile, lobbedPulse);
        }
    }

     void LaunchProjectile(GroundTile targetTile, Pulse pulse)
    {
        // Calculate flight duration based on lobDelay (in beats)
        double flightDuration = TempoHandler.beatLength * lobDelay;
        
        // Get start and end positions
        Vector3 startPos = transform.position;
        Vector3 targetPos = targetTile.transform.position;
        
        // Instantiate projectile
        GameObject projectile = Instantiate(lobProjectilePrefab, startPos, Quaternion.identity);
        LobProjectile lobScript = projectile.GetComponent<LobProjectile>();
        
        if (lobScript != null)
        {
            lobScript.Initialize(startPos, targetPos, (float)flightDuration, targetTile, (float)(TempoHandler.nextBeatTime - AudioSettings.dspTime) + .1f);
        }
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();

        //pdInstance.SendBang("bang");
    }

    public override void SetSelfUI()
    {
        towerUI.SetDropdown(SampleType.Shaker);
        towerUI.OnSampleSelected(SampleType.Shaker);
    }
}
