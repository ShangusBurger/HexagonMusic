using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CubeCoordinates;

public class SelectionHandler : MonoBehaviour
{
    //Mouse States
    public static MouseState currentMouseState = MouseState.Free;

    //Handling Tile Selection
    public static GroundTile currentHoveredTile = null;
    public static GroundTile currentSelectedTile = null;
    private Dictionary<Vector2, GameObject> tileObjects = new Dictionary<Vector2, GameObject>();
    [SerializeField] private GameObject towerSelectCanvas;
    public static SelectionHandler Instance;
    List<GroundTile> highlightedTiles = new List<GroundTile>();

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
        Debug.Log("Current Mouse State: " + currentMouseState);
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
        }
    }

    public static void OfferTowerPlacement(GroundTile tile)
    {
        Instance.towerSelectCanvas.SetActive(true);
        Instance.towerSelectCanvas.transform.position = new Vector3(tile.transform.position.x, Instance.towerSelectCanvas.transform.position.y, tile.transform.position.z);
        Instance.towerSelectCanvas.GetComponent<TowerSelection>().SetTargetTile(tile);
    }

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
                // Remove highlight from previous tile
                if (currentHoveredTile != null && currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Deselect();
                    foreach (GroundTile tile in highlightedTiles)
                    {
                        tile.Deselect();
                        highlightedTiles.Clear();
                    }
                }

                // Highlight new tile (only if it's not selected)
                currentHoveredTile = collidedTile;
                if (currentHoveredTile != currentSelectedTile)
                {
                    currentHoveredTile.Highlight();
                    List<Coordinate> coordsBetween = Coordinates.Instance.GetLine(currentSelectedTile.tileCoordinate, currentHoveredTile.tileCoordinate);
                    foreach (Coordinate coord in coordsBetween)
                    {
                        GroundTile coordTile = coord.go.GetComponent<GroundTile>();
                        highlightedTiles.Add(coordTile);
                        coordTile.Lowlight();
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
        }
    }
    
    void HandleMonoTowerClick()
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
                currentMouseState = MouseState.Free;
            }
        }
    }
}

public enum MouseState
{
    Free,
    SetMonoTower,
}
