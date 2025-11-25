using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CubeCoordinates;
using UnityEngine.UI;

public class SelectionHandler : MonoBehaviour
{
    //Mouse States
    public static MouseState currentMouseState = MouseState.Free;

    //Handling Tile Selection
    public static GroundTile currentHoveredTile = null;
    public static GroundTile currentSelectedTile = null;
    [SerializeField] private GameObject towerSelectCanvas;
    [SerializeField] private GameObject towerUICanvas;
    public static SelectionHandler Instance;
    List<GroundTile> lowlightedTiles = new List<GroundTile>();

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
    
    void HandleMouseHover()
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
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GroundTile>() != null)
            {
                GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();
                if (collidedTile != currentSelectedTile)
                {
                    // Deselect previous tile
                    if (currentSelectedTile != null)
                    {
                        currentSelectedTile.Deselect();
                    }

                    // Select new tile
                    currentSelectedTile = collidedTile;
                }
                
                currentSelectedTile.Select();
            }
            else if (!Physics.Raycast(ray, out hit))
            {
                // Clicked outside any tile, hide tower selection canvas
                if (TowerSelection.Instance.gameObject.activeSelf && currentSelectedTile != null)
                {
                    TowerSelection.Instance.gameObject.SetActive(false);
                    TowerUI.Instance.gameObject.SetActive(false);
                    currentSelectedTile.Deselect();
                }
                
            }
        }
    }

    // Opens the tower selection canvas at the tile's position. Buttons continue the UI.
    public static void OfferTowerPlacement(GroundTile tile)
    {
        Instance.towerUICanvas.SetActive(false);
        Instance.towerSelectCanvas.SetActive(true);
        Instance.towerSelectCanvas.transform.position = new Vector3(tile.transform.position.x, Instance.towerSelectCanvas.transform.position.y, tile.transform.position.z);
        Instance.towerSelectCanvas.GetComponent<TowerSelection>().SetTargetTile(tile);
    }

    public static void OpenTowerUI(GroundTile tile)
    {
        Instance.towerSelectCanvas.SetActive(false);
        Instance.towerUICanvas.SetActive(true);
        Instance.towerUICanvas.transform.position = new Vector3(tile.transform.position.x, Instance.towerSelectCanvas.transform.position.y, tile.transform.position.z);
        Instance.towerUICanvas.GetComponent<TowerUI>().SetTargetTile(tile);
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
                
                // Deselect current tile
                if (currentSelectedTile != null)
                {
                    currentSelectedTile.Deselect();
                    currentSelectedTile = null;
                }
                
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

            // Find the tile that best matches the direction AND distance toward the mouse
            GroundTile targetTile = null;
            LobberTower lobberTower = currentSelectedTile.tower as LobberTower;
            if (lobberTower != null)
            {
                // Get the best direction toward the hovered tile
                int bestDirection = ExtraCubeUtility.GetBestDirectionToTile(currentSelectedTile.tileCoordinate, collidedTile.tileCoordinate);
                
                // Calculate the distance from tower to mouse position
                float cubeDistance = Cubes.GetDistanceBetweenTwoCubes(currentSelectedTile.tileCoordinate.cube, collidedTile.tileCoordinate.cube);
                
                // Clamp the distance to valid lob range
                int targetDistance = Mathf.Clamp(Mathf.RoundToInt(cubeDistance), lobberTower.minLobDistance, lobberTower.maxLobDistance);
                
                // Get the tile at that distance in that direction
                Coordinate targetCoord = Coordinates.Instance.GetNeighbor(currentSelectedTile.tileCoordinate, bestDirection, targetDistance);
                if (targetCoord != null && lobberTower.IsValidLobTarget(targetCoord))
                {
                    targetTile = targetCoord.go.GetComponent<GroundTile>();
                }
            }

            // Only update if we found a different target tile
            if (targetTile != currentHoveredTile)
            {
                // Remove highlight from previous tile and clear all lowlighted tiles
                if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Deselect();
                }
                
                SelectionUtility.DeselectListOfTiles(lowlightedTiles);
                lowlightedTiles.Clear();

                // Set the new hovered tile
                currentHoveredTile = targetTile;
                
                // Highlight and show path if we have a valid target
                if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Highlight();
                    
                    // Show the path the pulse will travel along
                    int direction = GetLobDirection(currentSelectedTile.tileCoordinate, currentHoveredTile.tileCoordinate);
                    if (direction != -1)
                    {
                        // Lowlight tiles in the direction the pulse will continue
                        Coordinate currentCoord = currentHoveredTile.tileCoordinate;
                        int distance = 1;
                        
                        while (distance <= 10) // Show up to 10 tiles ahead
                        {
                            Coordinate nextCoord = Coordinates.Instance.GetNeighbor(currentCoord, direction, distance);
                            if (nextCoord == null) break;
                            
                            GroundTile nextTile = nextCoord.go.GetComponent<GroundTile>();
                            if (nextTile != null && nextTile != currentSelectedTile)
                            {
                                lowlightedTiles.Add(nextTile);
                                nextTile.Lowlight();
                            }
                            
                            distance++;
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
            lowlightedTiles.Clear();
        }
    }

    void HandleLobberTowerClick()
    {
    if (Mouse.current.leftButton.wasPressedThisFrame)
    {
        // Use the currently highlighted tile (which is already the best match). No need to raycast again. Will only trigger if hovering over tile.
        if (currentHoveredTile != null && currentSelectedTile != null && currentSelectedTile.tower != null)
        {
            LobberTower lobberTower = currentSelectedTile.tower as LobberTower;
            if (lobberTower != null)
            {
                lobberTower.SetTargetTile(currentHoveredTile.tileCoordinate);
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
            
            // Deselect current tile
            if (currentSelectedTile != null)
            {
                currentSelectedTile.Deselect();
                currentSelectedTile = null;
            }
            
            // Return to free mode
            currentMouseState = MouseState.Free;
        }
    }
}

    // Helper method to determine which direction a target is in (similar to LobberTower's private method)
    int GetLobDirection(Coordinate origin, Coordinate target)
    {
        if (target == null || origin == null)
            return -1;

        float distance = Cubes.GetDistanceBetweenTwoCubes(origin.cube, target.cube);

        for (int dir = 0; dir < 6; dir++)
        {
            Coordinate checkCoord = Coordinates.Instance.GetNeighbor(origin, dir, (int)distance);
            if (checkCoord != null && checkCoord.cube == target.cube)
            {
                return dir;
            }
        }

        return -1;
    }
}

public enum MouseState
{
    Free,
    SetMonoTower,
    SetLobberTower
}
