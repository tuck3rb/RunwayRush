using UnityEngine;

public enum WaypointType
{
    Runway,
    Gate, // *
    Holding, // *
    TakeoffPosition,
    Departure, // *
    // * could be used to add more mechanics to game. Play as ground controller? TRACON?
}

public class AirportWaypoint : MonoBehaviour
{
    public WaypointType type;
    public string waypointName; 
    
    private void OnDrawGizmos() // Different colors by type
    {
        Gizmos.color = type switch
        {
            WaypointType.Runway => Color.blue,
            WaypointType.Gate => Color.green,
            WaypointType.Holding => Color.yellow,
            WaypointType.TakeoffPosition => Color.red,
            _ => Color.white
        };
        
        Gizmos.DrawWireSphere(transform.position, 33f);
    }
}
