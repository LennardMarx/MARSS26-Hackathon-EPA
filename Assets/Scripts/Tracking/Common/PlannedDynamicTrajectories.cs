using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using System.Security.Cryptography;
using System.IO;

public class PlannedDynamicTrajectories : MonoBehaviour
{
    public GameObject phantom;
    public Transform cylinderPrefab;
    public GameObject end, start, circle_1, circle_2, circle_3;
    public GameObject pztEntry;
    //public GameObject referenceSpace;
    private GameObject cylinder;
    [HideInInspector]
    public Vector3 entry_skin_normal;
    private Vector3 target_pos;
    private Vector3 entry_skin_pos;
    public PlannedTrajectory t_first, t_second, t_third, t_fourth, t_fifth, t_sixth, t_seventh, t_eight, t_nine, t_ten;
    private Quaternion traj_orientation;

    public Text tr_debug;
    [HideInInspector]
    public Vector3 entry_phantom_pos;
    public Toggle trajectoryToggle;

    // This will store the chosen trajectory (its case value).
    public int randomChoice = 0;

    // --- New Fields for unique random trajectory selection ---
    // We want to use five trajectories (mapped to cases 4 to 8).
    private Queue<int> trajectoryQueue = new Queue<int>();
    private List<int> availableTrajectories = new List<int> { 4, 5, 6, 7, 8 };

    // Start is called before the first frame update
    void Start()
    {
        if (trajectoryToggle != null)
        {
            trajectoryToggle.onValueChanged.AddListener(ToggleTrajectory);
        }
        else
        {
            Debug.LogWarning("Trajectory Toggle is not assigned in the inspector!");
        }

        // Initialize the queue with a shuffled order.
        InitializeTrajectoryQueue();

        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        start.gameObject.SetActive(false);
        circle_1.gameObject.SetActive(false);
        circle_2.gameObject.SetActive(false);
        circle_3.gameObject.SetActive(false);

        // Initialize planned trajectories (as before)
        t_first.onSkin = new Vector3(0.01794f, -0.15977f, 0.09046006f);
        t_first.onPericardium = new Vector3(0.0254f, -0.09177002f, 0.06146002f);

        t_second.onSkin = new Vector3(-0.004729999f, -0.14751f, 0.04901999f);
        t_second.onPericardium = new Vector3(0.002280002f, -0.101911f, 0.050381f);

        t_third.onSkin = new Vector3(0.0008600019f, -0.14538f, 0.03981f);
        t_third.onPericardium = new Vector3(-0.001479998f, -0.09356001f, 0.05593002f);

        t_fourth.onSkin = new Vector3(0.01147f, -0.14705f, 0.05010998f);
        t_fourth.onPericardium = new Vector3(0.001280002f, -0.09470001f, 0.05605f);

        t_fifth.onSkin = new Vector3(-0.01760172f, -0.1660865f, 0.1092585f);
        t_fifth.onPericardium = new Vector3(-0.0006827191f, -0.1000795f, 0.04703325f);

        t_sixth.onSkin = new Vector3(-0.03895036f, -0.1611824f, 0.09265393f);
        t_sixth.onPericardium = new Vector3(-0.0006827191f, -0.1000795f, 0.04703325f);

        t_seventh.onSkin = new Vector3(0.02528296f, -0.150815f, 0.07057589f);
        t_seventh.onPericardium = new Vector3(-0.0006827191f, -0.100079f, 0.04703325f);

        t_eight.onSkin = new Vector3(-0.02528296f, -0.150815f, 0.07057589f);
        t_eight.onPericardium = new Vector3(-0.0265f, -0.1000795f, 0.0521f);

        t_nine.onSkin = new Vector3(-0.0047f, -0.150815f, 0.0958f);
        t_nine.onPericardium = new Vector3(0.0116f, -0.1026f, 0.04703325f);

        t_ten.onSkin = new Vector3(0.0192f, -0.150815f, 0.0815f);
        t_ten.onPericardium = new Vector3(0.0223f, -0.1026f, 0.04703325f);
    }

    // --- New Helper Method to Shuffle and Enqueue Trajectories ---
    private void InitializeTrajectoryQueue()
    {
        List<int> tempList = new List<int>(availableTrajectories);
        // Fisher-Yates shuffle:
        for (int i = tempList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = tempList[i];
            tempList[i] = tempList[randomIndex];
            tempList[randomIndex] = temp;
        }

        trajectoryQueue.Clear();
        foreach (int traj in tempList)
        {
            trajectoryQueue.Enqueue(traj);
        }
    }

