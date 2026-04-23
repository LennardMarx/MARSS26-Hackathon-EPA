using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEngine;




public class NDIToPhantomCalibration : MonoBehaviour
{
    public string NDItoPhantomPath = "NDIToPhantomCalibration.txt";

    public List<GameObject> phantomRefs;
    private List<Vector3> phantomPts;

    public List<GameObject> marker_list;
    private List<Vector3> markerPts;
    public GameObject toolTip;
    int n_ptAcq;

    [HideInInspector]
    public Matrix4x4 calibrationMatrix;
    public bool isCalibrated = false;

    public TrackedObject trackedObj;

    private void Awake()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        // Load the calibration matrix if it exists
        if (File.Exists(NDItoPhantomPath))
        {
            LoadTransformationMatrix(NDItoPhantomPath);
            isCalibrated = true;
        }
        
    }

    // Start is called before the first frame update
    void Start()
    {
        n_ptAcq = phantomRefs.Count;
        Debug.Log($"NDI to Phantom calibration started considering {n_ptAcq} points");
        
        // Initialize the list before using it
        phantomPts = new List<Vector3>();
        marker_list = new List<GameObject>();

        // Store positions of all phantomRefs in phantomPts
        for (int i = 0; i < n_ptAcq; i++)
        {
            phantomPts.Add(phantomRefs[i].transform.position);
            Debug.Log($"Considered position for ref {i + 1} is: {phantomRefs[i].transform.position}");
        }
    }

    void Update()
    {
        
        if( isCalibrated )
        {
            
            // Get the position of the object in NDI space (x_ndi)
            Vector3 ndiPosition = trackedObj.transform.position;

            // Convert the Vector3 position to a 4D homogeneous coordinate (x_ndi) with w = 1
            Vector4 ndiPositionHomogeneous = new Vector4(ndiPosition.x, ndiPosition.y, ndiPosition.z, 1);

            // Apply the transformation using the calibration matrix
            Vector4 cadPositionHomogeneous = calibrationMatrix * ndiPositionHomogeneous;

            // Convert the result back to a Vector3 (ignoring the w component)
            Vector3 cadPosition = new Vector3(cadPositionHomogeneous.x/1000, cadPositionHomogeneous.y/1000, cadPositionHomogeneous.z/1000);

            // Set the transformed position to the GameObject (x_cad)
            trackedObj.transform.position = cadPosition;

            Debug.Log("Transformed position (CAD space): " + cadPosition);
        }
    }

    public void takeTipPosition()
    {
        Vector3 sum_pos = new Vector3(0, 0, 0);

        for (int i = 0; i < 20; i++)
        {
            sum_pos += toolTip.transform.position;
        }

        Vector3 filtered_pos = sum_pos / 20;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
        marker.GetComponent<Renderer>().material.color = Color.red;
        marker.transform.position = filtered_pos;

        marker_list.Add(marker);
        Debug.Log($"Acquired NDI point: {marker_list.Count} at position: {filtered_pos}");
    }

    public void Empty_marker_list()
    {
        if (marker_list == null || marker_list.Count == 0)
            return;

        foreach (GameObject marker in marker_list)
        {
            Destroy(marker);
        }
        marker_list.Clear();
    }

    public void RegisterNDItoPhantom()
    {
        if (marker_list.Count != phantomPts.Count)
        {
            Debug.LogError("Mismatch between marker points and phantom reference points!");
            return;
        }

        markerPts = new List<Vector3>();
        foreach (var marker in marker_list)
        {
            markerPts.Add(marker.transform.position);
        }

        //calibrationMatrix = SVD(markerPts, phantomPts);
        Debug.Log("Computed transformation matrix: " + calibrationMatrix);

        // Save the transformation matrix to file
        SaveTransformationMatrix(calibrationMatrix, NDItoPhantomPath);

        isCalibrated = true;
    }

    /// <summary>
    /// Saves the transformation matrix to a file.
    /// </summary>
    public void SaveTransformationMatrix(Matrix4x4 matrix, string path)
    {
        StreamWriter stream = new StreamWriter(path, false);

        // Serialize the matrix to a string
        string matrixData = string.Join(";",
            matrix.m00, matrix.m01, matrix.m02, matrix.m03,
            matrix.m10, matrix.m11, matrix.m12, matrix.m13,
            matrix.m20, matrix.m21, matrix.m22, matrix.m23,
            matrix.m30, matrix.m31, matrix.m32, matrix.m33
        );

        stream.Write(matrixData);
        stream.Close();
        Debug.Log("Saved calibration matrix to " + path);
    }

    /// <summary>
    /// Loads the transformation matrix from a file.
    /// </summary>
    public void LoadTransformationMatrix(string path)
    {
        if (File.Exists(path))
        {
            StreamReader stream = new StreamReader(path);
            string str = stream.ReadToEnd();
            stream.Close();

            var split = str.Split(';');
            if (split.Length == 16)
            {
                // Deserialize the matrix
                Matrix4x4 loadedMatrix = new Matrix4x4(
                    new Vector4(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3])),
                    new Vector4(float.Parse(split[4]), float.Parse(split[5]), float.Parse(split[6]), float.Parse(split[7])),
                    new Vector4(float.Parse(split[8]), float.Parse(split[9]), float.Parse(split[10]), float.Parse(split[11])),
                    new Vector4(float.Parse(split[12]), float.Parse(split[13]), float.Parse(split[14]), float.Parse(split[15]))
                );

                calibrationMatrix = loadedMatrix;
                Debug.Log("Successfully loaded calibration matrix from " + path);
                // Printing the loaded matrix
                Debug.Log("Loaded Calibration Matrix:\n" +
                          loadedMatrix.GetRow(0) + "\n" +
                          loadedMatrix.GetRow(1) + "\n" +
                          loadedMatrix.GetRow(2) + "\n" +
                          loadedMatrix.GetRow(3));
            }
            else
            {
                Debug.LogError("Invalid matrix format in file.");
            }
        }
        else
        {
            Debug.LogWarning(path + " not found.");
        }
    }
}
