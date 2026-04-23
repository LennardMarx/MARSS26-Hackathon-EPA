using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

using Debug = UnityEngine.Debug;

public class SpatialMappingModel : MonoBehaviour
{
    //public TextMesh textConsole;
    public Text textConsole;
    public GameObject trackedTool;
    public GameObject phantomSpace;
    public Transform Cir;

    public int nDofs;

    [HideInInspector]
    public Transform tool;
    [HideInInspector]
    public Transform tip;

    public Toggle toolAlignamentToggle;
    public Toggle towardHeartToggle;

    private Action alignmentFunction; // callback for the alignment function
    // Reference to the MeshAnimator component
    private MeshAnimator meshAnimator;
    [HideInInspector]
    public FrameData dynamicData;
    private GameObject currentBlueSphere;
    private GameObject currentWhiteSphere;
    private float pericardiumDistanceThreshold, heartDistanceThreshold;

    [HideInInspector]
    public float lastAlignmentDistanceMm, lastAlignmentRotationError, lastTargetPeriMm, lastTargetHeartMm, toolAlignmentDuration, toTargetReachingDuration;
    [HideInInspector]
    public int heartTouchCount = 0;
    public List<Vector3> heartTouchPositions = new List<Vector3>(), pericardiumTouchPositions = new List<Vector3>();


    [HideInInspector]
    public bool isAlignmentActive, isToTargetActive;
    // time
    private float alignmentStartTime, toTargetStartTime;
    private bool isAlignmentRunning = false, isToTargetRunning = false;



    void Start()
    {
        if (toolAlignamentToggle != null)
        {
            toolAlignamentToggle.onValueChanged.AddListener(ToggleAlignment);
        }
        else
        {
            Debug.LogWarning("Tool Alignament Toggle is not assigned in the inspector!");
        }

        if (towardHeartToggle != null)
        {
            towardHeartToggle.onValueChanged.AddListener(ToggleToTarget);
        }
        else
        {
            Debug.LogWarning("Towards the Heart Toggle is not assigned in the inspector!");
        }


        if (nDofs == 2)
        {
            alignmentFunction = ToolAlignament2Dofs;
        }
        else if (nDofs == 4)
        {
            alignmentFunction = ToolAlignament4Dofs;
        }
        else
        {
            Debug.LogError("Invalid nDofs value. Must be 2 or 4.");
        }

        // initialization: look for GameObjects 1. tool (trackedTool) 2. planned trajectory (phantomSpace)
        if (trackedTool == null || phantomSpace == null)
        {
            Debug.LogError("Tracked tool or phantom space is not assigned!");
            return;
        }
        lastAlignmentRotationError = 0.0f;
        lastAlignmentDistanceMm = 0.0f;
        lastTargetHeartMm = 0.0f;
        lastTargetPeriMm = 0.0f;

        // SURGICAL TOOL SPACE
        tool = trackedTool.transform.Find("Tool"); // used for rotation misalignament
        tip = trackedTool.transform.Find("Tip");    // use for entry point misalignament

        if (tool == null)
        {
            Debug.LogError("Tool not found in Tracked Space!");
            return;
        }

        if (tip == null)
        {
            Debug.LogError("Tip not found in Tracked Space!");
            return;
        }

        // when dynamic data
        meshAnimator = phantomSpace.GetComponentInChildren<MeshAnimator>();
        if (meshAnimator == null)
        {
            Debug.LogError("MeshAnimator component not found on phantomSpace!");
        }
        else
        {
            Debug.Log("MeshAnimator correctly loaded.");
        }    
    }


    void Update()
    {
        if (isAlignmentActive && alignmentFunction != null)
        {
            alignmentFunction();
            
        }
        else if (isToTargetActive)
        {   // Update dynamicData from MeshAnimator every frame.
            if (meshAnimator != null)
            {
                dynamicData = meshAnimator.CurrentFrame;
            }
            ToTargetReaching();
        }
    }

