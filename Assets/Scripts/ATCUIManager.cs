using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public enum Airport
{
    KLIT,
    KMDW,
    KATL,
    KDTS
}

public class ATCUIManager : MonoBehaviour
{
     public static ATCUIManager Instance { get; private set; }

    [Header("UI Elements")] // Header makes label in Unity inspector
    [SerializeField] private GameObject commandPanel;
    [SerializeField] private TMP_Dropdown commandTypeDropdown;
    [SerializeField] private TMP_Dropdown subCommandDropdown;
    [SerializeField] private TMP_Dropdown locationDropdown;
    [SerializeField] private GameObject communicationPanel;
    [SerializeField] private TextMeshProUGUI communicationText;
    [SerializeField] private Button executeButton;
    [SerializeField] private Airport currentAirport;
    [SerializeField] private AudioClip radioCallSound;
    [SerializeField] private AudioClip radioResponseSound;

    // Fields
    private AudioSource audioSource;
    private AircraftController selectedAircraft;
    private Dictionary<Airport, List<string>> airportRunways = new Dictionary<Airport, List<string>>()
    {
        { Airport.KLIT, new List<string> { "18", "36", "4R", "4L", "22R", "22L" } },
        { Airport.KMDW, new List<string> { "13C", "13R", "31C", "31L", "4R", "4L", "22R", "22L"} },
        { Airport.KDTS, new List<string> { "14", "32" } },
        { Airport.KATL, new List<string> { "8R", "8L", "9R", "9L", "10", "26R", "26L", "27R", "27L", "28" } },
    };

    private void Awake()
    {
        Instance = this;
        commandPanel.SetActive(false);
    }

    private void Start()
    {
        // Hide command panel initially
        StartCoroutine(HideInitialMessage());
        commandPanel.SetActive(false);
        
        // add radio audio source
        audioSource = gameObject.AddComponent<AudioSource>();

        // Setup initial dropdowns
        SetupCommandTypeDropdown();

        // Add listeners for dropdown value changes
        commandTypeDropdown.onValueChanged.AddListener(OnCommandTypeChanged);
        subCommandDropdown.onValueChanged.AddListener(OnSubCommandChanged);
        executeButton.onClick.AddListener(ExecuteCommand);
    }

    private void SetupCommandTypeDropdown()
    {
        List<string> commands = new List<string>
        {
            "Select", // Placeholder
            // "Taxi",
            "Takeoff",
            "Land",
            "Turn",
            // "Hold",
            // "Go Around"
        };
        
        commandTypeDropdown.ClearOptions();
        commandTypeDropdown.AddOptions(commands);
    }

    private void OnCommandTypeChanged(int index)
    {
        // Clear subsequent dropdowns
        subCommandDropdown.ClearOptions();
        locationDropdown.ClearOptions();

        // Populate subcommand based on selected command type
        switch (commandTypeDropdown.options[index].text)
        {
            case "Taxi":
                subCommandDropdown.AddOptions(new List<string> { "Select", "to runway", "to gate" }); //ideas
                break;
            case "Takeoff":
                subCommandDropdown.AddOptions(new List<string> { "Select", "runway heading", "turn 090", "turn 180", "turn 270", "turn 360" });
                break;
            case "Land":
                subCommandDropdown.AddOptions(new List<string> { "Select", "runway" }); // circle to land?
                break;
            case "Hold":
                subCommandDropdown.AddOptions(new List<string> { "Select", "position" }); // how would this even work?
                break;
            case "Go Around":
                subCommandDropdown.AddOptions(new List<string> { "Select", "runway heading", "turn 090", "turn 180", "turn 270", "turn 360" });
                break;
            case "Turn":
                subCommandDropdown.AddOptions(new List<string> { "Select", "left", "right" });
                break;
        }
    }

    private void OnSubCommandChanged(int index)
    {
        locationDropdown.ClearOptions();

        if (commandTypeDropdown.options[commandTypeDropdown.value].text == "Turn")
        {
            locationDropdown.AddOptions(new List<string> { "360", "045", "090", "135", "180", "225", "270", "315" });
        }
        else
        {
            locationDropdown.AddOptions(airportRunways[currentAirport]); // would change if more options added
        }
    }

