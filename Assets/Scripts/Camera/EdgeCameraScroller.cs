using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class EdgeCameraScroller : MonoBehaviour
{
    [Header("Camera Movement Settings")]
    public float moveSpeed = 5f;
    public float edgeThreshold = 50f; // Distance from screen edge in pixels
    public float smoothTime = 0.3f;   // Time for smooth movement

    [Header("Camera Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minY = 10;
    public float maxY = 30; 
    
    [Header("Camera Bounds (Optional) XY > XZ Plane")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-10f, -10f);
    public Vector2 maxBounds = new Vector2(10f, 10f);
    
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    
    void Start()
    {
        targetPosition = transform.position;
    }
    
    void Update()
    {
        HandleEdgeScrolling();
        HandleZoom();
        MoveCamera();
    }
    
    void HandleEdgeScrolling()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Vector2 moveDirection = Vector2.zero;

        // Check horizontal edges
        if (mousePosition.x <= edgeThreshold)
        {
            // Left edge
            float normalizedDistance = (edgeThreshold - mousePosition.x) / edgeThreshold;
            moveDirection.x = -normalizedDistance;
        }
        else if (mousePosition.x >= Screen.width - edgeThreshold)
        {
            // Right edge
            float normalizedDistance = (mousePosition.x - (Screen.width - edgeThreshold)) / edgeThreshold;
            moveDirection.x = normalizedDistance;
        }
        
        // Check vertical edges
        if (mousePosition.y <= edgeThreshold)
        {
            // Bottom edge
            float normalizedDistance = (edgeThreshold - mousePosition.y) / edgeThreshold;
            moveDirection.y = -normalizedDistance;
        }
        else if (mousePosition.y >= Screen.height - edgeThreshold)
        {
            // Top edge
            float normalizedDistance = (mousePosition.y - (Screen.height - edgeThreshold)) / edgeThreshold;
            moveDirection.y = normalizedDistance;
        }

        moveDirection.x += Input.GetAxisRaw("Horizontal") * 2f;
        moveDirection.y += Input.GetAxisRaw("Vertical") * 2f;
        
        // Apply movement to target position
        Vector3 movement = new Vector3(moveDirection.x, 0, moveDirection.y) * moveSpeed * Time.deltaTime;
        targetPosition += movement;
        
        // Apply bounds if enabled
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);
        }
    }
    
    void HandleZoom()
    {
        float scrollInput = Mouse.current.scroll.ReadValue().y;

        if (scrollInput != 0f && !EventSystem.current.IsPointerOverGameObject())
        {
            targetPosition.y -= scrollInput * zoomSpeed * Time.deltaTime;
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }
    }
    void MoveCamera()
    {
        // Apply both movements
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);;
    }
    
    // Optional: Visual debug for bounds
    void OnDrawGizmos()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, transform.position.y, (minBounds.y + maxBounds.y) / 2f);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, 0, maxBounds.y - minBounds.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}