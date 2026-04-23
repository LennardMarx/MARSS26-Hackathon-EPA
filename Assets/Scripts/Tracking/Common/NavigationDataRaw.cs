using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

using Debug = UnityEngine.Debug;


public class NavigationDataRaw : MonoBehaviour
{
    [Header("Tool and Target Transforms")]
    public Transform tool;              // Assegna direttamente il Transform del tool
    public Transform toolTip;           // Assegna direttamente il Transform della tip del tool
    public Transform target;            // Assegna direttamente il Transform del target (es: start o end)
    public Transform targetDirection;   // Assegna direttamente il Transform della traiettoria pianificata

    [Header("Navigation Settings")]
    public int nDofs = 5; // 2 o 4 DoFs

    void Update()
    {
        if (tool == null || toolTip == null || target == null || targetDirection == null)
        {
            Debug.LogError("One or more Transform not assigned in the inspector!");
            return;
        }

        if (nDofs == 2)
            Compute2DofError();
        else if (nDofs == 5)
            Compute5DofError();
    }

    void Compute2DofError()
    {
        float distance = Vector3.Distance(toolTip.position, target.position);

        float alignmentError = Vector3.Angle(tool.up, targetDirection.up);

        Debug.Log($"Entry Point Error: {distance * 1000f} mm\nRotation Error: {alignmentError:F2}°");
    }

    void Compute5DofError()
    {
        // Distanze traslazionali (3 DoF)
        float distanceX = Mathf.Abs(toolTip.position.x - target.position.x);
        float distanceY = Mathf.Abs(toolTip.position.y - target.position.y);
        float distanceZ = Mathf.Abs(toolTip.position.z - target.position.z);
        float euclideanDistance = Vector3.Distance(toolTip.position, target.position);

        // Rotazioni (2 DoF: yaw e pitch)
        Vector3 toolYAxis = tool.up;
        Vector3 targetYAxis = targetDirection.up;
        float yawError = Mathf.Abs(Vector3.SignedAngle(new Vector3(toolYAxis.x, 0, toolYAxis.z),
                                                       new Vector3(targetYAxis.x, 0, targetYAxis.z),
                                                       Vector3.up));
        float pitchError = Mathf.Abs(Vector3.SignedAngle(new Vector3(0, toolYAxis.y, toolYAxis.z),
                                                         new Vector3(0, targetYAxis.y, targetYAxis.z),
                                                         Vector3.right));

        Debug.Log(
            $"Entry Point Error (Euclidean): {euclideanDistance * 1000f:F2} mm\n" +
            $"\tR-L: {distanceX * 1000f:F2} mm\tSUP-INF: {distanceZ * 1000f:F2} mm\tDepth: {distanceY * 1000f:F2} mm\n" +
            $"Azimuthal (horizontal) Error: {yawError:F2}°\n" +
            $"Elevation (vertical) Error: {pitchError:F2}°"
        );
    }
}