    public void SelectAircraft(AircraftController aircraft)
    {
        // Deselect previous aircraft
        if (selectedAircraft != null)
        {
            selectedAircraft.SetSelected(false);
        }

        selectedAircraft = aircraft;
        if (selectedAircraft != null)
        {
            selectedAircraft.SetSelected(true);
            commandPanel.SetActive(true);
        }
        else
        {
            commandPanel.SetActive(false);
        }
    }

    public void AddCommunication(string message)
    {
        communicationText.text = message + "\n" + communicationText.text;
    }

    private void ExecuteCommand()
    {
        if (selectedAircraft == null) return;

        if (commandTypeDropdown.value == 0) // Placeholder is still selected
        {
            AddCommunication("Please select a valid command.");
            return;
        }
        if (subCommandDropdown.value == 0) // Subcommand placeholder
        {
            AddCommunication("Please select a valid sub-command.");
            return;
        }

        string commandType = commandTypeDropdown.options[commandTypeDropdown.value].text;
        string subCommand = subCommandDropdown.options[subCommandDropdown.value].text;
        string location = locationDropdown.options.Count > 0 ? locationDropdown.options[locationDropdown.value].text : "";

        // Build the command string 
        string command = BuildCommandString();
        string atcMessage = $"ATC: {selectedAircraft.callsign}, {command}";
        string aircraftResponse = $"{selectedAircraft.callsign}: {BuildAcknowledgement()}";
        
        selectedAircraft.ExecuteCommand(commandType, subCommand, location);
        
        // Display the communication text
        StartCoroutine(DisplayMessageSequence(atcMessage, aircraftResponse));
    }

    private string BuildCommandString()
    {
        string command = commandTypeDropdown.options[commandTypeDropdown.value].text;
        string subCommand = subCommandDropdown.options[subCommandDropdown.value].text;
        string location = locationDropdown.options.Count > 0 ? locationDropdown.options[locationDropdown.value].text : "";

        return $"{command} {subCommand} {location}".Trim();
    }

    private string BuildAcknowledgement()
    {
        string command = commandTypeDropdown.options[commandTypeDropdown.value].text.ToLower();
        string subCommand = subCommandDropdown.options[subCommandDropdown.value].text;
        string location = locationDropdown.options.Count > 0 ? locationDropdown.options[locationDropdown.value].text : "";

        switch (command)
        {
            case "taxi":
                return $"Taxiing {subCommand} {location}";
            case "takeoff":
                return $"Cleared for takeoff, {subCommand} {location}";
            case "land":
                return $"Cleared to land runway {location}";
            case "hold":
                return $"Holding at {location}";
            default:
                return "Roger"; // Add call sign and tower ident later
        }
    }

    public IEnumerator HideInitialMessage() // Welcome message for airport map
    {
        yield return new WaitForSeconds(3f);
        communicationPanel.SetActive(false);
    }

    public IEnumerator DisplayMessageSequence(string atcMessage, string aircraftResponse)
    {
        // Clear
        commandPanel.SetActive(false);
        
        // Display ATC if it exists
        if (!string.IsNullOrEmpty(atcMessage))
        {
            communicationPanel.SetActive(true);
            communicationText.color = Color.yellow;
            communicationText.text = atcMessage;
            audioSource.PlayOneShot(radioCallSound);
            yield return new WaitForSeconds(4f);
            communicationText.text = "";
        }
        
        // Only show response if it exists
        if (!string.IsNullOrEmpty(aircraftResponse))
        {
            communicationPanel.SetActive(true);
            communicationText.color = Color.white;
            communicationText.text = aircraftResponse;
            audioSource.PlayOneShot(radioResponseSound);
            yield return new WaitForSeconds(4f);
            communicationText.text = "";
        }
        
        // Hide box
        communicationPanel.SetActive(false);
    }
}