    public void ToggleAlignment(bool isActive) // this function has to be set on the toggle button
    {
        Debug.Log("tool alignament flag:" + isActive);
        isAlignmentActive = isActive;
        isToTargetActive = false;
        towardHeartToggle.isOn = false;

        if (isActive)
        {
            alignmentStartTime = Time.time; // Start timing
            isToTargetRunning = true;
        }
        else if (isToTargetRunning) // When turned off, calculate duration
        {
            toolAlignmentDuration = Time.time - alignmentStartTime; // Capture this session's duration
            Debug.Log($"ToTargetReaching session duration: {toolAlignmentDuration} seconds");
            isToTargetRunning = false;
        }
    }

    public void ToggleToTarget(bool isActive) // this function has to be set on the toggle button
    {
        Debug.Log("towards heart flag: "+ isActive);
        isToTargetActive = isActive;
        
        isAlignmentActive = false;
        toolAlignamentToggle.isOn = false;
        
        if (isActive)
        {
            heartTouchCount = 0;
            toTargetStartTime = Time.time; // Start timing
            isToTargetRunning = true;
        }
        else if (isToTargetRunning) // When turned off, calculate duration
        {
            toTargetReachingDuration = Time.time - toTargetStartTime;
            Debug.Log($"ToTargetReaching session duration: {toTargetReachingDuration} seconds");
            isToTargetRunning = false;
        }
    }

    public void ToolAlignament2Dofs()
    {
        
        // PLANNING SPACE
        Transform trajectoryUtils = phantomSpace.transform.Find("TrajectoryUtils");
        Transform entryPoint = trajectoryUtils.transform.Find("start");
        if (entryPoint == null)
        {
            Debug.LogError("Trajectory Entry Point not found in Phantom Space!");
            return;
        }
        Transform plannedTrajectory = phantomSpace.transform.Find("PlannedTrajectory");
        if (plannedTrajectory == null)
        {
            Debug.LogError("PlannedTrajectory not found in Phantom Space!");
            return;
        }

        // euclidean distance computed between planned entry point and tool tip
        //float distance = Vector3.Distance(tip.position, entryPoint.position);
        // euclidean distance computed between planned entry point and tool tip
        float distanceX = Mathf.Abs(tip.position.x - entryPoint.position.x);
        float distanceZ = Mathf.Abs(tip.position.z - entryPoint.position.z);
        float distance = distanceX + distanceZ; // without depth

        // angle misalignament
        float alignmentError = Vector3.Angle(tool.up, plannedTrajectory.up);


        // results: displayed in the text console and as a Log message
        string result = $"Entry Point Error: {distance*1000f} mm\n" +
                        $"Rotation Error: {alignmentError:F2}°";

        lastAlignmentDistanceMm = distance * 1000f; // mm
        lastAlignmentRotationError = alignmentError;

        Debug.Log(result);
        if (textConsole != null)
        {
            textConsole.text = result;
        }
    }

    public void ToolAlignament4Dofs()
    {
        // PLANNING SPACE
        Transform trajectoryUtils = phantomSpace.transform.Find("TrajectoryUtils");
        Transform entryPoint = trajectoryUtils.transform.Find("start");
        if (entryPoint == null)
        {
            Debug.LogError("Trajectory Entry Point not found in Phantom Space!");
            return;
        }
        Transform plannedTrajectory = phantomSpace.transform.Find("PlannedTrajectory");
        if (plannedTrajectory == null)
        {
            Debug.LogError("PlannedTrajectory not found in Phantom Space!");
            return;
        }

        // euclidean distance computed between planned entry point and tool tip
        float distanceX = Mathf.Abs(tip.position.x - entryPoint.position.x);
        float distanceZ = Mathf.Abs(tip.position.z - entryPoint.position.z);
        Debug.Log($"tip pos.x : {tip.position.x}; start pos.x: {entryPoint.position.x}");


        // rotational misalignment: two angles -- Azimuthal (phi - Yaw Error): rotation around the Global Y Axis -- Elevation (tetha - Pitch Error): rotation around the Global Z Axis
        Vector3 toolYAxis = tool.up;  // Tool's Y-axis
        Vector3 plannedYAxis = plannedTrajectory.up; // Planned Y-axis
        float yawError = Mathf.Abs(Vector3.SignedAngle(new Vector3(toolYAxis.x, 0, toolYAxis.z),
                                             new Vector3(plannedYAxis.x, 0, plannedYAxis.z),
                                             Vector3.up));

        float pitchError = Mathf.Abs(Vector3.SignedAngle(new Vector3(0, toolYAxis.y, toolYAxis.z),
                                               new Vector3(0, plannedYAxis.y, plannedYAxis.z),
                                               Vector3.right));

        // Format output
        string result = $"Entry Point Error:\n\t R-L: {distanceX * 1000:F2} mm\t SUP-INF: {distanceZ * 1000:F2} mm\n" +
                        $"Azimuthal (horizontal) Error: {yawError:F2}°\n" +
                        $"Elevation (vertical) Error: {pitchError:F2}°";

        Debug.Log(result);
        if (textConsole != null)
        {
            textConsole.text = result;
        }
    }

