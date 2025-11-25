using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

public class LobProjectile : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float flightDuration;
    private float elapsedTime = 0f;
    private float gravity = 80f;
    
    private Vector3 velocity;
    private bool isFlying = false;

    private float initDelay = 0f;
    
    // Reference to the target tile and pulse info
    private GroundTile targetTile;
    
    public void Initialize(Vector3 start, Vector3 target, float duration, GroundTile destination, float del)
    {
        startPosition = start;
        targetPosition = target;
        flightDuration = duration;
        targetTile = destination;
        initDelay = del;
        
        transform.position = startPosition;
        
        // Calculate initial velocity to reach target with gravity
        CalculateTrajectory();
    }
    
    void CalculateTrajectory()
    {
        // Calculate horizontal distance and direction
        Vector3 horizontalDisplacement = new Vector3(
            targetPosition.x - startPosition.x,
            0f,
            targetPosition.z - startPosition.z
        );
        
        // Horizontal velocity (constant)
        Vector3 horizontalVelocity = horizontalDisplacement / flightDuration;
        
        // Vertical velocity (to account for gravity and reach target height)
        float verticalDisplacement = targetPosition.y - startPosition.y;
        float verticalVelocity = (verticalDisplacement / flightDuration) + (0.5f * gravity * flightDuration);
        
        // Combine into full velocity vector
        velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
    }
    
    void Update()
    {
        initDelay -= Time.deltaTime;

        if (initDelay > 0) return;
        
        elapsedTime += Time.deltaTime;
        
        if (elapsedTime >= flightDuration)
        {
            // Land at target
            transform.position = targetPosition;
            Destroy(gameObject);
            return;
        }
        
        // Update position with physics
        Vector3 horizontalMovement = new Vector3(velocity.x, 0f, velocity.z) * Time.deltaTime;
        float verticalMovement = (velocity.y - (gravity * elapsedTime)) * Time.deltaTime;
        
        transform.position += horizontalMovement + new Vector3(0f, verticalMovement, 0f);
    }
}