using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingInterface : MonoBehaviour
{

    public Dictionary<string, TrackingData> trackedObjects = new Dictionary<string, TrackingData>();
    public double timeOffset;
}
