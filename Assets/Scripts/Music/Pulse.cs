using System;
using UnityEngine;
using CubeCoordinates;
using System.Collections.Generic;

public class Pulse
{
    public Coordinate originTile;
    public int direction;
    public int delay;
    public int distance;
    public bool continuous;
    public bool source;
    public int life; // number of beats until the pulse should not propigate

    public Pulse(int direction, bool continuous = true, bool source = false, int distance = 1, int delay = 0, int life = -1)
    {
        this.direction = direction;
        this.delay = delay;
        this.distance = distance;
        this.continuous = continuous;
        this.source = source;
        this.life = life;

        PlayButtonController.OnTriggerStop += Kill;
    }

    void Kill()
    {
        life = 0;
    }
}
