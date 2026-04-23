using System;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class Kabsch : MonoBehaviour
{
    //public Transform[] inPoints;
    public Transform[] referencePoints;
    Vector3[] points; Vector4[] refPoints;
    KabschSolver solver = new KabschSolver();

    Matrix<float> R, t;
    int n_ptAcq = 8;

    public Transform output;
    [HideInInspector]
    public Matrix4x4 MTransform;
    public string NDItoPhantomPath = "KabschCalibration.txt";

    // dynamic point inPoints collection
    public GameObject toolTip;
    public GameObject inParentSpace;
    public List<Transform> inPoints = new List<Transform>(); // dynamic size
    [HideInInspector]
    public List<GameObject> marker_list;

    //public Transform parentTarget;
    private void Awake()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        // Load the calibration matrix if it exists
        if (File.Exists(NDItoPhantomPath))
        {
            LoadTransformationMatrix(NDItoPhantomPath);
            if (output != null)
            {
                output.rotation = MatrixMath.RotationFromMatrix(MTransform);
                output.position = MatrixMath.PositionFromMatrix(MTransform);
            }
            else
            {
                Debug.LogError("Output transform is not assigned!");
            }
        }

    }

    void Start()
    {
        int n_ptAcq = referencePoints.Length;
        Debug.Log($"NDI to Phantom calibration started considering {n_ptAcq} points");

        // Store positions of all phantomRefs in phantomPts
        for (int i = 0; i < n_ptAcq; i++)
        {
            Debug.Log($"Considered position for Phantom ref {i + 1} is: {referencePoints[i].position.x}, {referencePoints[i].position.y}, {referencePoints[i].position.z}");
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

        marker.transform.parent = inParentSpace.transform;

        marker_list.Add(marker);
        inPoints.Add(marker.transform);
        Debug.Log($"Acquired NDI point: {inPoints.Count} at position: {filtered_pos}");
    }

    public void Empty_marker_list()
    {
        if (marker_list == null || marker_list.Count == 0)
        {
            Debug.Log("No acquired NDI points to empty");
        }
        

        foreach (GameObject marker in marker_list)
        {
            Destroy(marker);
        }
        marker_list.Clear();
        inPoints.Clear();
        Debug.Log("NDI acquired points all cleared.");
    }

    public void Register()
    {
        align(); // first: initialization
        //align();

        //compute RMSE 
    }

    public void align()
    {

        if (inPoints.Count != referencePoints.Length)
        {
            Debug.LogError("Mismatch between marker points and phantom reference points!");
            return;
        }

        points = new Vector3[inPoints.Count];
        refPoints = new Vector4[referencePoints.Length];
        for (int i = 0; i < inPoints.Count; i++)
        {
            points[i] = inPoints[i].localPosition;
        }

        for (int i = 0; i < referencePoints.Length; i++)
        {
            refPoints[i] = new Vector4(referencePoints[i].position.x, referencePoints[i].position.y, referencePoints[i].position.z, referencePoints[i].localScale.x);
        }

        Vector3[] refPointsAsVector3 = new Vector3[referencePoints.Length];
        for (int i = 0; i < referencePoints.Length; i++)
        {
            refPointsAsVector3[i] = new Vector3(refPoints[i].x, refPoints[i].y, refPoints[i].z);
        }


        //MTransform = solver.SolveKabsch(points, refPoints);
        MTransform = SVD(points, refPointsAsVector3);


        Debug.Log("Computed transformation matrix: " + MTransform);

        // Save the transformation matrix to filekabschTransform
        SaveTransformationMatrix(MTransform, NDItoPhantomPath);

        output.rotation = MatrixMath.RotationFromMatrix(MTransform);
        output.position = MatrixMath.PositionFromMatrix(MTransform);
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
                // Convert to float array
                float[] values = Array.ConvertAll(split, float.Parse);

                // Manually transpose the matrix when loading
                Matrix4x4 loadedMatrix = new Matrix4x4(
                    new Vector4(values[0], values[4], values[8], values[12]),  // First column
                    new Vector4(values[1], values[5], values[9], values[13]),  // Second column
                    new Vector4(values[2], values[6], values[10], values[14]), // Third column
                    new Vector4(values[3], values[7], values[11], values[15])  // Fourth column
                );

                MTransform = loadedMatrix;
                Debug.Log("Successfully loaded calibration matrix from " + path);

                // Printing the corrected matrix
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

    public Matrix4x4 SVD(Vector3[] posPt, Vector3[] HoloPts) //T_tool_CT
    {
        //Compute centroid from acquired pt in tool system
        var centroidtool = new Vector3(0, 0, 0);
        var numPoints = posPt.Length;
        foreach (Vector3 point in posPt)
        {
            centroidtool += point;
        }
        centroidtool /= numPoints;



        //compute centroid of the same points on the holograpihc face
        var centroidHoloCT = new Vector3(0, 0, 0);
        foreach (Vector3 pointHolo in HoloPts)
        {
            centroidHoloCT += pointHolo;
        }
        centroidHoloCT /= n_ptAcq;


        //matrix with position of the 4points on the face
        Matrix<float> mHolo = Matrix<float>.Build.DenseOfColumnArrays(
        new[] { HoloPts[0].x, HoloPts[0].y, HoloPts[0].z },
        new[] { HoloPts[1].x, HoloPts[1].y, HoloPts[1].z },
        new[] { HoloPts[2].x, HoloPts[2].y, HoloPts[2].z },
        new[] { HoloPts[3].x, HoloPts[3].y, HoloPts[3].z },
        new[] { HoloPts[4].x, HoloPts[4].y, HoloPts[4].z },
        new[] { HoloPts[5].x, HoloPts[5].y, HoloPts[5].z },
        new[] { HoloPts[6].x, HoloPts[6].y, HoloPts[6].z },
        new[] { HoloPts[7].x, HoloPts[7].y, HoloPts[7].z }
        );



        Debug.Log("n_ptAcq punti holo Wcoord: " + mHolo);
        Debug.Log("centroide holo Wcoord: " + centroidHoloCT);

        Matrix<float> centrHolo = Matrix<float>.Build.DenseOfColumnArrays(
            new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z },
            new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z },
            new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z },
            new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z },
            new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z },
            new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z },
            new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z },
            new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z }
            );

        var HoloCenteredData = mHolo.Subtract(centrHolo);
        Debug.Log("4punti holo centrati Wcoord: " + HoloCenteredData);


        ////matrix with position of the 4points acquired with the tracked tool
        Matrix<float> mtool = Matrix<float>.Build.DenseOfColumnArrays(
        new[] { posPt[0].x, posPt[0].y, posPt[0].z },
        new[] { posPt[1].x, posPt[1].y, posPt[1].z },
        new[] { posPt[2].x, posPt[2].y, posPt[2].z },
        new[] { posPt[3].x, posPt[3].y, posPt[3].z },
        new[] { posPt[4].x, posPt[4].y, posPt[4].z },
        new[] { posPt[5].x, posPt[5].y, posPt[5].z },
        new[] { posPt[6].x, posPt[6].y, posPt[6].z },
        new[] { posPt[7].x, posPt[7].y, posPt[7].z }
        );



        Matrix<float> centrTool = Matrix<float>.Build.DenseOfColumnArrays(
            new[] { centroidtool.x, centroidtool.y, centroidtool.z },
            new[] { centroidtool.x, centroidtool.y, centroidtool.z },
            new[] { centroidtool.x, centroidtool.y, centroidtool.z },
            new[] { centroidtool.x, centroidtool.y, centroidtool.z },
            new[] { centroidtool.x, centroidtool.y, centroidtool.z },
            new[] { centroidtool.x, centroidtool.y, centroidtool.z },
            new[] { centroidtool.x, centroidtool.y, centroidtool.z },
            new[] { centroidtool.x, centroidtool.y, centroidtool.z }
            );

        var ToolCenteredData = mtool.Subtract(centrTool);
        Debug.Log("8pti tool : " + mtool);
        Debug.Log("centroide tool" + centrTool);
        Debug.Log("dati centrati tool" + ToolCenteredData);

        //var H = HoloCenteredData * AuriCenteredData.Transpose();
        var H = ToolCenteredData * HoloCenteredData.Transpose();

        Debug.Log(H);
        var svd = H.Svd(true);

        R = svd.VT.Transpose() * svd.U.Transpose();
        Debug.Log("R before is: " + R);
        //Debug.Log("U is: " + svd.U);
        //Debug.Log("VT is: " + svd.VT);

        if (R.Determinant() < 0)
        {
            var svdNew = R.Svd(true);
            for (int ir = 0; ir < svdNew.VT.RowCount; ir++)
            {
                svdNew.VT[ir, 2] = svdNew.VT[ir, 2] * (-1);
            }
            R = svdNew.VT.Transpose() * svdNew.U.Transpose();
            Debug.Log("R after is: " + R);
        }


        Matrix<float> centrAuri2 = Matrix<float>.Build.DenseOfColumnArrays(new[] { centroidtool.x, centroidtool.y, centroidtool.z });
        Matrix<float> centrHolo2 = Matrix<float>.Build.DenseOfColumnArrays(new[] { centroidHoloCT.x, centroidHoloCT.y, centroidHoloCT.z });

        t = centrHolo2 - R * centrAuri2;
        Debug.Log("#t final is : " + t);
        Debug.Log("R final is : " + R);


        Matrix4x4 TransfMatrix = new Matrix4x4();
        TransfMatrix.m00 = R[0, 0];
        TransfMatrix.m01 = R[0, 1];
        TransfMatrix.m02 = R[0, 2];
        TransfMatrix.m03 = t[0, 0];
        TransfMatrix.m10 = R[1, 0];
        TransfMatrix.m11 = R[1, 1];
        TransfMatrix.m12 = R[1, 2];
        TransfMatrix.m13 = t[1, 0];
        TransfMatrix.m20 = R[2, 0];
        TransfMatrix.m21 = R[2, 1];
        TransfMatrix.m22 = R[2, 2];
        TransfMatrix.m23 = t[2, 0];
        TransfMatrix.m30 = 0f;
        TransfMatrix.m31 = 0f;
        TransfMatrix.m32 = 0f;
        TransfMatrix.m33 = 1f;

        return TransfMatrix;
    }


    //Kabsch Implementation: https://github.com/zalo/MathUtilities/blob/master/Assets/Kabsch/Kabsch.cs
    public class KabschSolver
    {
        Vector3[] QuatBasis = new Vector3[3];
        Vector3[] DataCovariance = new Vector3[3];
        Quaternion OptimalRotation = Quaternion.identity;
        public float scaleRatio = 1f;
        public Matrix4x4 SolveKabsch(Vector3[] inPoints, Vector4[] refPoints, bool solveRotation = true, bool solveScale = false)
        {
            if (inPoints.Length != refPoints.Length) { return Matrix4x4.identity; }

            //Calculate the centroid offset and construct the centroid-shifted point matrices
            Vector3 inCentroid = Vector3.zero; Vector3 refCentroid = Vector3.zero;
            float inTotal = 0f, refTotal = 0f;
            for (int i = 0; i < inPoints.Length; i++)
            {
                inCentroid += new Vector3(inPoints[i].x, inPoints[i].y, inPoints[i].z) * refPoints[i].w;
                inTotal += refPoints[i].w;
                refCentroid += new Vector3(refPoints[i].x, refPoints[i].y, refPoints[i].z) * refPoints[i].w;
                refTotal += refPoints[i].w;
            }
            inCentroid /= inTotal;
            refCentroid /= refTotal;

            //Calculate the scale ratio
            if (solveScale)
            {
                float inScale = 0f, refScale = 0f;
                for (int i = 0; i < inPoints.Length; i++)
                {
                    inScale += (new Vector3(inPoints[i].x, inPoints[i].y, inPoints[i].z) - inCentroid).magnitude;
                    refScale += (new Vector3(refPoints[i].x, refPoints[i].y, refPoints[i].z) - refCentroid).magnitude;
                }
                scaleRatio = (refScale / inScale);
            }

            //Calculate the 3x3 covariance matrix, and the optimal rotation
            if (solveRotation)
            {
                extractRotation(TransposeMultSubtract(inPoints, refPoints, inCentroid, refCentroid, DataCovariance), ref OptimalRotation);
            }

            return Matrix4x4.TRS(refCentroid, Quaternion.identity, Vector3.one * scaleRatio) *
                   Matrix4x4.TRS(Vector3.zero, OptimalRotation, Vector3.one) *
                   Matrix4x4.TRS(-inCentroid, Quaternion.identity, Vector3.one);
        }

        //https://animation.rwth-aachen.de/media/papers/2016-MIG-StableRotation.pdf
        //Iteratively apply torque to the basis using Cross products (in place of SVD)
        void extractRotation(Vector3[] A, ref Quaternion q)
        {
            for (int iter = 0; iter < 9; iter++)
            {
                q.FillMatrixFromQuaternion(ref QuatBasis);
                Vector3 omega = (Vector3.Cross(QuatBasis[0], A[0]) +
                                 Vector3.Cross(QuatBasis[1], A[1]) +
                                 Vector3.Cross(QuatBasis[2], A[2])) *
                 (1f / Mathf.Abs(Vector3.Dot(QuatBasis[0], A[0]) +
                                 Vector3.Dot(QuatBasis[1], A[1]) +
                                 Vector3.Dot(QuatBasis[2], A[2]) + 0.000000001f));

                float w = omega.magnitude;
                if (w < 0.000000001f)
                    break;
                q = Quaternion.AngleAxis(w * Mathf.Rad2Deg, omega / w) * q;
                q = Quaternion.Lerp(q, q, 0f); //Normalizes the Quaternion; critical for error suppression
            }
        }

        //Calculate Covariance Matrices --------------------------------------------------
        public static Vector3[] TransposeMultSubtract(Vector3[] vec1, Vector4[] vec2, Vector3 vec1Centroid, Vector3 vec2Centroid, Vector3[] covariance)
        {
            for (int i = 0; i < 3; i++)
            { //i is the row in this matrix
                covariance[i] = Vector3.zero;
            }

            for (int k = 0; k < vec1.Length; k++)
            {//k is the column in this matrix
                Vector3 left = (vec1[k] - vec1Centroid) * vec2[k].w;
                Vector3 right = (new Vector3(vec2[k].x, vec2[k].y, vec2[k].z) - vec2Centroid) * Mathf.Abs(vec2[k].w);

                covariance[0][0] += left[0] * right[0];
                covariance[1][0] += left[1] * right[0];
                covariance[2][0] += left[2] * right[0];
                covariance[0][1] += left[0] * right[1];
                covariance[1][1] += left[1] * right[1];
                covariance[2][1] += left[2] * right[1];
                covariance[0][2] += left[0] * right[2];
                covariance[1][2] += left[1] * right[2];
                covariance[2][2] += left[2] * right[2];
            }

            return covariance;
        }
        public static Vector3[] TransposeMultSubtract(Vector3[] vec1, Vector3[] vec2, ref Vector3[] covariance)
        {
            for (int i = 0; i < 3; i++) covariance[i] = Vector3.zero;

            for (int k = 0; k < vec1.Length; k++)
            {//k is the column in this matrix
                Vector3 left = vec1[k];
                Vector3 right = vec2[k];

                covariance[0][0] += left[0] * right[0];
                covariance[1][0] += left[1] * right[0];
                covariance[2][0] += left[2] * right[0];
                covariance[0][1] += left[0] * right[1];
                covariance[1][1] += left[1] * right[1];
                covariance[2][1] += left[2] * right[1];
                covariance[0][2] += left[0] * right[2];
                covariance[1][2] += left[1] * right[2];
                covariance[2][2] += left[2] * right[2];
            }
            return covariance;
        }
    }
}

public static class FromMatrixExtension
{
    public static Vector3 GetVector3(this Matrix4x4 m) { return m.GetColumn(3); }
    public static Quaternion GetQuaternion(this Matrix4x4 m)
    {
        if (m.GetColumn(2) == m.GetColumn(1)) { return Quaternion.identity; }
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }
    public static void FillMatrixFromQuaternion(this Quaternion q, ref Vector3[] covariance)
    {
        covariance[0] = q * Vector3.right;
        covariance[1] = q * Vector3.up;
        covariance[2] = q * Vector3.forward;
    }
    public static Matrix4x4 Lerp(Matrix4x4 a, Matrix4x4 b, float alpha)
    {
        return Matrix4x4.TRS(Vector3.Lerp(a.GetVector3(), b.GetVector3(), alpha), Quaternion.Slerp(a.GetQuaternion(), b.GetQuaternion(), alpha), Vector3.one);
    }
}
