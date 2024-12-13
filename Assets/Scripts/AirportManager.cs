using UnityEngine;
using System.Collections.Generic;

// keeps track of waypoints for a map
public class AirportManager : MonoBehaviour
{
    public static AirportManager Instance { get; private set; }
    
    private Dictionary<string, AirportWaypoint> waypoints = new Dictionary<string, AirportWaypoint>();

    private void Awake()
    {
        Instance = this;
        RegisterWaypoints();
    }

    private void RegisterWaypoints()
    {
        // Find all waypoints in scene
        var waypoints = FindObjectsOfType<AirportWaypoint>();
        foreach (var waypoint in waypoints)
        {
            this.waypoints[waypoint.waypointName] = waypoint;
        }
    }

    public Vector2 GetWaypointPosition(string waypointName)
    {
        if (waypoints.TryGetValue(waypointName, out AirportWaypoint waypoint))
        {
            return waypoint.transform.position;
        }
        return Vector2.zero;
    }
}