    Vector3[] ConvertListToVector3Array(List<float> floatList)
    {
        if (floatList.Count % 3 != 0)
        {
            Debug.LogWarning("Invalid number of elements in the list. Each point should have three values (x, y, z).");
            return null;
        }

        Vector3[] vectorArray = new Vector3[floatList.Count / 3];

        for (int i = 0, j = 0; i < floatList.Count; i += 3, j++)
        {
            Vector3 point = new Vector3(-floatList[i], floatList[i + 1], floatList[i + 2]);
            vectorArray[j] = point;
        }

        return vectorArray;
    }

    public void InstantiateCylinder(Transform cylinderPrefab, Vector3 beginPoint, Vector3 endPoint)
    {
        if (cylinder != null)
        {
            Destroy(cylinder);
        }
        cylinder = Instantiate<GameObject>(cylinderPrefab.gameObject, Vector3.zero, Quaternion.identity);
        cylinder.transform.SetParent(phantom.transform);
        cylinder.name = "PlannedTrajectory";
        cylinder.gameObject.SetActive(false);
    }

    public void UpdateCylinderPosition(GameObject cylinder, Vector3 beginPoint, Vector3 endPoint)
    {
        Vector3 offset = (endPoint - beginPoint);
        Vector3 position = endPoint;
        cylinder.transform.position = position;
        cylinder.transform.LookAt(beginPoint);
        Vector3 localScale = cylinder.transform.localScale;
        localScale.y = ((endPoint - beginPoint)).magnitude;
        localScale.x = 0.0035f;
        localScale.z = 0.0035f;
        cylinder.transform.Rotate(90.0f, 0.0f, 0.0f, Space.Self);
        cylinder.transform.localScale = localScale;
        cylinder.gameObject.SetActive(true);
        start.gameObject.SetActive(true);
        circle_1.gameObject.SetActive(true);
        circle_2.gameObject.SetActive(true);
        circle_3.gameObject.SetActive(true);
        Quaternion rotationToAlign = Quaternion.FromToRotation(circle_1.transform.TransformDirection(new Vector3(0, 0, 1)), cylinder.transform.up);
        circle_1.transform.position = cylinder.transform.position;
        circle_2.transform.position = cylinder.transform.position;
        circle_3.transform.position = cylinder.transform.position;
        circle_1.transform.rotation = rotationToAlign * circle_1.transform.rotation;
        circle_2.transform.rotation = rotationToAlign * circle_2.transform.rotation;
        circle_3.transform.rotation = rotationToAlign * circle_3.transform.rotation;
        Quaternion rotation_skin_target = Quaternion.LookRotation(Vector3.up, entry_skin_normal);
        start.transform.rotation = rotation_skin_target;
        circle_1.transform.Translate(new Vector3(0, 0, 1) * -0.01f, Space.Self);
        circle_2.transform.Translate(new Vector3(0, 0, 1) * -0.02f, Space.Self);
        circle_3.transform.Translate(new Vector3(0, 0, 1) * -0.03f, Space.Self);
    }
    
    public void Create_trajectory()
    {
        Debug.Log($"entry {entry_skin_pos}, target {target_pos}, normal {entry_skin_normal}");
        target_pos = phantom.transform.TransformPoint(target_pos);
        entry_skin_pos = phantom.transform.TransformPoint(entry_skin_pos);
        RaycastHit hit;
        if (Physics.Linecast(entry_skin_pos, target_pos, out hit))
        {
            entry_phantom_pos = hit.point;
            entry_skin_normal = hit.normal;
            Debug.Log($"Intersection found at {hit.point} with normal {hit.normal}");
        }
        else
        {
            Debug.LogWarning("No intersection found between entry and target points.");
        }
        start.transform.position = new Vector3(entry_phantom_pos.x, entry_phantom_pos.y + 0.002f, entry_phantom_pos.z);
        end.transform.position = target_pos;
        pztEntry.transform.position = entry_skin_pos;
        UpdateCylinderPosition(cylinder, target_pos, entry_skin_pos);
    }


    /*
    public void Create_trajectory()
    {
        Debug.Log($"entry {entry_skin_pos}, target {target_pos}, normal {entry_skin_normal}");
        target_pos = phantom.transform.TransformPoint(target_pos);
        entry_skin_pos = phantom.transform.TransformPoint(entry_skin_pos);

        Transform referenceTransform = referenceSpace.transform;
        RaycastHit hit;
        if (Physics.Linecast(entry_skin_pos, target_pos, out hit))
        {
            entry_phantom_pos = hit.point;
            entry_skin_normal = hit.normal;
            Debug.Log($"Intersection found at {hit.point} with normal {hit.normal}");
        }
        else
        {
            Debug.LogWarning("No intersection found between entry and target points.");
        }

        start.transform.position = new Vector3(entry_phantom_pos.x, entry_phantom_pos.y + 0.002f, entry_phantom_pos.z);
        end.transform.position = target_pos;
        pztEntry.transform.position = entry_skin_pos;
        UpdateCylinderPosition(cylinder, target_pos, entry_skin_pos);

        // Transform entry_phantom_pos from global to local coordinate system of referenceSpace.
        Vector3 localEntryPhantomPos = referenceTransform.InverseTransformPoint(entry_phantom_pos);
        Debug.Log("Local Entry Phantom Position: " + localEntryPhantomPos);

        // Compose the filename with timestamp.
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string fileName = "EntryPhantomPos_" + (randomChoice + 1).ToString() + ".txt";
        string filePath = Path.Combine(desktopPath, fileName);

        // Prepare the content to save.
        string content = $"Local Entry Phantom Position:\n{localEntryPhantomPos}";

        try
        {
            File.WriteAllText(filePath, content);
            Debug.Log("Entry phantom position saved to: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving entry phantom position: " + ex.Message);
        }
    }*/


