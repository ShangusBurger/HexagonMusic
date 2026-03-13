using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using CubeCoordinates;
using UnityEngine.UI;

/// <summary>
/// Central save/load manager for HexMusic.
/// Handles both progress saves (automatic/pause menu) and map saves (clipboard-friendly).
/// Attach to a persistent GameObject in the scene.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string volumeParameter = "MasterVolume";

    // ── File paths ────────────────────────────────────────────────────
    private static string ProgressFilePath => Path.Combine(Application.persistentDataPath, "hexmusic_progress.json");
    private static string MapFolderPath => Path.Combine(Application.persistentDataPath, "Maps");

    // ── Events ────────────────────────────────────────────────────────
    public static event Action OnProgressSaved;
    public static event Action OnProgressLoaded;
    public static event Action OnMapSaved;
    public static event Action OnMapLoaded;

    // ── Map encoding header ───────────────────────────────────────────
    private const string MAP_HEADER = "HM1:";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnApplicationQuit()
    {
        SaveProgress();
    }

    // ══════════════════════════════════════════════════════════════════
    //  PROGRESS SAVE / LOAD
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saves all player progress to a local JSON file.
    /// Call from pause menu button or on application quit.
    /// </summary>
    public void SaveProgress()
    {
        var data = new ProgressSaveData();

        // ── Player stats ──
        if (PlayerStats.Instance != null)
        {
            data.totalTowerInteractions = PlayerStats.Instance.TotalTowerInteractions;
            data.totalSoundChanges = PlayerStats.Instance.TotalSoundChanges;
        }

        // ── Track progress ──
        if (ProgressHandler.Instance != null)
        {
            var tracks = ProgressHandler.Instance.GetAllTracks();
            data.trackProgress = new List<TrackSaveEntry>();
            for (int i = 0; i < tracks.Count; i++)
            {
                var state = ProgressHandler.Instance.GetTrackStateByIndex(i);
                if (state != null)
                {
                    data.trackProgress.Add(new TrackSaveEntry
                    {
                        trackId = state.track.trackId,
                        currentLevel = state.currentLevel
                    });
                }
            }
        }

        // ── Unlocks ──
        if (UnlockManager.Instance != null)
        {
            data.unlockedTowers = new List<int>();
            foreach (var t in UnlockManager.Instance.GetUnlockedTowers())
                data.unlockedTowers.Add((int)t);

            data.unlockedSamples = new List<string>(UnlockManager.Instance.GetUnlockedSamples());
        }

        // ── Master volume ──
        float vol = 0f;
        if (audioMixer != null && audioMixer.GetFloat(volumeParameter, out vol))
            data.masterVolume = vol;
        else
            data.masterVolume = 0f;

        // ── Write to disk ──
        string json = JsonUtility.ToJson(data, true);
        try
        {
            File.WriteAllText(ProgressFilePath, json);
            Debug.Log($"[SaveManager] Progress saved to {ProgressFilePath}");
            OnProgressSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save progress: {e.Message}");
        }
    }

    /// <summary>
    /// Loads player progress from disk and applies it to all systems.
    /// Call on game startup or from the main menu.
    /// </summary>
    public bool LoadProgress()
    {
        if (!File.Exists(ProgressFilePath))
        {
            Debug.Log("[SaveManager] No progress save file found.");
            return false;
        }

        string json;
        try
        {
            json = File.ReadAllText(ProgressFilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to read progress file: {e.Message}");
            return false;
        }

        ProgressSaveData data;
        try
        {
            data = JsonUtility.FromJson<ProgressSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to parse progress data: {e.Message}");
            return false;
        }

        // ── Restore player stats ──
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.SetStats(data.totalTowerInteractions, data.totalSoundChanges);
        }

        // ── Restore unlocks (before track progress so goals evaluate correctly) ──
        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.ResetToInitial();

            if (data.unlockedTowers != null)
                foreach (int t in data.unlockedTowers)
                    UnlockManager.Instance.UnlockTower((TowerType)t);

            if (data.unlockedSamples != null)
                foreach (string s in data.unlockedSamples)
                    UnlockManager.Instance.UnlockSample(s);
        }

        // ── Restore track progress ──
        if (ProgressHandler.Instance != null && data.trackProgress != null)
        {
            foreach (var entry in data.trackProgress)
            {
                ProgressHandler.Instance.SetTrackLevel(entry.trackId, entry.currentLevel);
            }
        }

        // ── Restore master volume ──
        if (audioMixer != null)
        {
            audioMixer.SetFloat(volumeParameter, data.masterVolume);
        }

        Debug.Log("[SaveManager] Progress loaded successfully.");
        OnProgressLoaded?.Invoke();
        return true;
    }

    /// <summary>
    /// Returns true if a progress save file exists on disk.
    /// </summary>
    public bool HasProgressSave()
    {
        return File.Exists(ProgressFilePath);
    }

    /// <summary>
    /// Deletes the progress save file. Use with caution.
    /// </summary>
    public void DeleteProgressSave()
    {
        if (File.Exists(ProgressFilePath))
        {
            File.Delete(ProgressFilePath);
            Debug.Log("[SaveManager] Progress save deleted.");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  MAP SAVE / LOAD
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saves the current tower layout to a local file.
    /// Returns the encoded string.
    /// </summary>
    public string SaveMapToLocal(string mapName)
    {
        if (string.IsNullOrEmpty(mapName))
            mapName = "Untitled";

        string encoded = EncodeCurrentMap(mapName);
        SaveMapToFile(mapName, encoded);

        Debug.Log($"[SaveManager] Map '{mapName}' saved locally.");
        OnMapSaved?.Invoke();
        return encoded;
    }

    /// <summary>
    /// Encodes the current tower layout and copies it to the system clipboard.
    /// Uses the provided name embedded in the string so recipients can see it.
    /// Returns the encoded string.
    /// </summary>
    public string CopyMapToClipboard(string mapName)
    {
        if (string.IsNullOrEmpty(mapName))
            mapName = "Untitled";

        string encoded = EncodeCurrentMap(mapName);
        GUIUtility.systemCopyBuffer = encoded;

        Debug.Log($"[SaveManager] Map '{mapName}' copied to clipboard ({encoded.Length} chars).");
        return encoded;
    }

    /// <summary>
    /// Imports an encoded map string into the local saved maps folder
    /// WITHOUT loading it into the game. Extracts the map name from
    /// the encoded data. Returns the extracted name, or null on failure.
    /// </summary>
    public string ImportMap(string encodedMap)
    {
        if (string.IsNullOrEmpty(encodedMap))
        {
            Debug.LogWarning("[SaveManager] Empty map string.");
            return null;
        }

        // Decode just enough to extract the name
        string mapName = ExtractMapName(encodedMap);
        if (mapName == null)
        {
            Debug.LogWarning("[SaveManager] Failed to extract map name.");
            return null;
        }

        // Ensure unique file name if a map with this name already exists
        string uniqueName = GetUniqueMapName(mapName);
        SaveMapToFile(uniqueName, encodedMap);

        Debug.Log($"[SaveManager] Imported map '{uniqueName}' to saved maps.");
        return uniqueName;
    }

    /// <summary>
    /// Loads a map from an encoded string into the game.
    /// Clears the current field first, then rebuilds all towers.
    /// </summary>
    public bool LoadMap(string encodedMap)
    {
        if (string.IsNullOrEmpty(encodedMap))
        {
            Debug.LogWarning("[SaveManager] Empty map string.");
            return false;
        }

        List<TowerSaveEntry> towers = DecodeMap(encodedMap);
        if (towers == null)
        {
            Debug.LogWarning("[SaveManager] Failed to decode map string.");
            return false;
        }

        // Suppress interaction counting for the clear + rebuild
        Tower.SuppressInteractions = true;

        // Clear undo history since the map state is being replaced
        if (UndoManager.Instance != null)
            UndoManager.Instance.ClearHistory();

        // Clear existing towers
        ClearFieldController.Instance?.ClearAllTowers();

        // Rebuild towers after a frame so the clear has time to process
        StartCoroutine(RebuildTowersNextFrame(towers, lastDecodedBpm));
        return true;
    }

    /// <summary>
    /// Checks an encoded map string for towers or samples that the player
    /// hasn't unlocked yet. Returns a human-readable warning message,
    /// or null if everything is unlocked.
    /// </summary>
    public string CheckMapForLockedContent(string encodedMap)
    {
        if (string.IsNullOrEmpty(encodedMap) || UnlockManager.Instance == null)
            return null;

        List<TowerSaveEntry> towers = DecodeMap(encodedMap);
        if (towers == null) return null;

        var lockedTowers = new HashSet<string>();
        var lockedSamples = new HashSet<string>();

        foreach (var entry in towers)
        {
            // Check tower type
            TowerType type = (TowerType)entry.towerType;
            if (!UnlockManager.Instance.IsTowerUnlocked(type))
                lockedTowers.Add(type.ToString());

            // Check sample
            if (!string.IsNullOrEmpty(entry.sampleName)
                && !UnlockManager.Instance.IsSampleUnlocked(entry.sampleName))
                lockedSamples.Add(entry.sampleName);
        }

        if (lockedTowers.Count == 0 && lockedSamples.Count == 0)
            return null;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("This map contains content you haven't unlocked yet:");
        sb.AppendLine();

        if (lockedTowers.Count > 0)
            sb.AppendLine("Towers: " + string.Join(", ", lockedTowers));

        if (lockedSamples.Count > 0)
            sb.AppendLine("Sounds: " + string.Join(", ", lockedSamples));

        sb.AppendLine();
        sb.Append("Load anyway?");

        return sb.ToString();
    }

    /// <summary>
    /// Reads the raw encoded string for a locally saved map file.
    /// Returns null if the file doesn't exist.
    /// </summary>
    public string ReadMapFileRaw(string mapName)
    {
        string path = Path.Combine(MapFolderPath, mapName + ".hexmap");
        if (!File.Exists(path)) return null;

        try { return File.ReadAllText(path); }
        catch { return null; }
    }

    /// <summary>
    /// Loads a map from a locally saved file by name.
    /// </summary>
    public bool LoadMapFromFile(string mapName)
    {
        string path = Path.Combine(MapFolderPath, mapName + ".hexmap");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] Map file not found: {path}");
            return false;
        }

        try
        {
            string encoded = File.ReadAllText(path);
            return LoadMap(encoded);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to read map file: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Returns a list of all locally saved map names.
    /// </summary>
    public List<string> GetSavedMapNames()
    {
        var names = new List<string>();
        if (!Directory.Exists(MapFolderPath)) return names;

        foreach (string file in Directory.GetFiles(MapFolderPath, "*.hexmap"))
        {
            names.Add(Path.GetFileNameWithoutExtension(file));
        }
        return names;
    }

    /// <summary>
    /// Deletes ALL locally saved map files.
    /// </summary>
    public void DeleteAllMaps()
    {
        if (!Directory.Exists(MapFolderPath)) return;

        foreach (string file in Directory.GetFiles(MapFolderPath, "*.hexmap"))
        {
            try { File.Delete(file); }
            catch (Exception e) { Debug.LogWarning($"[SaveManager] Could not delete {file}: {e.Message}"); }
        }
        Debug.Log("[SaveManager] All saved maps deleted.");
    }

    /// <summary>
    /// Resets all progression, stats, and unlocks back to initial state.
    /// </summary>
    public void ResetAllProgress()
    {
        // Reset track progress and unlocks
        if (ProgressHandler.Instance != null)
            ProgressHandler.Instance.ResetAllProgress();

        // Reset lifetime stats
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.ResetStats();

        // Reset tutorial seen state so intro tutorials replay
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.ResetSeenTutorials();

        // Delete the save file so it doesn't reload on next launch
        DeleteProgressSave();

        Debug.Log("[SaveManager] All progress reset.");
    }

    /// <summary>
    /// Deletes a locally saved map file.
    /// </summary>
    public void DeleteMapFile(string mapName)
    {
        string path = Path.Combine(MapFolderPath, mapName + ".hexmap");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveManager] Deleted map: {mapName}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  MAP ENCODING  (compact clipboard-friendly format)
    // ══════════════════════════════════════════════════════════════════
    //
    //  Format (before Base64):
    //    <mapName>|<bpm>|<samplePalette>|<towerEntry>;<towerEntry>;...
    //
    //  mapName       = user-provided name for the composition
    //  bpm           = tempo as a double
    //  samplePalette = comma-separated sample names used in the map
    //  towerEntry    = q,r,T,S,D,L,B,M
    //    q,r   = cube coords (s is implicit: s = -q - r)
    //    T     = TowerType int
    //    S     = sample palette index (-1 if no sample / Source with default)
    //    D     = directions joined with '.' or '-' if none
    //    L     = lob distance (-1 if not lobber)
    //    B     = buffer threshold (-1 if not buffer)
    //    M     = muted flag (0 or 1)
    //
    // ══════════════════════════════════════════════════════════════════

    private string EncodeCurrentMap(string mapName)
    {
        var sb = new StringBuilder();

        // Section 1: map name
        sb.Append(mapName);
        sb.Append('|');

        // Section 2: tempo
        sb.Append(TempoHandler.bpm.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        sb.Append('|');

        if (Tower.allTowers == null || Tower.allTowers.Count == 0)
        {
            // Empty map: name + bpm + empty palette + no towers
            sb.Append('|');
            string emptyB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
            return MAP_HEADER + emptyB64;
        }

        // Build sample palette
        var palette = new List<string>();
        var entries = new List<TowerSaveEntry>();

        foreach (Tower t in Tower.allTowers)
        {
            if (t == null || t.tile == null || t.tile.tileCoordinate == null) continue;

            var entry = new TowerSaveEntry();
            Vector3 cube = t.tile.tileCoordinate.cube;
            entry.q = Mathf.RoundToInt(cube.x);
            entry.r = Mathf.RoundToInt(cube.y);
            entry.towerType = (int)t.ownType;
            entry.isMuted = t.isMuted;

            // Sample name
            string sampleName = GetSampleNameFromClip(t.playbackClip);
            if (!string.IsNullOrEmpty(sampleName))
            {
                if (!palette.Contains(sampleName))
                    palette.Add(sampleName);
                entry.sampleIndex = palette.IndexOf(sampleName);
            }
            else
            {
                entry.sampleIndex = -1;
            }

            // Directions
            entry.directions = t.directions != null ? new List<int>(t.directions) : new List<int>();

            // Lobber distance
            entry.lobDistance = (t is LobberTower lob) ? lob.lobDistance : -1;

            // Buffer threshold
            entry.bufferThreshold = (t is BufferTower buf) ? buf.threshold : -1;

            entries.Add(entry);
        }

        // Section 3: sample palette
        sb.Append(string.Join(",", palette));
        sb.Append('|');

        // Section 4: tower entries
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (i > 0) sb.Append(';');

            string dirs = e.directions.Count > 0
                ? string.Join(".", e.directions)
                : "-";

            sb.Append($"{e.q},{e.r},{e.towerType},{e.sampleIndex},{dirs},{e.lobDistance},{e.bufferThreshold},{(e.isMuted ? 1 : 0)}");
        }

        // Base64 encode for clean clipboard text
        string raw = sb.ToString();
        string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return MAP_HEADER + b64;
    }

    /// <summary>
    /// Extracts just the map name from an encoded string without full decode.
    /// Returns null if the string is invalid.
    /// </summary>
    private string ExtractMapName(string encoded)
    {
        if (!encoded.StartsWith(MAP_HEADER)) return null;

        string b64 = encoded.Substring(MAP_HEADER.Length);
        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }
        catch
        {
            return null;
        }

        int firstPipe = raw.IndexOf('|');
        if (firstPipe < 0) return null;

        string name = raw.Substring(0, firstPipe).Trim();
        return string.IsNullOrEmpty(name) ? "Untitled" : name;
    }

    // ── Last decoded BPM (set by DecodeMap, read by LoadMap) ─────────
    private double lastDecodedBpm = -1;

    private List<TowerSaveEntry> DecodeMap(string encoded)
    {
        lastDecodedBpm = -1;

        if (!encoded.StartsWith(MAP_HEADER))
        {
            Debug.LogWarning("[SaveManager] Invalid map header.");
            return null;
        }

        string b64 = encoded.Substring(MAP_HEADER.Length);
        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }
        catch
        {
            Debug.LogWarning("[SaveManager] Failed to decode Base64 map data.");
            return null;
        }

        // Split into 4 sections: name | bpm | palette | towers
        string[] sections = raw.Split(new char[] { '|' }, 4);
        if (sections.Length < 4)
        {
            Debug.LogWarning("[SaveManager] Invalid map format (expected name|bpm|palette|towers).");
            return null;
        }

        if (double.TryParse(sections[1], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double parsedBpm))
            lastDecodedBpm = parsedBpm;

        string paletteStr = sections[2];
        string towersStr = sections[3];
        // Parse palette
        string[] palette = paletteStr.Length > 0
            ? paletteStr.Split(',')
            : new string[0];

        // Parse tower entries
        if (string.IsNullOrEmpty(towersStr))
            return new List<TowerSaveEntry>();

        string[] towerTokens = towersStr.Split(';');
        var result = new List<TowerSaveEntry>();

        foreach (string token in towerTokens)
        {
            if (string.IsNullOrWhiteSpace(token)) continue;

            string[] parts = token.Split(',');
            if (parts.Length < 8)
            {
                Debug.LogWarning($"[SaveManager] Skipping malformed tower entry: {token}");
                continue;
            }

            var entry = new TowerSaveEntry();
            entry.q = int.Parse(parts[0]);
            entry.r = int.Parse(parts[1]);
            entry.towerType = int.Parse(parts[2]);
            entry.sampleIndex = int.Parse(parts[3]);

            // Directions
            string dirStr = parts[4];
            entry.directions = new List<int>();
            if (dirStr != "-")
            {
                foreach (string d in dirStr.Split('.'))
                    entry.directions.Add(int.Parse(d));
            }

            entry.lobDistance = int.Parse(parts[5]);
            entry.bufferThreshold = int.Parse(parts[6]);
            entry.isMuted = parts[7] == "1";

            // Resolve sample name from palette
            if (entry.sampleIndex >= 0 && entry.sampleIndex < palette.Length)
                entry.sampleName = palette[entry.sampleIndex];

            result.Add(entry);
        }

        return result;
    }

    // ══════════════════════════════════════════════════════════════════
    //  TOWER REBUILDING
    // ══════════════════════════════════════════════════════════════════

    private IEnumerator RebuildTowersNextFrame(List<TowerSaveEntry> towers, double bpm)
    {
        // Suppress interaction counting for the entire rebuild
        Tower.SuppressInteractions = true;

        // Wait one frame for ClearField to finish
        yield return null;

        // Apply tempo if it was saved
        if (bpm > 0)
        {
            TempoHandler.ChangeBPM(bpm);

            // Update the Source tower's tempo slider UI to match
            StartCoroutine(ApplyDeferredTempoSlider(bpm));
        }

        Container allTiles = TileMapConstructor.allTiles;
        if (allTiles == null)
        {
            Debug.LogError("[SaveManager] TileMapConstructor.allTiles is null — cannot rebuild map.");
            Tower.SuppressInteractions = false;
            yield break;
        }

        foreach (var entry in towers)
        {
            Vector3 cube = new Vector3(entry.q, entry.r, -entry.q - entry.r);
            Coordinate coord = allTiles.GetCoordinate(cube);
            if (coord == null)
            {
                Debug.LogWarning($"[SaveManager] No tile at cube ({entry.q},{entry.r}) — skipping.");
                continue;
            }

            GroundTile tile = coord.go.GetComponent<GroundTile>();
            if (tile == null) continue;

            // Remove any existing tower on this tile
            if (tile.tower != null)
            {
                tile.tower.DestroySelf();
                yield return null;
            }

            TowerType type = (TowerType)entry.towerType;
            tile.AddTowerToTile(type);

            if (tile.tower != null)
            {
                // Set directions before Start() completes
                if (entry.directions != null && entry.directions.Count > 0)
                    tile.tower.directions = new List<int>(entry.directions);

                tile.tower.isMuted = entry.isMuted;

                // Lobber distance
                if (tile.tower is LobberTower lob && entry.lobDistance > 0)
                {
                    lob.lobDistance = entry.lobDistance;
                    StartCoroutine(ApplyDeferredLobCache(lob));
                }

                // Buffer threshold
                if (tile.tower is BufferTower buf && entry.bufferThreshold > 0)
                    buf.UpdateBufferSize(entry.bufferThreshold);

                // Defer sample assignment to after Start() runs
                if (!string.IsNullOrEmpty(entry.sampleName))
                    StartCoroutine(ApplyDeferredSample(tile.tower, entry.sampleName));

                // Defer visual rotation for directional towers (Mono/Bouncer)
                if (entry.directions != null && entry.directions.Count > 0)
                    StartCoroutine(ApplyDeferredDirectionVisual(tile.tower, entry.directions[0]));

                // Defer mute visual
                if (entry.isMuted)
                    StartCoroutine(ApplyDeferredMuteVisual(tile.tower));
            }
        }

        // Wait one more frame so all towers' Start() methods
        // (which call InteractionMade) run while suppression is still active
        yield return null;

        Debug.Log($"[SaveManager] Rebuilt {towers.Count} towers from map data.");
        Tower.SuppressInteractions = false;
        OnMapLoaded?.Invoke();
    }

    private IEnumerator ApplyDeferredSample(Tower tower, string sampleName)
    {
        yield return null;
        if (tower != null && tower.towerUI != null && !string.IsNullOrEmpty(sampleName))
        {
            tower.towerUI.SetDropdown(sampleName);
            tower.towerUI.OnSampleSelected(sampleName);
        }
    }

    /// <summary>
    /// Applies the same rotation formula used by SelectionHandler when
    /// setting a Mono/Bouncer tower's facing direction.
    /// Formula: eulerAngles = (0, (direction + 2) * 60 + 150, 0)
    /// </summary>
    private IEnumerator ApplyDeferredDirectionVisual(Tower tower, int direction)
    {
        yield return null;
        if (tower != null && tower.visualModel != null)
        {
            tower.visualModel.transform.eulerAngles =
                new Vector3(0f, ((float)direction + 2f) * 60f + 150f, 0f);
        }
    }

    private IEnumerator ApplyDeferredMuteVisual(Tower tower)
    {
        yield return null;
        if (tower != null && tower.isMuted && tower.towerUI != null)
        {
            tower.towerUI.muteButtonImage.sprite = tower.towerUI.mutedSprite;
        }
    }

    /// <summary>
    /// Finds the Source tower's tempo slider and sets its value to match
    /// the loaded BPM. Deferred so towers have finished Start().
    /// </summary>
    private IEnumerator ApplyDeferredTempoSlider(double bpm)
    {
        yield return null;
        yield return null; // extra frame for tower UI init

        if (Tower.allTowers == null) yield break;

        foreach (Tower t in Tower.allTowers)
        {
            if (t == null || t.ownType != TowerType.Source) continue;
            if (t.towerUI == null) continue;

            // The tempo slider is inside tempoSliderContainer on the TowerUI
            Slider slider = t.towerUI.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                slider.value = (float)bpm;
            }
            break; // Only one source tower
        }
    }
    
    private IEnumerator ApplyDeferredLobCache(LobberTower lob)
    {
        yield return null;  // Wait for Start() to run
        yield return null;  // Extra frame for safety
        lob.CacheLobTargets();
    }

    // ══════════════════════════════════════════════════════════════════
    //  FILE HELPERS
    // ══════════════════════════════════════════════════════════════════

    private void SaveMapToFile(string mapName, string encoded)
    {
        if (!Directory.Exists(MapFolderPath))
            Directory.CreateDirectory(MapFolderPath);

        string path = Path.Combine(MapFolderPath, SanitizeFileName(mapName) + ".hexmap");
        try
        {
            File.WriteAllText(path, encoded);
            Debug.Log($"[SaveManager] Map saved to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save map file: {e.Message}");
        }
    }

    private string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    /// <summary>
    /// Returns a unique map name by appending (1), (2), etc. if needed.
    /// </summary>
    private string GetUniqueMapName(string baseName)
    {
        if (!Directory.Exists(MapFolderPath))
            return baseName;

        string candidate = baseName;
        int counter = 1;
        while (File.Exists(Path.Combine(MapFolderPath, SanitizeFileName(candidate) + ".hexmap")))
        {
            candidate = $"{baseName} ({counter})";
            counter++;
        }
        return candidate;
    }

    // ══════════════════════════════════════════════════════════════════
    //  SAMPLE NAME REVERSE LOOKUP
    // ══════════════════════════════════════════════════════════════════

    private string GetSampleNameFromClip(AudioClip clip)
    {
        if (clip == null || SampleLibrary.Instance == null) return null;
        foreach (var entry in SampleLibrary.Instance.samples)
        {
            if (entry.clip == clip) return entry.name;
        }
        return null;
    }
}