    /*
    public void ToTargetReaching()
    {
        
        // PLANNING SPACE
        Transform trajectoryUtils = phantomSpace.transform.Find("TrajectoryUtils");
        Transform targetPoint = trajectoryUtils.transform.Find("end");
        if (targetPoint == null)
        {
            Debug.LogError("Trajectory Target Point not found in Phantom Space!");
            return;
        }
        Transform plannedTrajectory = phantomSpace.transform.Find("PlannedTrajectory");
        if (plannedTrajectory == null)
        {
            Debug.LogError("PlannedTrajectory not found in Phantom Space!");
            return;
        }

        // euclidean distance computed between planned entry point and tool tip
        float distance = Vector3.Distance(tip.position, targetPoint.position);
        float distanceX = Mathf.Abs(tip.position.x - targetPoint.position.x);
        float distanceY = Mathf.Abs(tip.position.y - targetPoint.position.y);
        float distanceZ = Mathf.Abs(tip.position.z - targetPoint.position.z);

        // angle misalignament
        float alignmentError = Vector3.Angle(tool.up, plannedTrajectory.up);


        // results: displayed in the text console and as a Log message
        string result = $"Target Point Error:\n\teuclidean distance: {distance * 1000} mm\n" +
            $"\ton ML : {distanceX*1000} mm\n"+
            $"\ton IS : {distanceZ*1000} mm\n" +
            $"\ton Depth : {distanceY*1000} mm" +
            "\n"+
                        $"Rotation Error:\n\t{alignmentError:F2}°";

        Debug.Log(result);
        if (textConsole != null)
        {
            textConsole.text = result;
        }
    }

    public void ToTargetReaching()
    {
        if (tip == null)
        {
            Debug.LogWarning("Tip is not set!");
            return;
        }
        if (dynamicData == null || dynamicData.pericardium == null)
        {
            Debug.LogWarning("Dynamic data or pericardium is not set!");
            return;
        }

        // Compute the tool's local Y axis in world space.
        Vector3 localY = tool.TransformDirection(Vector3.up);
        // Optionally, draw the ray for debugging (red line, 0.1m long, lasts 2 seconds)
        Debug.DrawRay(tip.position, localY * 0.1f, Color.red, 2f);

        // Set up the ray starting at tip.position along the computed local Y direction.
        Ray ray = new Ray(tip.position, localY);
        RaycastHit hit;
        float maxDistance = 100f; // Adjust as needed

        // Perform the raycast.
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            // Check if the hit collider belongs to the pericardium.
            if (hit.collider.gameObject == dynamicData.pericardium)
            {
                float distance = Vector3.Distance(tip.position, hit.point);
                string result = $"Distance between Tip and Pericardium: {distance * 1000:F2} mm";
                Debug.Log(result);
                if (textConsole != null)
                {
                    textConsole.text = result;
                }

                // Instantiate a sphere at the hit point.
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = hit.point;
                sphere.transform.localScale = Vector3.one * 0.005f; // Adjust scale as needed

                // Optionally disable the sphere's collider.
                Collider sphereCollider = sphere.GetComponent<Collider>();
                if (sphereCollider != null)
                {
                    sphereCollider.enabled = false;
                }
            }
            else
            {
                Debug.LogWarning("Raycast hit an object that is not the pericardium.");
            }
        }
        else
        {
            Debug.LogWarning("Raycast did not hit any collider.");
        }
    }*/
    /*
    public void ToTargetReaching()
    {
        if (tip == null)
        {
            Debug.LogWarning("Tip is not set!");
            return;
        }
        if (dynamicData == null || dynamicData.pericardium == null || dynamicData.heart == null)
        {
            Debug.LogWarning("Dynamic data is incomplete! (pericardium or heart is missing)");
            return;
        }

        // Get the tool's local Y axis in world space.
        Vector3 rayDirection = tool.TransformDirection(Vector3.up);

        // Create a ray from the tip along the tool's local Y.
        Ray ray = new Ray(Cir.position, rayDirection);
        float maxDistance = 500f; // Adjust as needed.

        // Get all intersections.
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        if (hits.Length == 0)
        {
            Debug.LogWarning("Raycast did not hit any colliders.");
            return;
        }

        // Sort hits by distance.
        Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        // We assume that the skin collider may be hit first.
        // We only care about pericardium and heart.
        RaycastHit? pericardiumHit = null;
        RaycastHit? heartHit = null;

        foreach (RaycastHit hit in hits)
        {
            // Ignore skin by checking if the hit object is not one of our target objects.
            if (hit.collider.gameObject == dynamicData.pericardium && !pericardiumHit.HasValue)
            {
                pericardiumHit = hit;
            }
            else if (hit.collider.gameObject == dynamicData.heart && !heartHit.HasValue)
            {
                heartHit = hit;
            }
        }

        // Handle pericardium intersection.
        if (pericardiumHit.HasValue)
        {
            Debug.Log("Pericardium intersected.");

            // Destroy the previous white sphere, if any.
            if (currentWhiteSphere != null)
            {
                Destroy(currentWhiteSphere);
            }

            // Instantiate a white sphere at the pericardium intersection.
            currentWhiteSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentWhiteSphere.transform.position = pericardiumHit.Value.point;
            currentWhiteSphere.transform.localScale = Vector3.one * 0.003f; // Adjust scale as needed
        }
        else
        {
            Debug.LogWarning("No intersection with pericardium was found.");
            lastTargetPeriMm = 100f;
            lastTargetHeartMm = 100f;
        }

        // Handle heart intersection.
        if (heartHit.HasValue)
        {
            Debug.Log("Heart intersected.");

            // Destroy the previous blue sphere, if any.
            if (currentBlueSphere != null)
            {
                Destroy(currentBlueSphere);
            }

            // Instantiate a blue sphere at the heart intersection.
            currentBlueSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentBlueSphere.transform.position = heartHit.Value.point;
            currentBlueSphere.transform.localScale = Vector3.one * 0.004f; // Adjust scale as needed

            // Change its color to blue.
            Renderer rend = currentBlueSphere.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.blue;
            }
        }
        else
        {
            Debug.LogWarning("No intersection with heart was found.");
            lastTargetPeriMm = 100f;
            lastTargetHeartMm = 100f;
        }

        // If both intersections exist, compute the distance between them.
        if (pericardiumHit.HasValue && heartHit.HasValue)
        {
            //float distanceTipToPeri = Vector3.Distance(tip.position, pericardiumHit.Value.point);
            float signedDistanceTipToPeri = Vector3.Dot(pericardiumHit.Value.point - tip.position, rayDirection);
            //float distanceTipToHeart = Vector3.Distance(tip.position, heartHit.Value.point);
            float signedDistanceTipToHeart = Vector3.Dot(heartHit.Value.point - tip.position, rayDirection);

            lastTargetPeriMm = signedDistanceTipToPeri * 1000f;
            lastTargetHeartMm = signedDistanceTipToHeart * 1000f;

            string result = "Distance between Tip and Pericardium: " + (signedDistanceTipToPeri * 1000f).ToString("F2") + " mm\n" +
                            "Distance between Tip and Heart: " + (signedDistanceTipToHeart * 1000f).ToString("F2") + " mm";
            if (textConsole != null)
            {
                textConsole.text = result;
            }
        }
    }*/

