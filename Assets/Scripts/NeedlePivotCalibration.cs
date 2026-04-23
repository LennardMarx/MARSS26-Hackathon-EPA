using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Globalization;


/* Calibrates Needle tip and a points along the needle (needle base).
 * Stores calibration to a text file and loads it automatically at startup.
 * 
 * Scene hierarchy:
 * Tracked marker (has this script as component
 * |- Tip
 * |- Base
 * 
 * Assign Tip and Base to this component.
 * 
 * Calibration process:
 * Tip: Use StartStopCalibrationTip(). Hold tip steady, move randomly the whole needle around the tip.
 * Base: Use StartStopCalibrationBase(). Hold one point on the needle steady. Rotate needle construct around the fixated point
 *
 */
public class NeedlePivotCalibration : MonoBehaviour
{
    /// <summary>
    /// Transform representing the tip of the needle
    /// </summary>
    public Transform Tip;

    /// <summary>
    /// Transform representing the base of the needle
    /// </summary>
    public Transform Base;

    /// <summary>
    /// Transform representing the CIR of the needle - in case the base and the tip are not aligned
    /// </summary>
    public Transform Cir;

    /// <summary>
    /// Duration of the pose acquisition time during the calibration phase
    /// </summary>
    public float secondsOfAcquisition = 10f;

    public string pathTip = "NeedleTipCalibration.txt";
    public string pathBase = "NeedleBaseCalibration.txt";

    private bool IsCollectingPoses = false;

    public class MarkerTransform
    {
        public Vector3 position;
        public Quaternion rotation;

        /// <summary>
        /// Constructor for MarkerTransform
        /// </summary>
        /// <param name="_position">Position of the marker</param>
        /// <param name="_rotation">Rotation of the marker</param>
        public MarkerTransform(Vector3 _position, Quaternion _rotation)
        {
            position = _position;
            rotation = _rotation;
        }
    }


    private List<MarkerTransform> markerPoses;
    private Vector3 p_tcs;

