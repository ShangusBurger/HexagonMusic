using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CubeCoordinates;
using UnityEngine.EventSystems;

/// <summary>
/// Tool-based interaction handler. The active tool (set via ToolbarUI) determines
/// what happens when the player clicks/drags on the hex grid.
///
/// MouseState flow:
///   HandTool         – click tower → open TowerUI, click+drag tower → move it
///   PlaceTower       – click any hex → place selected tower type (replaces existing)
///   SetMonoDirection – after placing Mono, click to set direction → back to PlaceTower
///   SetLobberDistance – after placing Lobber, click to set distance → back to PlaceTower
///   DraggingTower    – actively dragging a tower from one hex to another
/// </summary>
public class SelectionHandler : MonoBehaviour
{
    // ── Mouse States ──────────────────────────────────────────────────
    public static MouseState currentMouseState = MouseState.HandTool;

    // ── Tile tracking ─────────────────────────────────────────────────
    public static GroundTile currentHoveredTile = null;
    public static GroundTile currentSelectedTile = null;
    public static SelectionHandler Instance;
    List<GroundTile> lowlightedTiles = new List<GroundTile>();

    // ── Events ────────────────────────────────────────────────────────
    public static event Action HideAllTowerUI;

    // ── Drag state ────────────────────────────────────────────────────
    private GroundTile dragSourceTile = null;
    private float mouseDownTime;
    private Vector2 mouseDownPos;
    private const float DragThreshold = 8f;
    private const float ClickTimeThreshold = 0.25f;

    // ── Tower placement ───────────────────────────────────────────────
    private TowerType activePlacementType;

    // ── Ghost preview ─────────────────────────────────────────────────
    [Header("Ghost Preview")]
    [SerializeField] private float ghostAlpha = 0.35f;
    private GameObject ghostPreview;
    private TowerType? ghostType = null;
    private const float TowerYOffset = 1f;

    // ── Drag visual state ─────────────────────────────────────────────
    private GameObject dragHiddenVisual = null;

    // ── References ────────────────────────────────────────────────────
    [SerializeField] private GameObject towerSelectCanvas; // legacy; safe to remove

    // ══════════════════════════════════════════════════════════════════
    // Lifecycle
    // ══════════════════════════════════════════════════════════════════

    void Awake()
    {
        currentMouseState = MouseState.HandTool;
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        ToolbarUI.OnToolChanged += OnToolChanged;
        ToolbarUI.OnEraseToolSelected += OnEraseToolSelected;
    }

    void OnDestroy()
    {
        ToolbarUI.OnToolChanged -= OnToolChanged;
        ToolbarUI.OnEraseToolSelected -= OnEraseToolSelected;
        DestroyGhost();
    }