    public void ToTargetReaching()
    {
        //heartTouchCount = 0;
        //heartTouchPositions.Clear();
        //pericardiumTouchPositions.Clear();

        if (tip == null)
        {
            Debug.LogWarning("Tip is not set!");
            return;
        }
        if (dynamicData == null || dynamicData.pericardium == null || dynamicData.heart == null)
        {
            Debug.LogWarning("Dynamic data is incomplete! (pericardium or heart is missing)");
            return;
        }

        // Get the tool's local Y axis in world space.
        Vector3 rayDirection = tool.TransformDirection(Vector3.up);

        // Create a ray from the tip along the tool's local Y.
        Ray ray = new Ray(Cir.position, rayDirection);
        float maxDistance = 500f; // Adjust as needed.

        // Get all intersections.
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        if (hits.Length == 0)
        {
            Debug.LogWarning("Raycast did not hit any colliders.");
            return;
        }

        // Sort hits by distance.
        Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        // We assume that the skin collider may be hit first.
        // We only care about pericardium and heart.
        RaycastHit? pericardiumHit = null;
        RaycastHit? heartHit = null;

        foreach (RaycastHit hit in hits)
        {
            // Ignore skin by checking if the hit object is not one of our target objects.
            if (hit.collider.gameObject == dynamicData.pericardium && !pericardiumHit.HasValue)
            {
                pericardiumHit = hit;
            }
            else if (hit.collider.gameObject == dynamicData.heart && !heartHit.HasValue)
            {
                heartHit = hit;
            }
        }

        // Handle pericardium intersection.
        if (pericardiumHit.HasValue)
        {
            Debug.Log("Pericardium intersected.");

            // Destroy the previous white sphere, if any.
            if (currentWhiteSphere != null)
            {
                Destroy(currentWhiteSphere);
            }

            // Instantiate a white sphere at the pericardium intersection.
            currentWhiteSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentWhiteSphere.transform.position = pericardiumHit.Value.point;
            currentWhiteSphere.transform.localScale = Vector3.one * 0.003f; // Adjust scale as needed
        }
        else
        {
            Debug.LogWarning("No intersection with pericardium was found.");
            lastTargetPeriMm = 100f;
            lastTargetHeartMm = 100f;
        }

        // Handle heart intersection.
        if (heartHit.HasValue)
        {
            Debug.Log("Heart intersected.");

            // Destroy the previous blue sphere, if any.
            if (currentBlueSphere != null)
            {
                Destroy(currentBlueSphere);
            }

            // Instantiate a blue sphere at the heart intersection.
            currentBlueSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentBlueSphere.transform.position = heartHit.Value.point;
            currentBlueSphere.transform.localScale = Vector3.one * 0.004f; // Adjust scale as needed

            // Change its color to blue.
            Renderer rend = currentBlueSphere.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.blue;
            }
        }
        else
        {
            Debug.LogWarning("No intersection with heart was found.");
            lastTargetPeriMm = 100f;
            lastTargetHeartMm = 100f;
        }