    private void Awake()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        LoadCalibration(ref Tip, pathTip);
        LoadCalibration(ref Base, pathBase);
    }

    void Update()
    {
        if (IsCollectingPoses) CollectMarkerPose();
    }

    // void Start()
    // {
        // here you can start the calibration
    // }
    /// <summary>
    /// Starts or stops the calibration process for the base of the needle
    /// </summary>
    public void StartStopCalibrationBase()
    {
        if (!IsCollectingPoses)
        {
            markerPoses = new List<MarkerTransform>();
            IsCollectingPoses = true;
            Debug.Log("Started Base Pivot Calibration");
        }
        else
        {
            IsCollectingPoses = false;
            CalculatePivot();
            Base.localPosition = p_tcs;
            SaveCalibration(p_tcs, pathBase);
        }

    }

    /// <summary>
    /// Starts or stops the calibration process for the tip of the needle for N seconds
    /// </summary>
    public void StartStopCalibrationTip()
    {
        if (!IsCollectingPoses)
        {
            markerPoses = new List<MarkerTransform>();
            IsCollectingPoses = true;
            Debug.Log("Started Tip Pivot Calibration");

            // Stop automatically after 5 seconds
            StartCoroutine(StopCalibrationAfterTime(secondsOfAcquisition));
        }
    }

    IEnumerator StopCalibrationAfterTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopCalibration();
    }

    void StopCalibration()
    {
        IsCollectingPoses = false;
        CalculatePivot();
        Tip.localPosition = p_tcs;
        SaveCalibration(p_tcs, pathTip);

        CreateCalibrationCylinder();

        Debug.Log("Tip Calibration Finished");
    }
    /*public void StartStopCalibrationTip()
{
    if (!IsCollectingPoses)
    {
        markerPoses = new List<MarkerTransform>();
        IsCollectingPoses = true;
        Debug.Log("Started Tip Pivot Calibration");
    }
    else
    {
        IsCollectingPoses = false;
        CalculatePivot();
        Tip.localPosition = p_tcs;
        SaveCalibration(p_tcs, pathTip);
    }
}*/

    /// <summary>
    /// Calculates the pivot point from the collected marker poses
    /// </summary>
    void CalculatePivot()
    {

        float[,] x = { { -markerPoses[0].position.x }, { -markerPoses[0].position.y }, { -markerPoses[0].position.z } };
        Matrix<float> b = Matrix<float>.Build.DenseOfArray(x);
        Matrix<float> r = GetMatrixFromRotation(markerPoses[0].rotation);
        var I = Matrix<float>.Build.DenseDiagonal(3, 3, -1.0f);

        var A = r.Append(I);

        for (int i = 1; i < markerPoses.Count; i++)
        {
            float[,] tmp = { { -markerPoses[i].position.x }, { -markerPoses[i].position.y }, { -markerPoses[i].position.z } };
            var t = Matrix<float>.Build.DenseOfArray(tmp);
            b = b.Stack(t);
            Matrix<float> A_tmp = GetMatrixFromRotation(markerPoses[i].rotation).Append(I);
            A = A.Stack(A_tmp);
        }

        var sol = A.Solve(b);

        p_tcs = new Vector3(sol[0, 0], sol[1, 0], sol[2, 0]);


    }

    /// <summary>
    /// Converts the rotation from a quaternion to a Matrix4x4
    /// </summary>
    Matrix<float> GetMatrixFromRotation(Quaternion rotation)
    {
        Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);

        float[,] x = { { m[0, 0], m[0, 1], m[0, 2] },
                    { m[1, 0], m[1, 1], m[1, 2] },
                    { m[2, 0], m[2, 1], m[2, 2] } };
        Matrix<float> r = Matrix<float>.Build.DenseOfArray(x);

        return r;
    }

    /// <summary>
    /// Collects the current marker pose
    /// </summary>
    void CollectMarkerPose()
    {
        MarkerTransform markerPose = new MarkerTransform(transform.position, transform.rotation);
        markerPoses.Add(markerPose);
    }

    /// <summary>
    /// Saves the calibration to a file
    /// </summary>
    /// <param name="localposition">The position to be saved</param>
    /// <param name="path">Path to the file</param>
    /// <returns>Success status of the save operation</returns>
    public bool SaveCalibration(Vector3 localposition, string path)
    {

        StreamWriter stream = new StreamWriter(path, false);

        string data = localposition.x + ";" + localposition.y + ";" + localposition.z;

        stream.Write(data);

        stream.Close();

        Debug.Log("Saved needle calibration to " + path);

        return true;
    }


    /// <summary>
    /// Loads the calibration from a file
    /// </summary>
    /// <param name="t">Transform to load the calibration into</param>
    /// <param name="path">Path to the file</param>
    public void LoadCalibration(ref Transform t, string path)
    {
        if (File.Exists(path))
        {

            StreamReader stream = new StreamReader(path);

            string str = stream.ReadToEnd();

            stream.Close();

            var split = str.Split(';');
            if (split.Length >= 3)
            {
                Vector3 position = new Vector3(float.Parse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture),
                    float.Parse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture),
                    float.Parse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture));
                t.localPosition = position;
                Debug.Log("Succesfully loaded calibration from " + path);
                CreateCalibrationCylinder();
            }
            else
            {
                Debug.Log("Error parsing calibration from " + path);
            }

        }
        else
        {
            Debug.LogWarning(path + " not found.");
            return;
        }
    }

    /// <summary>
    /// Creates a cylinder between the Cir and the Tip with a local reference frame
    /// </summary>
    void CreateCalibrationCylinder()
    {
        if (Cir == null || Tip == null)
        {
            Debug.LogError("Cir or Tip is not assigned.");
            return;
        }

        // Create a new GameObject for the cylinder
        GameObject tool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tool.name = "Tool";
        // Parent it to the current GameObject for organization
        tool.transform.parent = this.transform;

        // Compute the midpoint between Cir and Tip
        Vector3 startPos = Cir.position;
        Vector3 endPos = Tip.position;
        Vector3 midPoint = (startPos + endPos) / 2f;

        // Set cylinder position to the midpoint
        tool.transform.position = midPoint;

        // Compute direction and rotation
        Vector3 direction = (endPos - startPos).normalized;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        tool.transform.rotation = rotation;

        // Scale cylinder to match distance between Cir and Tip
        float distance = Vector3.Distance(startPos, endPos);
        tool.transform.localScale = new Vector3(0.01f, distance / 2f, 0.01f); // Thin cylinder

        // Optional: Set color or material
        Renderer rend = tool.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Standard"));
        rend.material.color = Color.black; // Set cylinder to red

        Debug.Log("Cylinder created between Cir and Tip.");
    }
}