using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CubeCoordinates;
using UnityEngine.EventSystems;

public class SelectionHandler : MonoBehaviour
{
    //Mouse States
    public static MouseState currentMouseState = MouseState.Free;

    //Handling Tile Selection
    public static GroundTile currentHoveredTile = null;
    public static GroundTile currentSelectedTile = null;
    public static SelectionHandler Instance;
    List<GroundTile> lowlightedTiles = new List<GroundTile>();

    public static event Action HideAllTowerUI;

    //Overlays
    [SerializeField] private GameObject towerSelectCanvas;

    void Awake()
    {
        currentMouseState = MouseState.Free;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        switch (currentMouseState)
        {
            case MouseState.Free:
                HandleMouseHover();
                HandleMouseClick();
                break;
            case MouseState.SetMonoTower:
                HandleMonoTowerHover();
                HandleMonoTowerClick();
                break;
            case MouseState.SetLobberTower:
                HandleLobberTowerHover();
                HandleLobberTowerClick();
                break;
        }
        
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
    
    void HandleMouseHover()
    {
        // Skip hover handling if over UI
        if (IsPointerOverUI())
        {
            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Deselect();
                currentHoveredTile = null;
            }
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        
        // Handles hovering over hex tiles only
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
        {
            GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();

            //Only update if we're hovering over a different tile
            if (collidedTile != currentHoveredTile)
            {
                // Remove highlight from previous tile
                if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Deselect();
                }

                // Highlight new tile (only if it's not selected)
                currentHoveredTile = collidedTile;
                if (currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Highlight();
                }
            }
        }
        else
        {
            // Mouse is not over any tile, remove hover highlight
            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Deselect();
                currentHoveredTile = null;
            }
        }
    }

    // Handles clicking on tiles to select them
    void HandleMouseClick()
    {
        if (IsPointerOverUI()) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
            {
                GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();
                if (collidedTile != currentSelectedTile)
                {
                    DeselectCurrent();

                    // Select new tile
                    currentSelectedTile = collidedTile;
                    currentSelectedTile.Select();
                }
                else
                {
                    DeselectCurrent();
                    
                    TowerSelection.Instance.gameObject.SetActive(false);
                    HideAllTowerUI?.Invoke();
                }
                

            }
            else if (!Physics.Raycast(ray, out hit))
            {
                // Clicked outside any tile, hide tower selection canvas
                if (TowerSelection.Instance.gameObject.activeSelf && currentSelectedTile != null)
                {
                    TowerSelection.Instance.gameObject.SetActive(false);
                    HideAllTowerUI?.Invoke();
                    DeselectCurrent();
                }
                
            }
        }
    }

    public static void HideTowerUIs()
    {
        HideAllTowerUI?.Invoke();
    }

    // Opens the tower selection canvas at the tile's position. Buttons continue the UI.
    public static void OfferTowerPlacement(GroundTile tile)
    {
        HideAllTowerUI?.Invoke();
        Instance.towerSelectCanvas.SetActive(true);
        Instance.towerSelectCanvas.transform.position = new Vector3(tile.transform.position.x, Instance.towerSelectCanvas.transform.position.y, tile.transform.position.z);
        Instance.towerSelectCanvas.GetComponent<TowerSelection>().SetTargetTile(tile);
    }

    public static void OpenTowerUI(GroundTile tile)
    {
        Instance.towerSelectCanvas.SetActive(false);
        tile.tower.towerUI.gameObject.SetActive(true);
    }

    // Handles hovering when setting direction for Mono Tower
    void HandleMonoTowerHover()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // Handles hovering over hex tiles only
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
        {
            GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();

            //Only update if we're hovering over a different tile
            if (collidedTile != currentHoveredTile)
            {
                // Remove highlight from previous tile and clear all lowlighted tiles
                if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Deselect();
                }
                
                SelectionUtility.DeselectListOfTiles(lowlightedTiles);
                lowlightedTiles.Clear();

                // Highlight new tile (only if it's not selected)
                currentHoveredTile = collidedTile;
                if (currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Highlight();
                    int bestDirection = ExtraCubeUtility.GetBestDirectionToTile(currentSelectedTile.tileCoordinate, currentHoveredTile.tileCoordinate);
                    Coordinate targetCoord = GetFurthestCoordinateInDirection(currentSelectedTile.tileCoordinate, bestDirection);
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
                }
            }
        }
        else
        {
            // Mouse is not over any tile, remove hover highlight
            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Deselect();
                currentHoveredTile = null;
            }
            SelectionUtility.DeselectListOfTiles(lowlightedTiles);
        }
    }

    Coordinate GetFurthestCoordinateInDirection(Coordinate origin, int direction)
    {
        Coordinate furthest = null;
        int distance = 1;
        
        // Keep checking neighbors in this direction until we run out of valid tiles
        while (distance < 100) // Safety limit
        {
            Coordinate next = Coordinates.Instance.GetNeighbor(origin, direction, distance);
            if (next == null)
            {
                break;
            }
            furthest = next;
            distance++;
        }
        
        return furthest;
    }
    
    // Handles clicking when setting direction for Mono Tower
    void HandleMonoTowerClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
            {
                GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();
                
                // Store the direction in the tower
                if (currentSelectedTile != null && currentSelectedTile.tower != null)
                {
                    int direction = ExtraCubeUtility.GetBestDirectionToTile(currentSelectedTile.tileCoordinate, collidedTile.tileCoordinate);
                    currentSelectedTile.tower.SetDirection(direction);
                }
                
                // Clear all lowlighted tiles
                SelectionUtility.DeselectListOfTiles(lowlightedTiles);
                lowlightedTiles.Clear();
                
                // Deselect hovered tile
                if (currentHoveredTile != null)
                {
                    currentHoveredTile.Deselect();
                    currentHoveredTile = null;
                }
                OpenTowerUI(currentSelectedTile);
                DeselectCurrent();
                
                // Return to free mode
                currentMouseState = MouseState.Free;
            }
        }
    }

    void HandleLobberTowerHover()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
        {
            GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();

            // Calculate the distance from tower to mouse
            int targetDistance;
            List<Coordinate> ringTiles = new List<Coordinate>();
            LobberTower lobberTower = currentSelectedTile.tower as LobberTower;

            // Get distance to mouse position
            float mouseDistance = Cubes.GetDistanceBetweenTwoCubes(currentSelectedTile.tileCoordinate.cube, collidedTile.tileCoordinate.cube);
            
            // Clamp to valid range
            targetDistance = Mathf.Clamp(Mathf.RoundToInt(mouseDistance), lobberTower.minLobDistance, lobberTower.maxLobDistance);
            
            // Get all tiles at this distance (the ring)
            ringTiles = GetLobRingAtDistance(targetDistance, currentSelectedTile);

            if (currentHoveredTile != collidedTile)
            {
                 // Remove highlight from previous tile and clear all lowlighted tiles
                if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Deselect();
                }
                
                SelectionUtility.DeselectListOfTiles(lowlightedTiles);
                lowlightedTiles.Clear();

                // Highlight new tile (only if it's not selected)
                currentHoveredTile = collidedTile;
                if (currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Highlight();
                }

                // Find the tile closest to the mouse in the ring
                GroundTile closestTile = null;
                float closestDist = float.MaxValue;
                
                foreach (Coordinate coord in ringTiles)
                { 
                    float dist = Cubes.GetDistanceBetweenTwoCubes(coord.cube, collidedTile.tileCoordinate.cube);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestTile = coord.go.GetComponent<GroundTile>();
                    }
                }
                
                foreach (GroundTile tile in lowlightedTiles)
                {
                    tile.Deselect();
                }
                lowlightedTiles.Clear();

                // Highlight all tiles in the ring
                foreach (Coordinate coord in ringTiles)
                {
                    GroundTile ringTile = coord.go.GetComponent<GroundTile>();
                    if (ringTile != null && ringTile != currentSelectedTile)
                    {
                        lowlightedTiles.Add(ringTile);
                        ringTile.Lowlight();
                    }
                }
                
                
            }
        }
        else
        {
            // Mouse is not over any tile, remove all highlights
            if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
            {
                currentHoveredTile.Deselect();
            }
            foreach (GroundTile tile in lowlightedTiles)
            {
                tile.Deselect();
            }
            lowlightedTiles.Clear();
        }
    }

    // Get all tiles at a specific distance in all 6 directions (forms a ring)
    public List<Coordinate> GetLobRingAtDistance(int distance, GroundTile centerTile)
    {
        List<Coordinate> ringTiles = new List<Coordinate>();
        
        if (centerTile == null || centerTile.tileCoordinate == null)
            return ringTiles;


        // Check each of the 6 hex directions at this distance
        for (int dir = 0; dir < 6; dir++)
        {
            Coordinate coord = Coordinates.Instance.GetNeighbor(centerTile.tileCoordinate, dir, distance);
            if (coord != null)
            {
                ringTiles.Add(coord);
            }
        }

        return ringTiles;
    }

    void HandleLobberTowerClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Set the lob distance based on the ring that's currently displayed
            if (currentSelectedTile != null && currentSelectedTile.tower != null && lowlightedTiles.Count > 0)
            {
                LobberTower lobberTower = currentSelectedTile.tower as LobberTower;
                if (lobberTower != null && currentSelectedTile.tileCoordinate != null)
                {
                    // Calculate distance from any tile in the lowlighted ring
                    GroundTile anyRingTile = lowlightedTiles[0];
                    int distance = (int)Cubes.GetDistanceBetweenTwoCubes(currentSelectedTile.tileCoordinate.cube, anyRingTile.tileCoordinate.cube);
                    lobberTower.lobDistance = distance;
                }
                
                // Clear all lowlighted tiles
                foreach (GroundTile tile in lowlightedTiles)
                {
                    tile.Deselect();
                }
                lowlightedTiles.Clear();
                
                // Deselect hovered tile
                if (currentHoveredTile != null)
                {
                    currentHoveredTile.Deselect();
                    currentHoveredTile = null;
                }
                OpenTowerUI(currentSelectedTile);
                DeselectCurrent();
                
                // Return to free mode
                currentMouseState = MouseState.Free;
            }
        }
    }

    // Deselect current tile
    public static void DeselectCurrent()
    {
        if (currentSelectedTile != null)
        {
            currentSelectedTile.Deselect();
            currentSelectedTile = null;
        }
    }
}

public enum MouseState
{
    Free,
    SetMonoTower,
    SetLobberTower
}
