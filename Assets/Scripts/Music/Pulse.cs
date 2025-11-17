using System;
using UnityEngine;
using CubeCoordinates;

public class Pulse
{
    public Coordinate originTile;
    public int direction;
    public int delay;
    public int distance;
    public bool continuous;
    public bool source;

    public Pulse(int direction, bool continuous = true, bool source = false, int distance = 1, int delay = 0)
    {
        this.direction = direction;
        this.delay = delay;
        this.distance = distance;
        this.continuous = continuous;
        this.source = source;
    }
}
