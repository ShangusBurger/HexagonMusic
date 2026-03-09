using System;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════════════════
//  PROGRESS SAVE DATA  (serialized to JSON on disk)
// ══════════════════════════════════════════════════════════════════

[Serializable]
public class ProgressSaveData
{
    public int totalTowerInteractions;
    public int totalSoundChanges;
    public List<TrackSaveEntry> trackProgress;
    public List<int> unlockedTowers;
    public List<string> unlockedSamples;
    public float masterVolume;
}

[Serializable]
public class TrackSaveEntry
{
    public string trackId;
    public int currentLevel;
}

// ══════════════════════════════════════════════════════════════════
//  TOWER SAVE ENTRY  (used internally during map encode/decode)
// ══════════════════════════════════════════════════════════════════

[Serializable]
public class TowerSaveEntry
{
    public int q;                    // cube x
    public int r;                    // cube y  (z = -q - r)
    public int towerType;            // TowerType enum as int
    public int sampleIndex;          // index into palette (-1 if none)
    public string sampleName;        // resolved after decode
    public List<int> directions;     // direction list
    public int lobDistance;           // -1 if not a lobber
    public int bufferThreshold;      // -1 if not a buffer
    public bool isMuted;
}