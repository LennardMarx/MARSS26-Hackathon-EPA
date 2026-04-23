using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class CloneData
{
    public Vector3 tipPosition;
    public Vector3 needlePosition;
    public Vector3 cirPosition;
    public int trajId;
    public int heartTouchCount;
    public Vector3[] heartTouchPositions;
    public float duration;
}

/// <summary>
/// Creates and updates runtime clones of tip and needleReference GameObjects.
/// Also, saves clone positions to a JSON file when a designated toggle is turned off.
/// </summary>
public class CloneManager : MonoBehaviour
{
    public GameObject tip;
    public GameObject needleReference;
    public GameObject cir;
    public PlannedDynamicTrajectories trajectories;
    public SpatialMappingModel spatialModel;
    public GameObject referenceSpace;

    private GameObject tipClone;
    private GameObject needleClone;

    [Header("UI References")]
    public InputField nameInputField;
    public Button submitButton;
    public Toggle saveToggleAlignament; // Trigger save when turned off.
    public Toggle saveToggleAtTarget;   // Trigger save when turned off.

    private string baseFolderName = "Experiments";
    private string userDirectoryPath = ""; // Will be set on submit

    // Used to track the toggle's previous state.
    private bool lastToggleAlignamentValue = false;
    private bool lastToggleAtTargetValue = false;

    
    void Start()
    {
        tipClone = GameObject.Find("TipGlobalClone");
        needleClone = GameObject.Find("NeedleRefGlobalClone");

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmit);
        }
        else
        {
            Debug.LogWarning("Submit Button is not assigned in the Inspector.");
        }

        if (saveToggleAlignament != null && saveToggleAtTarget != null)
        {
            // Initialize toggles to false.
            saveToggleAlignament.isOn = false;
            lastToggleAlignamentValue = false;
            saveToggleAlignament.onValueChanged.AddListener(OnToggleAlignamentChanged);

            saveToggleAtTarget.isOn = false;
            lastToggleAtTargetValue = false;
            saveToggleAtTarget.onValueChanged.AddListener(OnToggleAtTargetChanged);
        }
        else
        {
            Debug.LogWarning("Save Toggles are not assigned in the Inspector.");
        }
    }

    void Update()
    {
        UpdateClone(tip, tipClone);
        UpdateClone(needleReference, needleClone);
    }

    private void UpdateClone(GameObject original, GameObject clone)
    {
        if (original != null && clone != null)
        {
            clone.transform.position = original.transform.position;
            clone.transform.rotation = original.transform.rotation;
        }
    }

    /// <summary>
    /// Called when the submit button is pressed.
    /// Creates the user directory using Application.persistentDataPath.
    /// </summary>
    public void OnSubmit()
    {
        string userName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(userName))
        {
            Debug.LogWarning("User name is empty. Please enter a valid name.");
            return;
        }

        // Using persistentDataPath for cross-platform compatibility.
        string experimentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        userDirectoryPath = Path.Combine(experimentsPath, "Experiments", userName);

        if (!Directory.Exists(experimentsPath))
        {
            Directory.CreateDirectory(experimentsPath);
            Debug.Log("Created experiments directory at: " + experimentsPath);
        }

        if (!Directory.Exists(userDirectoryPath))
        {
            Directory.CreateDirectory(userDirectoryPath);
            Debug.Log("Created user directory at: " + userDirectoryPath);
        }
        else
        {
            Debug.Log("Directory already exists: " + userDirectoryPath);
        }
    }

    /// <summary>
    /// Called when the alignment toggle value changes.
    /// When the toggle goes from on to off, it saves the positions.
    /// </summary>
    /// <param name="isOn">Current state of the toggle.</param>
    private void OnToggleAlignamentChanged(bool isOn)
    {
        Debug.Log("Alignment Toggle changed. New value: " + isOn);
        if (lastToggleAlignamentValue && !isOn)
        {
            Debug.Log("SAVING ALIGNMENT");
            SaveAlignmentPositions();
        }
        lastToggleAlignamentValue = isOn;
    }

    /// <summary>
    /// Called when the at-target toggle value changes.
    /// When the toggle goes from on to off, it saves the positions.
    /// </summary>
    /// <param name="isOn">Current state of the toggle.</param>
    private void OnToggleAtTargetChanged(bool isOn)
    {
        Debug.Log("At Target Toggle changed. New value: " + isOn);
        if (lastToggleAtTargetValue && !isOn)
        {
            Debug.Log("SAVING AT TARGET");
            SaveAtTargetPositions();
        }
        lastToggleAtTargetValue = isOn;
    }

    /// <summary>
    /// Saves clone positions to a JSON file inside the user's directory for alignment.
    /// The positions are transformed to the local coordinate system of the referenceSpace.
    /// </summary>
    private void SaveAlignmentPositions()
    {
        if (string.IsNullOrEmpty(userDirectoryPath))
        {
            Debug.LogWarning("User directory is not set. Please submit a valid user name first.");
            return;
        }
        if (tipClone == null || needleClone == null)
        {
            Debug.LogWarning("One or both clones are missing. Cannot save positions.");
            return;
        }
        if (referenceSpace == null)
        {
            Debug.LogError("Reference object (referenceSpace) not assigned!");
            return;
        }

        Transform refTransform = referenceSpace.transform;

        // Get heart touch positions once as an array.
        /*Vector3[] globalHeartTouchPositions = spatialModel.heartTouchPositions.ToArray();
        Vector3[] localheartTouchPositions = new Vector3[globalHeartTouchPositions.Length];
        for (int i = 0; i < globalHeartTouchPositions.Length; i++)
        {
            localheartTouchPositions[i] = refTransform.InverseTransformPoint(globalHeartTouchPositions[i]);
        }*/

        CloneData data = new CloneData // saving local positions
        {
            tipPosition = refTransform.InverseTransformPoint(tipClone.transform.position),
            needlePosition = refTransform.InverseTransformPoint(needleClone.transform.position),
            cirPosition = refTransform.InverseTransformPoint(cir.transform.position),
            trajId = trajectories.randomChoice + 1,
            heartTouchCount = spatialModel.heartTouchCount,
            duration = spatialModel.toolAlignmentDuration
        };

        string jsonData = JsonUtility.ToJson(data, true);
        string fileName = "AlignmentPositions_" + (trajectories.randomChoice + 1).ToString() + ".json";
        string filePath = Path.Combine(userDirectoryPath, fileName);

        try
        {
            File.WriteAllText(filePath, jsonData);
            Debug.Log("Alignment positions saved to: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving alignment positions: " + ex.Message);
        }
    }

    /// <summary>
    /// Saves clone positions to a JSON file inside the user's directory for at-target positions.
    /// The positions are transformed to the local coordinate system of the referenceSpace.
    /// </summary>
    private void SaveAtTargetPositions()
    {
        if (string.IsNullOrEmpty(userDirectoryPath))
        {
            Debug.LogWarning("User directory is not set. Please submit a valid user name first.");
            return;
        }
        if (tipClone == null || needleClone == null)
        {
            Debug.LogWarning("One or both clones are missing. Cannot save positions.");
            return;
        }
        if (referenceSpace == null)
        {
            Debug.LogError("Reference object (referenceSpace) not assigned!");
            return;
        }

        Transform refTransform = referenceSpace.transform;

        //Vector3[] globalHeartTouchPositions = spatialModel.heartTouchPositions.ToArray();
        //Vector3[] localheartTouchPositions = new Vector3[globalHeartTouchPositions.Length];
        //for (int i = 0; i < globalHeartTouchPositions.Length; i++)
        //{
          //  localheartTouchPositions[i] = refTransform.InverseTransformPoint(globalHeartTouchPositions[i]);
        //}

        CloneData data = new CloneData // saving local positions
        {
            tipPosition = refTransform.InverseTransformPoint(tipClone.transform.position),
            needlePosition = refTransform.InverseTransformPoint(needleClone.transform.position),
            cirPosition = refTransform.InverseTransformPoint(cir.transform.position),
            trajId = trajectories.randomChoice + 1,
            heartTouchCount = spatialModel.heartTouchCount,
            duration = spatialModel.toTargetReachingDuration
        };

        string jsonData = JsonUtility.ToJson(data, true);
        string fileName = "AtTargetPositions_" + (trajectories.randomChoice + 1).ToString() + ".json";
        string filePath = Path.Combine(userDirectoryPath, fileName);

        try
        {
            File.WriteAllText(filePath, jsonData);
            Debug.Log("At target positions saved to: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving at target positions: " + ex.Message);
        }
    }
}