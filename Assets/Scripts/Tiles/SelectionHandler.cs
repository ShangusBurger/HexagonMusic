using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionHandler : MonoBehaviour
{
    //Mouse States
    private MouseState currentMouseState = MouseState.Free;

    //Handling Tile Selection
    public static GroundTile currentHoveredTile = null;
    public static GroundTile currentSelectedTile = null;
    private Dictionary<Vector2, GameObject> tileObjects = new Dictionary<Vector2, GameObject>();
    
    void Update()
    {
        HandleMouseHover();
        HandleMouseClick();
    }
    
    void HandleMouseHover()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GroundTile collidedTile = hit.collider.transform.GetComponent<GroundTile>();
            //Vector2 hexCoord = WorldToHexCoordinate(worldPos);

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

            if (Physics.Raycast(ray, out RaycastHit hit))
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
    
    // void SelectTile(Vector2 hexCoord)
    // {
    //     // Remove selection from previous tile
    //     if (currentSelectedTile != new Vector2(-9999, -9999))
    //     {
    //         SetTileMaterial(currentSelectedTile, defaultMaterial);
    //     }
        
    //     // Select new tile
    //     currentSelectedTile = hexCoord;
    //     SetTileMaterial(currentSelectedTile, selectedMaterial);
    // }
    
    // Material GetTileMaterial(Vector2 hexCoord)
    // {
    //     if (hexCoord == currentSelectedTile)
    //         return selectedMaterial;
    //     else
    //         return defaultMaterial;
    // }
    
    // void SetTileMaterial(Vector2 hexCoord, Material material)
    // {
    //     if (tileObjects.TryGetValue(hexCoord, out GameObject tileObj))
    //     {
    //         Renderer renderer = tileObj.GetComponent<Renderer>();
    //         if (renderer != null)
    //         {
    //             renderer.material = material;
    //         }
    //     }
    // }
    
    // Your existing coordinate conversion methods
    // public Vector2 WorldToHexCoordinate(Vector3 worldPos)
    // {
    //     float size = hexTileSize;
        
    //     float q = (2f/3f * worldPos.x) / size;
    //     float r = (-1f/3f * worldPos.x + Mathf.Sqrt(3f)/3f * worldPos.z) / size;
        
    //     return HexRound(q, r);
    // }
    
    // Vector2 HexRound(float q, float r)
    // {
    //     float s = -q - r;
        
    //     int rq = Mathf.RoundToInt(q);
    //     int rr = Mathf.RoundToInt(r);
    //     int rs = Mathf.RoundToInt(s);
        
    //     float q_diff = Mathf.Abs(rq - q);
    //     float r_diff = Mathf.Abs(rr - r);
    //     float s_diff = Mathf.Abs(rs - s);
        
    //     if (q_diff > r_diff && q_diff > s_diff)
    //         rq = -rr - rs;
    //     else if (r_diff > s_diff)
    //         rr = -rq - rs;
        
    //     return new Vector2(rq, rr);
    // }
}

public enum MouseState
{
    Free,
    HoldTower,
    HoldConnector
}
