using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

/// <summary>
/// Manages an undo stack for tower actions (place, delete, move, replace).
/// Ctrl+Z pops the most recent action and reverses it.
/// Undo operations do NOT count toward progression stats.
/// Attach to a persistent GameObject.
/// </summary>
public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxUndoSteps = 50;

    private Stack<UndoAction> undoStack = new Stack<UndoAction>();
    private Stack<UndoAction> redoStack = new Stack<UndoAction>();

    // ── Pending state for Replace + directional towers ────────────────
    // When a tower is replaced with a Mono/Lobber, the old tower snapshot
    // is stored here until the direction/distance is confirmed.
    private TowerSnapshot pendingReplacedSnapshot;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (InputFocusGuard.IsInputFieldFocused()) return;

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
          || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
            && Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
          || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
            && Input.GetKeyDown(KeyCode.Y))
        {
            Redo();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  RECORDING ACTIONS (called by SelectionHandler)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Records a tower placement. Call after the tower is fully configured
    /// (direction set for Mono, distance set for Lobber).
    /// </summary>
    public void RecordPlace(GroundTile tile)
    {
        if (tile == null || tile.tower == null) return;

        var action = new UndoAction
        {
            type = UndoActionType.Place,
            created = SnapshotTower(tile)
        };

        // If a replace was pending, attach the old tower snapshot
        if (pendingReplacedSnapshot != null)
        {
            action.type = UndoActionType.Replace;
            action.destroyed = pendingReplacedSnapshot;
            pendingReplacedSnapshot = null;
        }

        PushAction(action);
    }

    /// <summary>
    /// Records a tower deletion. Call BEFORE the tower is destroyed.
    /// </summary>
    public void RecordDelete(GroundTile tile)
    {
        if (tile == null || tile.tower == null) return;

        PushAction(new UndoAction
        {
            type = UndoActionType.Delete,
            destroyed = SnapshotTower(tile)
        });
    }

    /// <summary>
    /// Records a tower move. Call BEFORE the tower is moved, passing
    /// the source tile (with the tower still on it) and the destination tile.
    /// </summary>
    public void RecordMove(GroundTile fromTile, GroundTile toTile)
    {
        if (fromTile == null || fromTile.tower == null || toTile == null) return;

        PushAction(new UndoAction
        {
            type = UndoActionType.Move,
            destroyed = SnapshotTower(fromTile),   // tower at old position
            moveDestQ = Mathf.RoundToInt(toTile.tileCoordinate.cube.x),
            moveDestR = Mathf.RoundToInt(toTile.tileCoordinate.cube.y)
        });
    }

    /// <summary>
    /// Stores a snapshot of the tower about to be replaced.
    /// The actual undo record is created when RecordPlace is called
    /// after the new tower's direction/distance is confirmed.
    /// </summary>
    public void StorePendingReplace(GroundTile tile)
    {
        if (tile == null || tile.tower == null)
        {
            pendingReplacedSnapshot = null;
            return;
        }
        pendingReplacedSnapshot = SnapshotTower(tile);
    }

    /// <summary>
    /// Clears the pending replace snapshot (e.g. if placement is cancelled).
    /// </summary>
    public void ClearPendingReplace()
    {
        pendingReplacedSnapshot = null;
    }

    /// <summary>
    /// Clears the entire undo history (e.g. after loading a map).
    /// </summary>
    public void ClearHistory()
    {
        undoStack.Clear();
        redoStack.Clear();
        pendingReplacedSnapshot = null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  UNDO EXECUTION
    // ══════════════════════════════════════════════════════════════════

    public void Undo()
    {
        if (undoStack.Count == 0) return;

        var action = undoStack.Pop();
        redoStack.Push(action);

        Tower.SuppressInteractions = true;

        switch (action.type)
        {
            case UndoActionType.Place:
                // Remove the placed tower
                DestroyTowerAtSnapshot(action.created);
                break;

            case UndoActionType.Delete:
                // Restore the deleted tower
                StartCoroutine(RestoreTower(action.destroyed));
                break;

            case UndoActionType.Move:
                // Remove tower at destination, restore at source
                DestroyTowerAtSnapshot(action.destroyed, useDestCoords: true,
                    destQ: action.moveDestQ, destR: action.moveDestR);
                StartCoroutine(RestoreTower(action.destroyed));
                break;

            case UndoActionType.Replace:
                // Remove the new tower, restore the old one
                DestroyTowerAtSnapshot(action.created);
                StartCoroutine(RestoreTower(action.destroyed));
                break;
        }

        // Clean up selection state
        SelectionHandler.HideTowerUIs();
        SelectionHandler.DeselectCurrent();

        // Defer turning off suppression so Start() runs under it
        StartCoroutine(EndSuppressionAfterFrames(2));
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;

        var action = redoStack.Pop();
        undoStack.Push(action);

        Tower.SuppressInteractions = true;

        switch (action.type)
        {
            case UndoActionType.Place:
                // Re-place the tower
                StartCoroutine(RestoreTower(action.created));
                break;

            case UndoActionType.Delete:
                // Re-delete the tower
                DestroyTowerAtSnapshot(action.destroyed);
                break;

            case UndoActionType.Move:
                // Re-move: destroy at source, restore at destination
                DestroyTowerAtSnapshot(action.destroyed);
                var destSnap = CloneSnapshotAt(action.destroyed, action.moveDestQ, action.moveDestR);
                StartCoroutine(RestoreTower(destSnap));
                break;

            case UndoActionType.Replace:
                // Re-replace: destroy old, restore new
                DestroyTowerAtSnapshot(action.destroyed);
                StartCoroutine(RestoreTower(action.created));
                break;
        }

        SelectionHandler.HideTowerUIs();
        SelectionHandler.DeselectCurrent();

        StartCoroutine(EndSuppressionAfterFrames(2));
    }

    private IEnumerator EndSuppressionAfterFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
            yield return null;
        Tower.SuppressInteractions = false;
    }

    // ══════════════════════════════════════════════════════════════════
    //  SNAPSHOT
    // ══════════════════════════════════════════════════════════════════

    private TowerSnapshot SnapshotTower(GroundTile tile)
    {
        Tower t = tile.tower;
        var snap = new TowerSnapshot();

        snap.q = Mathf.RoundToInt(tile.tileCoordinate.cube.x);
        snap.r = Mathf.RoundToInt(tile.tileCoordinate.cube.y);
        snap.towerType = t.ownType;
        snap.isMuted = t.isMuted;
        snap.directions = t.directions != null ? new List<int>(t.directions) : new List<int>();

        // Sample
        if (t.playbackClip != null && SampleLibrary.Instance != null)
        {
            foreach (var entry in SampleLibrary.Instance.samples)
            {
                if (entry.clip == t.playbackClip)
                {
                    snap.sampleName = entry.name;
                    break;
                }
            }
        }

        // Lobber distance
        snap.lobDistance = (t is LobberTower lob) ? lob.lobDistance : -1;

        // Buffer threshold
        snap.bufferThreshold = (t is BufferTower buf) ? buf.threshold : -1;

        // Visual rotation
        if (t.visualModel != null)
            snap.visualRotation = t.visualModel.transform.eulerAngles;

        return snap;
    }

    // ══════════════════════════════════════════════════════════════════
    //  DESTROY / RESTORE
    // ══════════════════════════════════════════════════════════════════

    private void DestroyTowerAtSnapshot(TowerSnapshot snap, bool useDestCoords = false,
        int destQ = 0, int destR = 0)
    {
        int q = useDestCoords ? destQ : snap.q;
        int r = useDestCoords ? destR : snap.r;
        Vector3 cube = new Vector3(q, r, -q - r);

        Coordinate coord = TileMapConstructor.allTiles?.GetCoordinate(cube);
        if (coord == null) return;

        GroundTile tile = coord.go.GetComponent<GroundTile>();
        if (tile == null || tile.tower == null) return;

        // Bypass normal DestroySelf to avoid double-counting
        ClearFieldController.OnClearField -= tile.tower.DestroySelf;
        tile.tower.towerUI.RemoveFromReference();
        Tower.allTowers.Remove(tile.tower);
        Destroy(tile.tower.gameObject);
        tile.tower = null;
    }

    private IEnumerator RestoreTower(TowerSnapshot snap)
    {
        Vector3 cube = new Vector3(snap.q, snap.r, -snap.q - snap.r);
        Coordinate coord = TileMapConstructor.allTiles?.GetCoordinate(cube);
        if (coord == null) yield break;

        GroundTile tile = coord.go.GetComponent<GroundTile>();
        if (tile == null) yield break;

        // If there's somehow a tower here already, remove it
        if (tile.tower != null)
        {
            DestroyTowerAtSnapshot(snap);
            yield return null;
        }

        tile.AddTowerToTile(snap.towerType);
        if (tile.tower == null) yield break;

        // Set state before Start() runs
        if (snap.directions != null && snap.directions.Count > 0)
            tile.tower.directions = new List<int>(snap.directions);

        tile.tower.isMuted = snap.isMuted;

        if (tile.tower is LobberTower lob && snap.lobDistance > 0)
            lob.lobDistance = snap.lobDistance;

        if (tile.tower is BufferTower buf && snap.bufferThreshold > 0)
            buf.UpdateBufferSize(snap.bufferThreshold);

        // Wait for Start() to run
        yield return null;
        // Wait one more frame for InitializeDropdown / SetSelfUI to fully settle
        yield return null;

        // Apply deferred state
        if (!string.IsNullOrEmpty(snap.sampleName) && tile.tower != null && tile.tower.towerUI != null)
        {
            tile.tower.towerUI.SetDropdown(snap.sampleName);
            tile.tower.towerUI.OnSampleSelected(snap.sampleName);
        }

        // Only apply saved rotation for Mono towers — they are the only type
        // with a player-set direction. Other towers (Splitter, Mirror, etc.)
        // have their rotation driven by animations and pulse direction.
        if (snap.towerType == TowerType.Mono && tile.tower != null && tile.tower.visualModel != null)
            tile.tower.visualModel.transform.eulerAngles = snap.visualRotation;

        if (snap.isMuted && tile.tower != null && tile.tower.towerUI != null)
            tile.tower.towerUI.muteButtonImage.sprite = tile.tower.towerUI.mutedSprite;

        GroundTile.NotifyTowerChangeMade();
    }

    // ══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════

    private void PushAction(UndoAction action)
    {
        undoStack.Push(action);
        redoStack.Clear(); // new action invalidates the redo timeline

        // Trim stack if over limit
        if (undoStack.Count > maxUndoSteps)
        {
            var temp = new Stack<UndoAction>();
            int count = 0;
            foreach (var a in undoStack)
            {
                if (count >= maxUndoSteps) break;
                temp.Push(a);
                count++;
            }
            undoStack.Clear();
            foreach (var a in temp)
                undoStack.Push(a);
        }
    }

    /// <summary>
    /// Creates a copy of a snapshot with overridden coordinates.
    /// Used for Move redo to place the tower at the destination.
    /// </summary>
    private TowerSnapshot CloneSnapshotAt(TowerSnapshot original, int q, int r)
    {
        return new TowerSnapshot
        {
            q = q,
            r = r,
            towerType = original.towerType,
            sampleName = original.sampleName,
            directions = original.directions != null ? new List<int>(original.directions) : new List<int>(),
            lobDistance = original.lobDistance,
            bufferThreshold = original.bufferThreshold,
            isMuted = original.isMuted,
            visualRotation = original.visualRotation
        };
    }
}

// ══════════════════════════════════════════════════════════════════
//  DATA TYPES
// ══════════════════════════════════════════════════════════════════

public enum UndoActionType
{
    Place,
    Delete,
    Move,
    Replace
}

[Serializable]
public class TowerSnapshot
{
    public int q;
    public int r;
    public TowerType towerType;
    public string sampleName;
    public List<int> directions;
    public int lobDistance;
    public int bufferThreshold;
    public bool isMuted;
    public Vector3 visualRotation;
}

public class UndoAction
{
    public UndoActionType type;
    public TowerSnapshot created;    // tower that was added (to remove on undo)
    public TowerSnapshot destroyed;  // tower that was removed (to restore on undo)
    public int moveDestQ;            // destination coords for Move actions
    public int moveDestR;
}