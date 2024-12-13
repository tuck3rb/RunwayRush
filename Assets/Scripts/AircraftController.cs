using UnityEngine;
// using System.Collections.Generic;
// using System.Collections;
using TMPro;

public enum AircraftCommand // what you're telling the plane to do
{
    Taxi,
    TakeOff,
    Land,
    Hold, // could be implemented later
    GoAround // could be implemented later
}
public enum AircraftState // Aircraft state
{
    InAir,
    Landing,
    Landed,
    TakingOff,
    Taxiing,
    Parked,
    ReadyForTakeoff,
    Holding,
}

public class AircraftController : MonoBehaviour
{
    [Header("Aircraft Info")]
    public string callsign; // tail number
    public AircraftState currentState = AircraftState.InAir; // default to InAir
    public bool isSelected = false;

    [Header("Movement Settings")] // Headers allow to see these settings in Unity inspector
    [SerializeField] private float maxTaxiSpeed = 12f;
    [SerializeField] private float maxFlyingSpeed = 15f;
    [SerializeField] private float acceleration = 3f;
    [SerializeField] private float deceleration = 1f;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private float turnSlowdownFactor = 0.5f; // Slow down during turns

    [Header("Takeoff Settings")]
    [SerializeField] private float takeoffSpeed = 25f;
    [SerializeField] private float climbSpeed = 35f;
    [SerializeField] private float departureDistance = 1000f;
    [SerializeField] private float landingSpeed = 18f;

    // Fields
    private Vector2 targetPosition;
    private float currentSpeed = 0f;
    private bool isMoving = false;
    private bool isHolding = false;
    private float targetRotation;
    private bool isRotating = false;
    private Vector2 takeoffWaypoint;
    private float distanceTraveled = 0f;
    private bool inDeparture = false; 
    private float targetHeading;
    private string landingRunway;
    private SpriteRenderer radarBlip;
    private SpriteRenderer radarBlipSpriteRenderer;
    private LineRenderer radarBlipLineRenderer;
    private TextMeshPro callsignText;
    private GameObject callsignBackground;
    private Transform callsignContainer;
    public TMP_FontAsset tagFont;
    public bool hasEmergency = false;
    private float emergencyTimeLimit = 100f;
    private float emergencyTimeRemaining;

    
    private void Start()
    {
        // Generate random callsign if none assigned
        if (string.IsNullOrEmpty(callsign))
        {
            callsign = "N" + Random.Range(100, 999);
        }

        // Creating the tail number/callsign tag that goes atop a plane
        
        // Box that holds tag
        callsignContainer = new GameObject("CallsignContainer").transform;
        callsignContainer.SetParent(transform);
        callsignContainer.localPosition = Vector3.zero;

        // Tag
        callsignBackground = new GameObject("Background");
        callsignBackground.transform.SetParent(callsignContainer.transform);
        callsignBackground.transform.localPosition = Vector3.right * 70f; // offset from atop plane

        SpriteRenderer backgroundRenderer = callsignBackground.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = CreateRectangleSprite(new Color(0, 0, 0, 0.5f));
        backgroundRenderer.transform.localScale = new Vector3(7000f, 2000f, 1f);
        backgroundRenderer.sortingOrder = 1;
        backgroundRenderer.sortingLayerName = "Aircraft"; // ensuring it appears in same layer as aircraft

        // Tag text
        GameObject textObj = new GameObject("CallsignText");
        textObj.transform.SetParent(callsignContainer.transform);
        textObj.transform.localPosition = new Vector3(0, 0.5f, 0);
        
        callsignText = textObj.AddComponent<TextMeshPro>();
        callsignText.font = tagFont;
        callsignText.text = callsign;
        callsignText.fontSize = 250;
        callsignText.alignment = TextAlignmentOptions.Center;
        callsignText.color = Color.white;
        callsignText.sortingOrder = 2;
        callsignText.sortingLayerID = SortingLayer.NameToID("Aircraft");
        // sizing textbox
        callsignText.rectTransform.sizeDelta = new Vector2(200, callsignText.rectTransform.sizeDelta.y);
        callsignText.characterSpacing = 10f;
        callsignText.transform.localPosition = Vector3.right * 70f; // offset from atop plane
    }

    private void Update()
    {
        if (isMoving && !isHolding)
        {
            MoveAircraft();
        }
        else if (isHolding || !isMoving)
        {
            ApplyDeceleration(); // more realistic motion
        }

        // ensuring tag doesn't rotate with plane
        if (callsignContainer != null)
        {
            callsignContainer.rotation = Quaternion.identity; 
            callsignContainer.position = transform.position + transform.right * 30f;
        }

        // checks if emergency has been failed
        if (hasEmergency)
        {
            emergencyTimeRemaining -= Time.deltaTime;
            if (emergencyTimeRemaining <= 0)
            {
                GameManager.Instance.GameOver(GameEndReason.EmergencyFailed);
            }
        }
    }