    // --- Trajectory Methods (Using the planned data) ---
    public void FirstTrajectory()
        {
            entry_skin_pos = t_first.onSkin;
            target_pos = t_first.onPericardium;
            InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
            Create_trajectory();
            tr_debug.text = "Selected: 1";
            Debug.Log($"Displaing TR 1, t:{target_pos}, e:{entry_skin_pos}");

        }

    public void SecondTrajectory()
    {
        entry_skin_pos = t_second.onSkin;
        target_pos = t_second.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 2";
        Debug.Log($"Displaing TR 2, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void ThirdTrajectory()
    {
        entry_skin_pos = t_third.onSkin;
        target_pos = t_third.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 3";
        Debug.Log($"Displaing TR 3, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void FourthTrajectory()
    {
        entry_skin_pos = t_fourth.onSkin;
        target_pos = t_fourth.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 4";
        Debug.Log($"Displaing TR 4, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void FifthTrajectory()
    {
        entry_skin_pos = t_fifth.onSkin;
        target_pos = t_fifth.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 5";
        Debug.Log($"Displaing TR 5, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void SixthTrajectory()
    {
        entry_skin_pos = t_sixth.onSkin;
        target_pos = t_sixth.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 6";
        Debug.Log($"Displaing TR 6, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void SeventhTrajectory()
    {
        entry_skin_pos = t_seventh.onSkin;
        target_pos = t_seventh.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 7";
        Debug.Log($"Displaing TR 7, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void EightTrajectory()
    {
        entry_skin_pos = t_eight.onSkin;
        target_pos = t_eight.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 8";
        Debug.Log($"Displaing TR 8, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void NineTrajectory()
    {
        entry_skin_pos = t_nine.onSkin;
        target_pos = t_nine.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 9";
        Debug.Log($"Displaing TR 9, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void TenTrajectory()
    {
        entry_skin_pos = t_ten.onSkin;
        target_pos = t_ten.onPericardium;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        Create_trajectory();
        tr_debug.text = "Selected: 10";
        Debug.Log($"Displaing TR 10, t:{target_pos}, e:{entry_skin_pos}");
    }

    public void ToggleTrajectory(bool isVisible)
    {
        Debug.Log("PRESSED TOGGLE PATH VISIBILITY: " + isVisible);
        if (cylinder != null)
        {
            cylinder.SetActive(isVisible);
        }
    }

    /// <summary>
    /// Returns a different trajectory for five subsequent calls by cycling through a shuffled queue.
    /// Only the trajectories corresponding to cases 4–8 are used.
    /// </summary>
    public void RandomTrajectory()
    {
        // If the queue is empty, reinitialize (reshuffle)
        if (trajectoryQueue.Count == 0)
        {
            InitializeTrajectoryQueue();
        }
        // Dequeue the next trajectory choice.
        randomChoice = trajectoryQueue.Dequeue();

        // Call the appropriate trajectory method based on the randomChoice.
        switch (randomChoice)
        {
            case 4:
                // Map 4 to FifthTrajectory()
                FifthTrajectory();
                break;
            case 5:
                // Map 5 to SixthTrajectory()
                SixthTrajectory();
                break;
            case 6:
                // Map 6 to SeventhTrajectory()
                SeventhTrajectory();
                break;
            case 7:
                // Map 7 to EightTrajectory()
                EightTrajectory();
                break;
            case 8:
                // Map 8 to NineTrajectory()
                NineTrajectory();
                break;

            default:
                Debug.LogWarning("Random trajectory choice is out of range.");
                tr_debug.text = "Random trajectory choice is out of range.";
                break;
        }
        // If the queue is empty after this call, it means we've exhausted all trajectories in this cycle.
        if (trajectoryQueue.Count == 0)
        {
            //Debug.Log("All trajectories in this cycle have been used.");
            tr_debug.text = "All trajectories finished: STOP TEST!!";
        }
        else
        {
            tr_debug.text = $"Random trajectory selected: {randomChoice+1}";
        }
    }
}
