using UnityEngine;
using extOSC;
using extOSC.Core;
using Debug = UnityEngine.Debug;

public enum AlignmentMode
{
    TwoDOF,
    FourDOF
}
public enum GuidanceStage
{
    Translation,
    Rotation,
    TargetReached
}
public class ToMaxSoniXense : MonoBehaviour
{
    private OSCTransmitter _transmitter;

    private Transform toolTf;
    private Transform tipTf;
    private Transform targetTf;

    [Header("Scene References")]
    public GameObject toolParent;
    public GameObject target;
    public GameObject normalizationSphere;

    [Header("OSC Settings")]
    public string oscAddress = "/msg/distance";
    public string remoteHost = "10.184.11.132";
    public int remotePort = 6000;

    [Header("Alignment Settings")]
    public AlignmentMode alignmentMode = AlignmentMode.TwoDOF;

    [Header("Debug")]
    public bool printHierarchy = false;
    public bool verboseOSC = true;
    public bool stopCommunicationOnTarget = false;

    [Header("Guidance State")]
    public GuidanceStage guidanceStage = GuidanceStage.Translation;

    [Header("Debug Output")]
    public float lastDistanceMm;
    public float lastRotationError;
    public float lastDistanceXMm;
    public float lastDistanceZMm;
    public float lastYawError;
    public float lastPitchError;

    private Vector3 normalizationCenter;
    private float normalizationRadius;
    private bool hasReachedTarget = false;
    private bool communicationStopped = false;

    //private GuidanceStage guidanceStage = GuidanceStage.Translation;

    void Start()
    {
        if (toolParent == null || target == null || normalizationSphere == null)
        {
            Debug.LogError("Tool parent, target or normalization sphere not assigned.");
            enabled = false;
            return;
        }

        targetTf = target.transform;
        normalizationCenter = normalizationSphere.transform.position;
        normalizationRadius = normalizationSphere.transform.localScale.x / 2f;
        normalizationRadius = normalizationRadius * 1000f; // convert to mm

        _transmitter = gameObject.AddComponent<OSCTransmitter>();
        _transmitter.RemoteHost = remoteHost;
        _transmitter.RemotePort = remotePort;

        Debug.Log("OSC transmitter initialized.");
    }


    void Update()
    {
        if (communicationStopped)
            return;   // <- absolutely stops OSC forever

        if (toolTf == null || tipTf == null)
        {
            FindToolAndTip();
            if (toolTf == null || tipTf == null)
                return;
        }

        switch (alignmentMode)
        {
            case AlignmentMode.TwoDOF:
                Send2Dofs();
                break;

            case AlignmentMode.FourDOF:
                Send4Dofs();
                break;
        }
    }


    void FindToolAndTip()
    {
        Transform[] allChildren = toolParent.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allChildren)
        {
            string lower = t.name.ToLower();

            if (toolTf == null && lower.Contains("tool"))
            {
                toolTf = t;
                Debug.Log("Tool found: " + GetFullPath(toolTf));
            }

            if (tipTf == null && lower.Contains("tip"))
            {
                tipTf = t;
                Debug.Log("Tip found: " + GetFullPath(tipTf));
            }
        }