    void Update()
    {
        switch (currentMouseState)
        {
            case MouseState.HandTool:
                HandleMouseHover();
                HandleHandToolInput();
                break;
            case MouseState.PlaceTower:
                HandleMouseHover();
                UpdateGhostToHoveredTile();
                HandlePlaceTowerInput();
                break;
            case MouseState.SetMonoDirection:
                HandleMonoTowerHover();
                HandleMonoTowerClick();
                break;
            case MouseState.SetLobberDistance:
                HandleLobberTowerHover();
                HandleLobberTowerClick();
                break;
            case MouseState.DraggingTower:
                HandleMouseHover();
                UpdateGhostToHoveredTile();
                HandleDragUpdate();
                break;
            case MouseState.EraseTool:
                HandleMouseHover();
                HandleEraseInput();
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Tool changed callback (from ToolbarUI)
    // ══════════════════════════════════════════════════════════════════

    void OnToolChanged(TowerType? type)
    {
        CancelSecondaryState();

        if (type == null)
        {
            currentMouseState = MouseState.HandTool;
            DestroyGhost();
        }
        else
        {
            currentMouseState = MouseState.PlaceTower;
            activePlacementType = type.Value;
            CreateGhostFromPrefab(type.Value);
        }

        DeselectCurrent();
        HideAllTowerUI?.Invoke();
    }

    void OnEraseToolSelected()
    {
        CancelSecondaryState();
        DestroyGhost();
        DeselectCurrent();
        HideAllTowerUI?.Invoke();
        currentMouseState = MouseState.EraseTool;
    }

    void CancelSecondaryState()
    {
        SelectionUtility.DeselectListOfTiles(lowlightedTiles);
        lowlightedTiles.Clear();
        if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
        {
            currentHoveredTile.Deselect();
            currentHoveredTile = null;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Ghost preview — shared by tool placement AND drag-drop
    // ══════════════════════════════════════════════════════════════════

    void CreateGhostFromPrefab(TowerType type)
    {
        DestroyGhost();
        GameObject prefab = GetTowerPrefab(type);
        if (prefab == null) return;

        ghostPreview = Instantiate(prefab);
        ghostType = type;
        StripGhostNonVisuals(ghostPreview);
        SetGhostTransparency(ghostPreview);
        ghostPreview.SetActive(false);
    }

    void CreateGhostFromExistingTower(Tower tower)
    {
        DestroyGhost();
        GameObject prefab = GetTowerPrefab(tower.ownType);
        if (prefab == null) return;

        ghostPreview = Instantiate(prefab);
        ghostType = tower.ownType;
        StripGhostNonVisuals(ghostPreview);

        // Copy the visual rotation from the existing tower
        if (tower.visualModel != null)
        {
            MeshRenderer ghostMR = ghostPreview.GetComponentInChildren<MeshRenderer>();
            if (ghostMR != null)
                ghostMR.transform.rotation = tower.visualModel.transform.rotation;
        }

        SetGhostTransparency(ghostPreview);
        ghostPreview.SetActive(false);
    }

    void DestroyGhost()
    {
        if (ghostPreview != null)
        {
            Destroy(ghostPreview);
            ghostPreview = null;
            ghostType = null;
        }
    }

    void UpdateGhostToHoveredTile()
    {
        if (ghostPreview == null) return;

        if (currentHoveredTile != null && !IsPointerOverUI())
        {
            ghostPreview.SetActive(true);
            ghostPreview.transform.position = currentHoveredTile.transform.position + Vector3.up * TowerYOffset;
        }
        else
        {
            ghostPreview.SetActive(false);
        }
    }

    void SetGhostVisible(bool visible)
    {
        if (ghostPreview != null)
            ghostPreview.SetActive(visible);
    }

    // ── Ghost utility ─────────────────────────────────────────────────

    void StripGhostNonVisuals(GameObject ghost)
    {
        foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>(true))
            Destroy(mb);
        foreach (var col in ghost.GetComponentsInChildren<Collider>(true))
            Destroy(col);
        foreach (var audio in ghost.GetComponentsInChildren<AudioSource>(true))
            Destroy(audio);
        foreach (var anim in ghost.GetComponentsInChildren<Animator>(true))
            Destroy(anim);
        foreach (var canvas in ghost.GetComponentsInChildren<Canvas>(true))
            Destroy(canvas.gameObject);
    }

    void SetGhostTransparency(GameObject ghost)
    {
        foreach (var renderer in ghost.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var mat in renderer.materials)
                SetMaterialTransparent(mat, ghostAlpha);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    void SetMaterialTransparent(Material mat, float alpha)
    {
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        Color c = mat.color;
        c.a = alpha;
        mat.color = c;
    }

    GameObject GetTowerPrefab(TowerType type)
    {
        if (TileMapConstructor.Instance == null) return null;
        switch (type)
        {
            case TowerType.Source:   return TileMapConstructor.Instance.sourceTowerPrefab;
            case TowerType.Mono:     return TileMapConstructor.Instance.monoTowerPrefab;
            case TowerType.Splitter: return TileMapConstructor.Instance.splitterTowerPrefab;
            case TowerType.Sink:     return TileMapConstructor.Instance.sinkTowerPrefab;
            case TowerType.Lobber:   return TileMapConstructor.Instance.lobberTowerPrefab;
            case TowerType.Sprayer:  return TileMapConstructor.Instance.sprayerTowerPrefab;
            case TowerType.Buffer:   return TileMapConstructor.Instance.bufferTowerPrefab;
            case TowerType.Switcher: return TileMapConstructor.Instance.switcherTowerPrefab;
            case TowerType.Passer:  return TileMapConstructor.Instance.passerTowerPrefab;
            default: return null;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Sample name lookup — reverse-lookup from AudioClip
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds the sample name in SampleLibrary that matches the given AudioClip.
    /// Returns null if not found.
    /// </summary>
    string GetSampleNameFromClip(AudioClip clip)
    {
        if (clip == null || SampleLibrary.Instance == null) return null;
        foreach (var entry in SampleLibrary.Instance.samples)
        {
            if (entry.clip == clip) return entry.name;
        }
        return null;
    }

    /// <summary>
    /// Waits one frame (so Tower.Start() has run and TowerUI is initialized),
    /// then applies the saved sample name via TowerUI so both the clip AND
    /// the dropdown stay in sync.
    /// </summary>
    IEnumerator ApplyDeferredSample(Tower tower, string sampleName)
    {
        yield return null; // wait for Start() + InitializeDropdown + SetSelfUI to finish
        if (tower != null && tower.towerUI != null && !string.IsNullOrEmpty(sampleName))
        {
            tower.towerUI.SetDropdown(sampleName);
            tower.towerUI.OnSampleSelected(sampleName);
        }
    }

    /// <summary>
    /// Waits one frame then applies visual rotation (since visualModel
    /// isn't assigned until Tower.Start() runs).
    /// </summary>
    IEnumerator ApplyDeferredRotation(Tower tower, Quaternion rotation)
    {
        yield return null;
        if (tower != null && tower.visualModel != null)
            tower.visualModel.transform.rotation = rotation;
    }

    // ══════════════════════════════════════════════════════════════════
    // Drag visual helpers — hide/show the real tower model
    // ══════════════════════════════════════════════════════════════════

    void HideDragSourceVisual(GroundTile tile)
    {
        if (tile == null || tile.tower == null || tile.tower.visualModel == null) return;
        dragHiddenVisual = tile.tower.visualModel;
        dragHiddenVisual.SetActive(false);
    }

    void RestoreDragSourceVisual()
    {
        if (dragHiddenVisual != null)
        {
            dragHiddenVisual.SetActive(true);
            dragHiddenVisual = null;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Shared helpers
    // ══════════════════════════════════════════════════════════════════

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    GroundTile RaycastToTile()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
            return hit.collider.GetComponent<GroundTile>();
        return null;
    }

    bool RaycastHitsNothing()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        return !Physics.Raycast(ray, out _);
    }

    void HandleMouseHover()
    {
        if (IsPointerOverUI())
        {
            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Deselect();
                currentHoveredTile = null;
            }
            return;
        }

        GroundTile tile = RaycastToTile();

        if (tile != null)
        {
            if (currentHoveredTile != null && currentHoveredTile != tile && currentHoveredTile != currentSelectedTile)
                currentHoveredTile.Deselect();

            currentHoveredTile = tile;
            if (currentHoveredTile != currentSelectedTile)
                currentHoveredTile.Highlight();
        }
        else
        {
            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Deselect();
                currentHoveredTile = null;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // HAND TOOL  (click to open TowerUI, drag to move towers)
    // ══════════════════════════════════════════════════════════════════

    void HandleHandToolInput()
    {
        if (IsPointerOverUI()) return;

        // --- Mouse Down: start potential click or drag ---
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseDownTime = Time.unscaledTime;
            mouseDownPos = Mouse.current.position.ReadValue();

            GroundTile tile = RaycastToTile();
            // Source towers cannot be dragged
            if (tile != null && tile.tower != null && tile.tower.ownType != TowerType.Source)
                dragSourceTile = tile;
            else
                dragSourceTile = null;
        }

        // --- Mouse Held: detect drag ---
        if (Mouse.current.leftButton.isPressed && dragSourceTile != null)
        {
            Vector2 delta = Mouse.current.position.ReadValue() - mouseDownPos;
            if (delta.magnitude > DragThreshold)
            {
                currentMouseState = MouseState.DraggingTower;
                currentSelectedTile = dragSourceTile;
                currentSelectedTile.Select();
                HideAllTowerUI?.Invoke();

                CreateGhostFromExistingTower(dragSourceTile.tower);
                HideDragSourceVisual(dragSourceTile);
                return;
            }
        }

        // --- Mouse Up: it was a click (not a drag) ---
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            GroundTile tile = RaycastToTile();

            if (tile != null)
            {
                if (tile.tower != null)
                {
                    if (currentSelectedTile != null && currentSelectedTile != tile)
                        DeselectCurrent();

                    currentSelectedTile = tile;
                    currentSelectedTile.Select();
                    HideAllTowerUI?.Invoke();
                    OpenTowerUI(tile);
                }
                else
                {
                    DeselectCurrent();
                    HideAllTowerUI?.Invoke();
                }
            }
            else if (RaycastHitsNothing())
            {
                DeselectCurrent();
                HideAllTowerUI?.Invoke();
            }

            dragSourceTile = null;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // DRAGGING TOWER  (move tower from one hex to another)
    // ══════════════════════════════════════════════════════════════════

    void HandleDragUpdate()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            GroundTile dropTile = RaycastToTile();

            bool validDrop = dropTile != null
                && dropTile != dragSourceTile
                && dropTile.tower == null;

            if (validDrop)
            {
                RestoreDragSourceVisual();
                MoveTower(dragSourceTile, dropTile);
            }
            else
            {
                // Cancelled — show original tower model again
                RestoreDragSourceVisual();
            }

            DestroyGhost();
            DeselectCurrent();
            dragSourceTile = null;
            dragHiddenVisual = null;
            currentMouseState = MouseState.HandTool;
        }
    }

    void MoveTower(GroundTile from, GroundTile to)
    {
        if (from.tower == null) return;

        // ── Cache ALL state ──
        string sampleName = GetSampleNameFromClip(from.tower.playbackClip);
        TowerType type = from.tower.ownType;
        List<int> dirs = new List<int>(from.tower.directions);
        bool muted = from.tower.isMuted;

        int lobDist = -1;
        if (from.tower is LobberTower lt)
            lobDist = lt.lobDistance;

        Quaternion visualRotation = Quaternion.identity;
        if (from.tower.visualModel != null)
            visualRotation = from.tower.visualModel.transform.rotation;

        // ── Remove old tower ──
        ClearFieldController.OnClearField -= from.tower.DestroySelf;
        from.tower.towerUI.RemoveFromReference();
        Tower.allTowers.Remove(from.tower);
        Destroy(from.tower.gameObject);
        from.tower = null;
        from.Deselect();

        // ── Place new tower at destination ──
        to.AddTowerToTile(type);

        if (to.tower != null)
        {
            // Set directions BEFORE Start() runs (directions fix)
            to.tower.directions = dirs;
            to.tower.isMuted = muted;

            if (to.tower is LobberTower newLob && lobDist > 0)
                newLob.lobDistance = lobDist;

            // Defer sample + rotation to AFTER Start() runs
            if (!string.IsNullOrEmpty(sampleName))
                StartCoroutine(ApplyDeferredSample(to.tower, sampleName));
            StartCoroutine(ApplyDeferredRotation(to.tower, visualRotation));
        }

        GroundTile.NotifyTowerChangeMade();
    }

    // ══════════════════════════════════════════════════════════════════
    // ERASE TOOL  (click any tower to delete it)
    // ══════════════════════════════════════════════════════════════════

    void HandleEraseInput()
    {
        if (IsPointerOverUI()) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            GroundTile tile = RaycastToTile();
            if (tile == null || tile.tower == null) return;

            // Use the tower's existing DestroySelf which handles cleanup,
            // interaction tracking, and event unsubscription.
            tile.tower.DestroySelf();

            DeselectCurrent();
            HideAllTowerUI?.Invoke();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // PLACE TOWER TOOL  (click any hex to place selected tower type)
    // ══════════════════════════════════════════════════════════════════

    void HandlePlaceTowerInput()
    {
        if (IsPointerOverUI()) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            GroundTile tile = RaycastToTile();
            if (tile == null) return;

            if (tile.tower != null)
                ReplaceTowerOnTile(tile, activePlacementType);
            else
                PlaceTowerOnTile(tile, activePlacementType);
        }
    }

    void PlaceTowerOnTile(GroundTile tile, TowerType type)
    {
        DeselectCurrent();
        HideAllTowerUI?.Invoke();

        currentSelectedTile = tile;
        tile.AddTowerToTile(type);

        if (type == TowerType.Mono)
        {
            currentMouseState = MouseState.SetMonoDirection;
            SetGhostVisible(false);
        }
        else if (type == TowerType.Lobber)
        {
            currentMouseState = MouseState.SetLobberDistance;
            SetGhostVisible(false);
        }
    }

    void ReplaceTowerOnTile(GroundTile tile, TowerType newType)
    {
        // Save sample name from existing tower (before destroying it)
        string savedSampleName = GetSampleNameFromClip(tile.tower.playbackClip);

        // Remove old tower
        ClearFieldController.OnClearField -= tile.tower.DestroySelf;
        tile.tower.towerUI.RemoveFromReference();
        Tower.allTowers.Remove(tile.tower);
        Destroy(tile.tower.gameObject);
        tile.tower = null;

        DeselectCurrent();
        HideAllTowerUI?.Invoke();

        currentSelectedTile = tile;
        tile.AddTowerToTile(newType);

        // Defer sample restoration to after Start() + SetSelfUI has run
        if (tile.tower != null && !string.IsNullOrEmpty(savedSampleName))
            StartCoroutine(ApplyDeferredSample(tile.tower, savedSampleName));

        if (newType == TowerType.Mono)
        {
            currentMouseState = MouseState.SetMonoDirection;
            SetGhostVisible(false);
        }
        else if (newType == TowerType.Lobber)
        {
            currentMouseState = MouseState.SetLobberDistance;
            SetGhostVisible(false);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // MONO DIRECTION  (returns to PlaceTower + re-shows ghost)
    // ══════════════════════════════════════════════════════════════════

    void HandleMonoTowerHover()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
        {
            GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();

            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
                currentHoveredTile.Deselect();

            SelectionUtility.DeselectListOfTiles(lowlightedTiles);
            lowlightedTiles.Clear();

            currentHoveredTile = collidedTile;
            if (currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Highlight();
                int bestDir = ExtraCubeUtility.GetBestDirectionToTile(currentSelectedTile.tileCoordinate, currentHoveredTile.tileCoordinate);
                Coordinate targetCoord = GetFurthestCoordinateInDirection(currentSelectedTile.tileCoordinate, bestDir);
                List<Coordinate> coordsBetween = Coordinates.Instance.GetLine(currentSelectedTile.tileCoordinate, targetCoord);

                foreach (Coordinate coord in coordsBetween)
                {
                    GroundTile coordTile = coord.go.GetComponent<GroundTile>();
                    if (coordTile != currentSelectedTile && coordTile != null)
                    {
                        lowlightedTiles.Add(coordTile);
                        coordTile.Lowlight();
                    }
                }

                if (currentSelectedTile.tower?.visualModel != null)
                    currentSelectedTile.tower.visualModel.transform.eulerAngles = new Vector3(0f, ((float)bestDir + 2f) * 60f + 150f, 0f);
            }
        }
        else
        {
            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Deselect();
                currentHoveredTile = null;
            }
            SelectionUtility.DeselectListOfTiles(lowlightedTiles);
        }
    }

    void HandleMonoTowerClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null && !IsPointerOverUI())
            {
                GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();

                if (currentSelectedTile != null && currentSelectedTile.tower != null)
                {
                    int direction = ExtraCubeUtility.GetBestDirectionToTile(currentSelectedTile.tileCoordinate, collidedTile.tileCoordinate);
                    currentSelectedTile.tower.SetDirection(direction);
                }

                SelectionUtility.DeselectListOfTiles(lowlightedTiles);
                lowlightedTiles.Clear();
                if (currentHoveredTile != null) { currentHoveredTile.Deselect(); currentHoveredTile = null; }
                DeselectCurrent();

                currentMouseState = MouseState.PlaceTower;
                SetGhostVisible(true);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // LOBBER DISTANCE  (returns to PlaceTower + re-shows ghost)
    // ══════════════════════════════════════════════════════════════════

    void HandleLobberTowerHover()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
        {
            GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();
            LobberTower lobberTower = currentSelectedTile.tower as LobberTower;

            float mouseDistance = Cubes.GetDistanceBetweenTwoCubes(currentSelectedTile.tileCoordinate.cube, collidedTile.tileCoordinate.cube);
            int targetDistance = Mathf.Clamp(Mathf.RoundToInt(mouseDistance), lobberTower.minLobDistance, lobberTower.maxLobDistance);
            List<Coordinate> ringTiles = GetLobRingAtDistance(targetDistance, currentSelectedTile);

            if (currentHoveredTile != collidedTile)
            {
                if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
                    currentHoveredTile.Deselect();

                SelectionUtility.DeselectListOfTiles(lowlightedTiles);
                lowlightedTiles.Clear();

                currentHoveredTile = collidedTile;
                if (currentHoveredTile != currentSelectedTile)
                    currentHoveredTile.Highlight();

                foreach (Coordinate coord in ringTiles)
                {
                    GroundTile tile = coord.go.GetComponent<GroundTile>();
                    if (tile != currentSelectedTile && tile != null)
                    {
                        lowlightedTiles.Add(tile);
                        tile.Lowlight();
                    }
                }
            }
        }
        else
        {
            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Deselect();
                currentHoveredTile = null;
            }
            SelectionUtility.DeselectListOfTiles(lowlightedTiles);
        }
    }

    void HandleLobberTowerClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI())
        {
            if (currentSelectedTile != null && currentSelectedTile.tower != null && lowlightedTiles.Count > 0)
            {
                LobberTower lobberTower = currentSelectedTile.tower as LobberTower;
                if (lobberTower != null && currentSelectedTile.tileCoordinate != null)
                {
                    GroundTile anyRingTile = lowlightedTiles[0];
                    int distance = (int)Cubes.GetDistanceBetweenTwoCubes(currentSelectedTile.tileCoordinate.cube, anyRingTile.tileCoordinate.cube);
                    lobberTower.lobDistance = distance;
                }

                foreach (GroundTile tile in lowlightedTiles)
                    tile.Deselect();
                lowlightedTiles.Clear();

                if (currentHoveredTile != null) { currentHoveredTile.Deselect(); currentHoveredTile = null; }
                DeselectCurrent();

                currentMouseState = MouseState.PlaceTower;
                SetGhostVisible(true);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Utility
    // ══════════════════════════════════════════════════════════════════

    List<Coordinate> GetLobRingAtDistance(int distance, GroundTile centerTile)
    {
        List<Coordinate> ringTiles = new List<Coordinate>();
        if (centerTile == null || centerTile.tileCoordinate == null) return ringTiles;

        for (int dir = 0; dir < 6; dir++)
        {
            Coordinate coord = Coordinates.Instance.GetNeighbor(centerTile.tileCoordinate, dir, distance);
            if (coord != null)
                ringTiles.Add(coord);
        }
        return ringTiles;
    }

    Coordinate GetFurthestCoordinateInDirection(Coordinate origin, int direction)
    {
        Coordinate furthest = null;
        int distance = 1;
        while (distance < 100)
        {
            Coordinate next = Coordinates.Instance.GetNeighbor(origin, direction, distance);
            if (next == null) break;
            furthest = next;
            distance++;
        }
        return furthest;
    }

    // ── Public API ────────────────────────────────────────────────────

    public static void HideTowerUIs()
    {
        HideAllTowerUI?.Invoke();
    }

    public static void OpenTowerUI(GroundTile tile)
    {
        tile.tower.towerUI.gameObject.SetActive(true);
    }

    public static void DeselectCurrent()
    {
        if (currentSelectedTile != null)
        {
            currentSelectedTile.Deselect();
            currentSelectedTile = null;
        }
    }

    // Legacy stub
    public static void OfferTowerPlacement(GroundTile tile) { }
}

public enum MouseState
{
    HandTool,
    PlaceTower,
    SetMonoDirection,
    SetLobberDistance,
    DraggingTower,
    EraseTool
}