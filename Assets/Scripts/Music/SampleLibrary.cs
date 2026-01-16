using System;
using System.Collections.Generic;
using UnityEngine;

// Enum for available sample types
public enum SampleType
{
    BassDrum,
    Snare,
    HiHat,
    Tom,
    Shaker
}

[Serializable]
public class AudioSampleEntry
{
    public SampleType sampleType;
    public AudioClip clip;
}

public class SampleLibrary : MonoBehaviour
{
    public static SampleLibrary Instance { get; private set; }

    [SerializeField] private List<AudioSampleEntry> samples = new List<AudioSampleEntry>();

    private Dictionary<SampleType, AudioClip> sampleLookup;

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
        sampleLookup = new Dictionary<SampleType, AudioClip>();
        foreach (var entry in samples)
        {
            if (!sampleLookup.ContainsKey(entry.sampleType))
            {
                sampleLookup.Add(entry.sampleType, entry.clip);
            }
        }
    }

    public AudioClip GetSample(SampleType type)
    {
        if (sampleLookup != null && sampleLookup.TryGetValue(type, out AudioClip clip))
        {
            return clip;
        }
        return null;
    }

    public List<string> GetSampleNames()
    {
        List<string> names = new List<string>();
        foreach (SampleType type in Enum.GetValues(typeof(SampleType)))
        {
            names.Add(FormatSampleName(type));
        }
        return names;
    }

    public static string FormatSampleName(SampleType type)
    {
        switch (type)
        {
            case SampleType.BassDrum: return "Bass Drum";
            case SampleType.Snare: return "Snare";
            case SampleType.HiHat: return "Hi-Hat";
            case SampleType.Tom: return "Tom";
            case SampleType.Shaker: return "Shaker";
            default: return type.ToString();
        }
    }
}