        // If both intersections exist, compute the distance between them.
        if (pericardiumHit.HasValue && heartHit.HasValue)
        {
            float signedDistanceTipToPeri = Vector3.Dot(pericardiumHit.Value.point - tip.position, rayDirection);
            float signedDistanceTipToHeart = Vector3.Dot(heartHit.Value.point - tip.position, rayDirection);

            lastTargetPeriMm = signedDistanceTipToPeri * 1000f;
            lastTargetHeartMm = signedDistanceTipToHeart * 1000f;

            // Increment counter if tip touches heart (lastTargetHeartMm == 0)
            if (lastTargetHeartMm <= 0.01f)
            {
                heartTouchCount++;

                //heartTouchPositions.Add(heartHit.Value.point);
                Debug.Log($"Tip touched the heart!& count: {heartTouchCount}");
            }
            /*
            if (lastTargetPeriMm <= 0.5f)
            {
                pericardiumTouchPositions.Add(pericardiumHit.Value.point);
                Debug.Log("Tip touched the Pericardium!");
            }*/

            string result = "Distance between Tip and Pericardium: " + (signedDistanceTipToPeri * 1000f).ToString("F2") + " mm\n" +
                            "Distance between Tip and Heart: " + (signedDistanceTipToHeart * 1000f).ToString("F2") + " mm\n";

            if (textConsole != null)
            {
                textConsole.text = result;
            }
        }
    }


}
