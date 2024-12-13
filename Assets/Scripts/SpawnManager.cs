using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class SpawnManager : MonoBehaviour 
{
    [SerializeField] private float groundSpawnChance = 0.4f;
    [SerializeField] private AircraftListManager listManager;
    
    //  Fields
    public GameObject aircraftPrefab;
    public GameObject jetPrefab;
    public GameObject radarBlipPrefab;
    public Transform[] airSpawnPoints;
    public Transform[] groundSpawnPoints;

    private float gameTimer;
    private float timeToNextSpawn;

    void Start()
    {
        SetNewRandomSpawnTime();
        gameTimer = GameManager.Instance.GameDuration;
    }

    void SpawnAircraft()
    {
        // Decide if this will be ground or air spawn
        if (Random.value < groundSpawnChance)
        {
            SpawnGroundAircraft();
        }
        else
        {
            SpawnAirAircraft();
        }
    }

    void Update()
    {
        if (GameManager.Instance.GameDuration > 0)
        {
            gameTimer -= Time.deltaTime;
            if (gameTimer <= 0)
            {
                // Player won
                GameManager.Instance.Victory();
            }
        }
        
        timeToNextSpawn -= Time.deltaTime;
        if (timeToNextSpawn <= 0)
        {
            SpawnAircraft();
            SetNewRandomSpawnTime();
        }
    }

    private void SetNewRandomSpawnTime() // Might mess with intervals more to get more variation
    {
        timeToNextSpawn = Random.Range(5f, 15f);
        switch (GameManager.Instance.TrafficLevel)
        {
            case TrafficLevel.Low:
                timeToNextSpawn = Random.Range(10f, 50f);
                break;
            case TrafficLevel.Medium:
                timeToNextSpawn = Random.Range(7f, 40f);
                break;
            case TrafficLevel.High:
                timeToNextSpawn = Random.Range(5f, 30f);
                break;
            default:
                timeToNextSpawn = Random.Range(7f, 40f);  // Default to medium if something goes wrong
                break;
        }
    }

    void SpawnAirAircraft()
    {
        Transform spawnPoint = airSpawnPoints[Random.Range(0, airSpawnPoints.Length)];
        GameObject prefabToSpawn = Random.value < 0.5f ? aircraftPrefab : jetPrefab; // random pick
        GameObject aircraftObj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        
        AircraftController aircraft = aircraftObj.GetComponent<AircraftController>();
        if (aircraft != null)
        {
            aircraft.callsign = "N" + Random.Range(100, 999) + GenerateRandomLetters(2);
            aircraft.currentState = AircraftState.InAir; // actual tail spawner
            
            Vector2 targetPos = (Vector2)spawnPoint.position + ((Vector2)spawnPoint.up * 2000f);
            aircraft.SetDestination(targetPos);

            string announcement = $"{aircraft.callsign}: Tower, {aircraft.callsign} inbound from the {spawnPoint.name}";
            ATCUIManager.Instance.StartCoroutine(ATCUIManager.Instance.DisplayMessageSequence("", announcement));

            // Add radar blip
            GameObject radarBlip = Instantiate(radarBlipPrefab);
            radarBlip.transform.SetParent(aircraftObj.transform); 
            radarBlip.transform.localPosition = Vector3.zero; 
            radarBlip.transform.localRotation = Quaternion.identity;
            aircraft.SetRadarBlip(radarBlip);

            listManager.AddAircraft(aircraft);
        }

        if (GameManager.Instance.EmergenciesEnabled && Random.Range(0, 50) == 0)  // 1 in 50 chance
        {
            aircraft.DeclareEmergency();
        }
    }

    void SpawnGroundAircraft()
    {
        // Get a list of unoccupied ground spawn points
        List<Transform> availableSpawnPoints = new List<Transform>();
        
        foreach (Transform spawnPoint in groundSpawnPoints)
        {
            // Check for any aircraft nearby
            Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPoint.position, 50f);
            bool isOccupied = false;
            
            foreach (Collider2D collider in colliders)
            {
                if (collider.GetComponent<AircraftController>() != null)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }

        // Only spawn if we have an available point
        if (availableSpawnPoints.Count > 0)
        {
            Transform spawnPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
            GameObject prefabToSpawn = Random.value < 0.5f ? aircraftPrefab : jetPrefab; // random pick
            GameObject aircraftObj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            
            AircraftController aircraft = aircraftObj.GetComponent<AircraftController>();
            if (aircraft != null)
            {
                aircraft.callsign = "N" + Random.Range(100, 999) + GenerateRandomLetters(2);
                aircraft.currentState = AircraftState.ReadyForTakeoff; // actual tail spawne

                string announcement = $"{aircraft.callsign}: Tower, {aircraft.callsign} holding short at {spawnPoint.name}";
                ATCUIManager.Instance.StartCoroutine(ATCUIManager.Instance.DisplayMessageSequence("", announcement));

                // Add radar blip
                GameObject radarBlip = Instantiate(radarBlipPrefab);
                radarBlip.transform.SetParent(aircraftObj.transform); 
                radarBlip.transform.localPosition = Vector3.zero; 
                radarBlip.transform.localRotation = Quaternion.identity;
                aircraft.SetRadarBlip(radarBlip);

                listManager.AddAircraft(aircraft);
            }
        }
    }

    private string GenerateRandomLetters(int length)
    {
        const string letters = "ABCDEFGIJKLNOPRSTUWX"; // in font some letters looked bad so ommit those
        string result = "";
        for (int i = 0; i < length; i++)
        {
            result += letters[Random.Range(0, letters.Length)];
        }
        return result;
    }
}