        if (printHierarchy)
        {
            Debug.Log("Hierarchy under toolParent:");
            foreach (Transform t in allChildren)
            {
                Debug.Log(GetFullPath(t));
            }

            printHierarchy = false;
        }
    }

    string GetFullPath(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    void Send2Dofs()
    {
        // --- DISTANCE from TIP ---
        //float dx = tipTf.position.x - targetTf.position.x;
        //float dz = tipTf.position.z - targetTf.position.z;
        //float distance = Mathf.Sqrt(dx * dx + dz * dz); // without depth
        float distance = Vector3.Distance(tipTf.position, targetTf.position);
        const float maxmm = 150f;

        lastDistanceMm = distance * 1000f;
        float lastDistanceMmNorm = Mathf.Clamp(lastDistanceMm / maxmm, 0f, 1f);

        // --- ROTATION from TOOL ---
        Vector3 toolY = toolTf.up;
        Vector3 tgtY = targetTf.up;

        float alignmentError = Vector3.SignedAngle(toolY, tgtY, toolTf.forward);
        lastRotationError = alignmentError;

        float alignmentErrorNorm = Mathf.Clamp(Mathf.Abs(alignmentError) / 180f, 0f, 1f);

        float distanceToSend = 0f;
        float rotationToSend = 0f;

        // --- GUIDANCE STATE MACHINE ---
        switch (guidanceStage)
        {
            case GuidanceStage.Translation:

                distanceToSend = lastDistanceMmNorm;
                rotationToSend = 0.8f;

                // Move to rotation stage once translation is achieved
                if (lastDistanceMm <= 5f)
                {
                    guidanceStage = GuidanceStage.Rotation;
                    Debug.Log("Translation reached -> switching to ROTATION guidance");
                }

                break;


            case GuidanceStage.Rotation:

                distanceToSend = 0.5f;
                rotationToSend = alignmentErrorNorm;

                // Target reached depends ONLY on rotation
                if (Mathf.Abs(lastRotationError) < 2f)
                {
                    guidanceStage = GuidanceStage.TargetReached;
                    hasReachedTarget = true;
                    Debug.Log("Rotation aligned -> TARGET REACHED");
                }

                break;


            case GuidanceStage.TargetReached:

                distanceToSend = 0.1f;
                rotationToSend = 0.7f;

                break;
        }

        int atTargetFlag = hasReachedTarget ? 1 : 0;

        // --- OPTIONAL STOP ---
        if (stopCommunicationOnTarget && hasReachedTarget)
        {
            var finalMsg = new OSCMessage(oscAddress);
            finalMsg.AddValue(OSCValue.Float(distanceToSend));
            finalMsg.AddValue(OSCValue.Float(rotationToSend));
            finalMsg.AddValue(OSCValue.Float(1f));

            _transmitter.Send(finalMsg);

            Debug.Log("Target reached. Stopping OSC communication.");
            communicationStopped = true;
            return;
        }

        // --- OSC MESSAGE ---
        var msg = new OSCMessage(oscAddress);

        msg.AddValue(OSCValue.Float(distanceToSend));
        msg.AddValue(OSCValue.Float(rotationToSend));
        msg.AddValue(OSCValue.Float(atTargetFlag));

        _transmitter.Send(msg);

        if (verboseOSC)
        {
            Debug.Log($"[2DOF | {guidanceStage}] Dist {distanceToSend:F2} | Rot {rotationToSend:F2} | Target {atTargetFlag}");
        }
    }

    void Send4Dofs()
    {
        // --- TRANSLATIONAL ERRORS (TIP) ---
        float dx = tipTf.position.x - targetTf.position.x;
        float dz = tipTf.position.z - targetTf.position.z;

        lastDistanceXMm = dx * 1000f;
        lastDistanceZMm = dz * 1000f;

        const float maxmm = 100f;  // +/- 10 mm normalization window

        float distXnorm = Mathf.Clamp(lastDistanceXMm / maxmm, 0f, 1f);
        float distZnorm = Mathf.Clamp(lastDistanceZMm / maxmm, 0f, 1f);
        //float distXnorm = Mathf.Clamp(lastDistanceXMm / (normalizationRadius * 1000f), 0f, 1f);
        //float distZnorm = Mathf.Clamp(lastDistanceZMm / (normalizationRadius * 1000f), 0f, 1f);

        // --- ROTATIONAL ERRORS (TOOL) ---
        Vector3 toolY = toolTf.up;
        Vector3 tgtY = targetTf.up;

        float yaw = Vector3.SignedAngle(
            new Vector3(toolY.x, 0f, toolY.z),
            new Vector3(tgtY.x, 0f, tgtY.z),
            Vector3.up
        );
        lastYawError = Mathf.Clamp(yaw / 180f, -1f, 1f);

        float pitch = Vector3.SignedAngle(
            new Vector3(0f, toolY.y, toolY.z),
            new Vector3(0f, tgtY.y, tgtY.z),
            Vector3.right
        );
        lastPitchError = Mathf.Clamp(pitch / 180f, -1f, 1f);

        // --- OSC MESSAGE ---
        var msg = new OSCMessage(oscAddress);

        msg.AddValue(OSCValue.Float(distXnorm));
        msg.AddValue(OSCValue.Float(distZnorm));
        msg.AddValue(OSCValue.Float(lastYawError));
        msg.AddValue(OSCValue.Float(lastPitchError));

        _transmitter.Send(msg);

        if (verboseOSC)
        {
            Debug.Log($"[4DOF] dX {distXnorm:F4} | dZ {distZnorm:F4} | Yaw {lastYawError:F4} | Pitch {lastPitchError:F4}");
        }
    }
}