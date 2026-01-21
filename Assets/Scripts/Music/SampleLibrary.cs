using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Enum for available sample types
public enum SampleType
{
    Percussion,
    Chord
}

[Serializable]
public class AudioSampleEntry
{
    public SampleType sampleType;
    public AudioClip clip;
    public string name;
    public AudioMixerGroup mixer;
}

public class SampleLibrary : MonoBehaviour
{
    public static SampleLibrary Instance { get; private set; }

    public List<AudioSampleEntry> samples = new List<AudioSampleEntry>();

    public Dictionary<string, AudioSampleEntry> sampleLookup;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildLookup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void BuildLookup()
    {
        sampleLookup = new Dictionary<string, AudioSampleEntry>();
        foreach (var entry in samples)
        {
            if (!sampleLookup.ContainsKey(entry.name))
            {
                sampleLookup.Add(entry.name, entry);
            }
        }
    }

    public AudioClip GetSample(string sampleName)
    {
        if (sampleLookup != null && sampleLookup.TryGetValue(sampleName, out AudioSampleEntry entry))
        {
            return entry.clip;
        }
        return null;
    }

    public List<string> GetSampleNames()
    {
        List<string> names = new List<string>();
        foreach (AudioSampleEntry entry in samples)
        {
            names.Add(entry.name);
        }
        return names;
    }
}

