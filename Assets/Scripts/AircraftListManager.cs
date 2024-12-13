using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Keeps track of aircraft in game and shows them on the scroll list on the sidebar
public class AircraftListManager : MonoBehaviour
{
    public GameObject aircraftEntryPrefab;
    public Transform contentParent;
    private Dictionary<string, GameObject> aircraftEntries = new Dictionary<string, GameObject>();

    public void AddAircraft(AircraftController aircraft)
    {
        if (!aircraftEntries.ContainsKey(aircraft.callsign))
        {
            GameObject entry = Instantiate(aircraftEntryPrefab, contentParent);
            entry.GetComponentInChildren<TextMeshProUGUI>().text = aircraft.callsign;
            
            Button button = entry.GetComponent<Button>();
            button.onClick.AddListener(() => ATCUIManager.Instance.SelectAircraft(aircraft));
            
            aircraftEntries.Add(aircraft.callsign, entry);
        }
    }

    public void RemoveAircraft(string callsign)
    {
        if (aircraftEntries.TryGetValue(callsign, out GameObject entry))
        {
            Destroy(entry);
            aircraftEntries.Remove(callsign);
        }
    }
}
