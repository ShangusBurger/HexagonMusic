using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TempoHandler : MonoBehaviour
{
    //Tempo
    public static double bpm = 172.0;

    //Beat tracking
    public static double startDSPTime = 0.0;
    public static double nextBeatTime = 0.0;
    public static double barLength;
    public static double beatLength; 
    
    public static int signatureHi = 24;
    public static int signatureLo = 8;
    public static int barNumber = 0;
    public static int beatNumber = 0;
    public static event Action TriggerBeat;

    // GameObject containers
    public static List<List<GroundTile>> tilesToBeat = new List<List<GroundTile>>();
    public static List<GroundTile> tilesToDeBeat = new List<GroundTile>();

    // Audio Data
    public int audioSampleRate;

    void Start()
    {
        startDSPTime = AudioSettings.dspTime;
        beatLength = 60.0 / bpm * 4.0 / (double) signatureLo;
        barLength = beatLength * (double) signatureHi;
        nextBeatTime = startDSPTime + beatLength;
        audioSampleRate = AudioSettings.outputSampleRate;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        float timeLengthOfBuffer = (float) data.Length / (float) audioSampleRate;

        //If a beat has passed
        while (AudioSettings.dspTime > nextBeatTime)
        {
            TriggerBeat?.Invoke();
            nextBeatTime += beatLength;
        }
    }
}
