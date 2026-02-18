using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TempoHandler : MonoBehaviour
{
    // Tempo
    public static double bpm = 172.0;

    // Beat tracking
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

    // Thread-safe beat queue (stores the nextBeatTime value towers should see)
    private readonly Queue<double> _scheduledNextBeatTimes = new Queue<double>();
    private readonly object _lock = new object();

    void Start()
    {
        startDSPTime = AudioSettings.dspTime;
        beatLength = 60.0 / bpm * 4.0 / (double)signatureLo;
        barLength = beatLength * (double)signatureHi;
        nextBeatTime = startDSPTime + beatLength;
        audioSampleRate = AudioSettings.outputSampleRate;
    }

    // Audio thread: detect beats and enqueue them
    void OnAudioFilterRead(float[] data, int channels)
    {
        while (AudioSettings.dspTime > nextBeatTime)
        {
            nextBeatTime += beatLength;
            lock (_lock)
            {
                // Store the nextBeatTime towers should see when this beat fires
                _scheduledNextBeatTimes.Enqueue(nextBeatTime);
            }
        }
    }

    // Main thread: process at most one beat per frame
    void Update()
    {
        lock (_lock)
        {
            if (_scheduledNextBeatTimes.Count > 0)
            {
                // Restore nextBeatTime so towers read the correct scheduling target
                nextBeatTime = _scheduledNextBeatTimes.Dequeue();
                TriggerBeat?.Invoke();
            }
        }
    }
}