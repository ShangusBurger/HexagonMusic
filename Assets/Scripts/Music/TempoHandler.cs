using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TempoHandler : MonoBehaviour
{
    //Tempo
    public static double bpm = 160.0;

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
    

    //Test metronome
    /* public float gain = 0.5F;
    private double nextTick = 0.0F;
    private float amp = 0.0F;
    private float phase = 0.0F;
    private double sampleRate = 0.0F;
    private int accent;
    private bool running = false; */

    // GameObject containers
    public static List<List<GroundTile>> tilesToBeat = new List<List<GroundTile>>();
    public static List<GroundTile> tilesToDeBeat = new List<GroundTile>();

    void Start()
    {
        //accent = signatureHi;
        startDSPTime = AudioSettings.dspTime;
        //sampleRate = AudioSettings.outputSampleRate;
        //nextTick = startDSPTime * sampleRate;
        //running = true;
        beatLength = 60.0 / bpm * 4.0f / signatureLo;
        barLength = beatLength * signatureHi;
        nextBeatTime = startDSPTime + beatLength;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        //If a beat has passed
        if (AudioSettings.dspTime > nextBeatTime)
        {
            TriggerBeat?.Invoke();
            nextBeatTime += beatLength;
        }
        /*if (!running)
            return;

        double samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
        double sample = AudioSettings.dspTime * sampleRate;
        int dataLen = data.Length / channels;
        int n = 0;
        while (n < dataLen)
        {
            float x = gain * amp * Mathf.Sin(phase);
            int i = 0;
            while (i < channels)
            {
                data[n * channels + i] += x;
                i++;
            }
            while (sample + n >= nextTick)
            {
                nextTick += samplesPerTick;
                amp = 1.0F;
                if (++accent > signatureHi)
                {
                    accent = 1;
                    amp *= 2.0F;
                    barNumber++;
                }
            }
            phase += amp * 0.3F;
            amp *= 0.993F;
            n++;
        }
    } */
    }
}