    private void MoveAircraft()
    {
        Vector2 currentPos = transform.position;
        Vector2 directionToTarget = (targetPosition - currentPos).normalized;
        
        // Rotation calculations
        targetRotation = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;
        float currentRotation = transform.eulerAngles.z;
        float angleDiff = Mathf.DeltaAngle(currentRotation, targetRotation);
        bool isTurning = Mathf.Abs(angleDiff) > 45f;

        // Handle rotation
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, targetRotation),
            rotationSpeed * Time.deltaTime
        );

        // Speed calculations
        float targetSpeed = (currentState == AircraftState.InAir) ? maxFlyingSpeed : maxTaxiSpeed;
        if (isTurning)
        {
            targetSpeed *= turnSlowdownFactor;
        }

        // Acceleration
        if (currentSpeed < targetSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
            if (currentSpeed > targetSpeed)
                currentSpeed = targetSpeed;
        }
        else if (currentSpeed > targetSpeed)
        {
            currentSpeed -= deceleration * Time.deltaTime;
            if (currentSpeed < targetSpeed)
                currentSpeed = targetSpeed;
        }

        if (currentState == AircraftState.TakingOff)
        {
            HandleTakeoffPhase();
        }

        // Movement
        Vector2 movement = directionToTarget * currentSpeed * Time.deltaTime;
        transform.position = new Vector3(
            currentPos.x + movement.x,
            currentPos.y + movement.y,
            transform.position.z
        );

        // check if reached target
        if (Vector2.Distance(currentPos, targetPosition) < 0.1f)
        {
            isMoving = false;
            OnDestinationReached();
        }

        if (inDeparture)
        {
            distanceTraveled += currentSpeed * Time.deltaTime;
            if (distanceTraveled >= 995) // Plane deletes itself once safely departed ~1000 away
            {
                Destroy(gameObject);
                FindObjectOfType<AircraftListManager>().RemoveAircraft(callsign);
            }
        }
    }

    private void ApplyDeceleration()
    {
        if (currentSpeed > 0)
        {
            currentSpeed -= deceleration * Time.deltaTime;
            currentSpeed = Mathf.Max(currentSpeed, 0); // Make sure speed doesnt go below 0
            
            // Apply remaining momentum
            if (currentSpeed > 0)
            {
                Vector2 currentPos = transform.position;
                // Calculate forward based on current rotation
                // Convert rotation to direction vectors
                Vector2 forward = (Vector2)(Quaternion.Euler(0, 0, transform.eulerAngles.z) * Vector2.up);
                Vector2 movement = forward * currentSpeed * Time.deltaTime;
                transform.position = new Vector3(
                    currentPos.x + movement.x,
                    currentPos.y + movement.y,
                    transform.position.z
                );
            }
        }
    }
    
    private void OnDestinationReached()
    {
        switch (currentState)
        {
            case AircraftState.Taxiing:
                if (takeoffWaypoint != Vector2.zero)  // If we're taxiing for takeoff
                {
                    currentState = AircraftState.TakingOff;
                    SetDestination(takeoffWaypoint);
                }
                else
                {
                    currentState = AircraftState.ReadyForTakeoff;
                }
                break;
            case AircraftState.Landing: // Coming in for landing turn towards the end of runway
                Vector2 takeoffPoint = AirportManager.Instance.GetWaypointPosition($"{landingRunway}_TAKEOFF");
                if (takeoffPoint != Vector2.zero)
                {
                    SetDestination(takeoffPoint);
                    currentState = AircraftState.Landed;
                }
                break;
                
            case AircraftState.Landed: // Not tower's problem anymore once safely on ground
                Destroy(gameObject);
                FindObjectOfType<AircraftListManager>().RemoveAircraft(callsign);
                break;
        }
    }

    public void SetDestination(Vector2 position)
    {
        if (position != Vector2.zero)
        {
            targetPosition = position;
            isMoving = true;
            isHolding = false;
        }
    }

    public void ExecuteCommand(string commandType, string subCommand, string location)
    {
        switch (commandType.ToLower()) // not all implemented but good for future use
        {
            case "taxi":
                HandleTaxiCommand(subCommand, location);
                break;
            case "takeoff":
                HandleTakeoffCommand(subCommand, location);
                break;
            case "hold":
                isHolding = true;
                currentState = AircraftState.Holding;
                break;
            case "continue":
                isHolding = false;
                break;
            case "land":
                HandleLandingCommand(subCommand, location);
                break;
            case "turn":
                HandleTurnCommand(subCommand, location);
                break;
            default:
                break;
        }
    }

    private void HandleTaxiCommand(string subCommand, string location)
    {
        if (subCommand.Contains("runway"))
        {
            Vector2 runwayPos = AirportManager.Instance.GetWaypointPosition(location);
            SetDestination(runwayPos);
            currentState = AircraftState.Taxiing;
        }
    }

    private void HandleTakeoffCommand(string subCommand, string location)
    {
        // Debug.Log($"Current state: {currentState}");
        if (currentState == AircraftState.ReadyForTakeoff)
        {
            currentState = AircraftState.Taxiing;
            takeoffWaypoint = AirportManager.Instance.GetWaypointPosition($"{location}_TAKEOFF");
            
            Vector2 runwayPos = AirportManager.Instance.GetWaypointPosition(location);
            SetDestination(runwayPos);
            
            isRotating = false;
            if (subCommand.Contains("turn")) // "turn 090" or "turn 180", etc.
            {
                string[] parts = subCommand.Split(' ');
                if (float.TryParse(parts[1], out float heading))
                {
                    targetHeading = heading;
                    isRotating = true;
                }
            }
        }
    }

    private void HandleTakeoffPhase()
    {
        if (currentSpeed >= takeoffSpeed)
        {
            maxFlyingSpeed = climbSpeed;
        }

        if (Vector2.Distance(transform.position, takeoffWaypoint) < 0.1f) // Reached runway end
        {
            currentState = AircraftState.InAir;
            inDeparture = true;

            // Only change direction if we're asked to turn
            if (isRotating)
            {
                float desiredRotation = -targetHeading;
                targetRotation = desiredRotation;
                Vector2 direction = (Quaternion.Euler(0, 0, desiredRotation) * Vector2.up);
                targetPosition = (Vector2)transform.position + direction * departureDistance;
            }
            else
            {
                // Continue straight out
                Vector2 direction = transform.up;
                targetPosition = (Vector2)transform.position + direction * departureDistance;
            }
            isMoving = true;
        }
    }

    private void HandleLandingCommand(string subCommand, string location)
    {
        if (currentState == AircraftState.InAir)
        {
            currentState = AircraftState.Landing;
            maxFlyingSpeed = landingSpeed;
            landingRunway = location;
            Vector2 runwayPos = AirportManager.Instance.GetWaypointPosition(location);
            SetDestination(runwayPos);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<AircraftController>() != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.black; // turn black so user sees  collision
            }
            GameManager.Instance.GameOver(GameEndReason.Collision); // Ends game  
        }
    }

    public void SetRadarBlip(GameObject blip)
    {
        radarBlipSpriteRenderer = blip.GetComponent<SpriteRenderer>();
        radarBlipLineRenderer = blip.GetComponent<LineRenderer>();
    }

    public void SetSelected(bool selected) // selecting plane for giving commands
    {
        isSelected = selected;

        // Color changes in tag and on radar
        if (callsignText != null)
        {
            callsignText.color = selected ? Color.yellow : Color.white;
        }
        if (radarBlipSpriteRenderer != null)
        {
            radarBlipSpriteRenderer.color = selected ? Color.yellow : Color.green;
            radarBlipLineRenderer.startColor = selected ? Color.yellow : Color.green;
            radarBlipLineRenderer.endColor = selected ? Color.yellow : Color.green;
        }
    }

    private Sprite CreateRectangleSprite(Color color) // helper function 
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.SetPixel(0, 0, color);
        texture.SetPixel(1, 0, color);
        texture.SetPixel(0, 1, color);
        texture.SetPixel(1, 1, color);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public void DeclareEmergency()
    {
        hasEmergency = true;
        emergencyTimeRemaining = emergencyTimeLimit;

        // Turn red on radar and tag
        if (radarBlipSpriteRenderer != null)
            radarBlipSpriteRenderer.color = Color.red;
        if (radarBlipLineRenderer != null)
        {
            radarBlipLineRenderer.startColor = Color.red;
            radarBlipLineRenderer.endColor = Color.red;
        }
        if (callsignText != null)
        {
            callsignText.color = Color.red;
        }

        // message
        if (ATCUIManager.Instance != null)
        {
            string announcement = $"{callsign}: MAYDAY MAYDAY MAYDAY, {callsign} declaring emergency";
            ATCUIManager.Instance.StartCoroutine(ATCUIManager.Instance.DisplayMessageSequence("", announcement));
        }
    }

    private void HandleTurnCommand(string subCommand, string heading)
    {
        // math with this is difficult and still doesnt work perfectly 
        if (currentState == AircraftState.InAir)
        {
            if (float.TryParse(heading, out float targetHeading))
            {
                float currentHeading = transform.eulerAngles.z;
                float diff = targetHeading - currentHeading;
                
                // Normalize the difference to -180 to 180
                if (diff > 180) diff -= 360;
                if (diff < -180) diff += 360;

                // Check if current turn direction matches commanded direction
                bool shouldTurnRight = diff > 0;
                if ((subCommand == "right" && !shouldTurnRight) || 
                    (subCommand == "left" && shouldTurnRight))
                {
                    // Adjust the target heading by 360 to force turn in correct direction
                    targetHeading = subCommand == "right" ? targetHeading + 360 : targetHeading - 360;
                }

                this.targetHeading = targetHeading;
                SetDestination((Vector2)transform.position + CalculateHeadingVector(this.targetHeading) * 1000f);
            }
        }
    }

    private Vector2 CalculateHeadingVector(float heading) // helper function
    {
        float angleRadians = (90 - heading) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
    }
}
