using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NDI
using NDI.CapiSample.Data;
using NDI.CapiSample.Protocol;

//using Zeroconf;


public class NDITracking : TrackingInterface {
    public string host = "169.254.210.15";
    public bool useZeroConfig = false;

    [Tooltip("ROM file TextAssets. They should be created with the NDI Tools, given the extension \".bytes\" and placed in the Resources folder, so that they can be loaded as binary files during runtime.")]
    public List<TextAsset> roms;

    //This variable gets populated on start. It stores the binary data from the "roms" list
    private List<byte[]> romsBytes;

    private List<string> threadLog = new List<string>();
    private System.Threading.Tasks.Task trackingTask;
    //System.Threading.CancellationTokenSource TokenSource;

    private bool cancel = false;

    static float tracking100PerCentQualityLimit = 0.1f;//TODO: needs to find resonable value

    public bool preferTXoverBX1 = false;


    private static TrackingData TrackingDataFromNDIFormat(float posx, float posy, float posz, float rotqx, float rotqy, float rotqz, float rotq0, double time, NDI.CapiSample.Data.TransformStatus status, double error) {
        float localQuality = 0.0f;
        double localTimestamp = 0.0d;
        if(status == TransformStatus.TooFewMarkers) {
            return new TrackingData(0f, 0f, 0f, 1.0f, 0f, 0f, 0f, localTimestamp, 0.0f, 0);

        }
        if(status == TransformStatus.Inteference) {
            return new TrackingData(0f, 0f, 0f, 1.0f, 0f, 0f, 0f, localTimestamp, 0.0f, 0);
        }
        if(rotqx == 0.0f && rotqy == 0.0f && rotqz == 0.0f && rotq0 == 0.0f) {
            return new TrackingData(0f, 0f, 0f, 1.0f, 0f, 0f, 0f, localTimestamp, 0.0f, 0);
        }


        UnityEngine.Vector3 pos = new UnityEngine.Vector3(posx, posy, posz);
        UnityEngine.Quaternion rot = UnityEngine.Quaternion.Normalize(new UnityEngine.Quaternion(rotqx, rotqy, rotqz, rotq0));
        //UnityEngine.Quaternion rot = QuaternionMath.NormalizeQuaternion(rotqx, rotqy, rotqz, rotq0);

        //Debug.Log(System.Convert.ToString(System.BitConverter.GetBytes(rotqx)[0], 2).PadLeft(8, '0'));
        //Debug.Log(System.Convert.ToString(System.BitConverter.GetBytes(rotqx)[1], 2).PadLeft(8, '0'));
        //Debug.Log(System.Convert.ToString(System.BitConverter.GetBytes(rotqx)[2], 2).PadLeft(8, '0'));
        //Debug.Log(System.Convert.ToString(System.BitConverter.GetBytes(rotqx)[3], 2).PadLeft(8, '0'));
        if(Mathf.Abs(rotqx) > 100f && Mathf.Abs(rotqy) > 100f && Mathf.Abs(rotqz) > 100f && Mathf.Abs(rotq0) > 100f) {
            //While its mathematically fine to store large values in a Quaternion, they are usually in a range around -1 to 1.
            //I'll assume that its an error if they're all very big
            rot = UnityEngine.Quaternion.identity;
        }
        else {
            rot = UnityEngine.Quaternion.Normalize(new UnityEngine.Quaternion(rotqx, rotqy, rotqz, rotq0));
        }


        Matrix4x4 m = Matrix4x4.TRS(pos, rot, UnityEngine.Vector3.one);
        m = MatrixMath.ConvertRHCS2LHCS(m);
        pos = MatrixMath.PositionFromMatrix(m);
        rot = MatrixMath.RotationFromMatrix(m);



        if(pos.x != 0.0f && pos.y != 0.0f && pos.z != 0.0f) {
            if(pos.x > -100000.0f && pos.y > -100000.0f && pos.z > -100000.0f && pos.x < 100000.0f && pos.y < 100000.0f && pos.z < 100000.0f) {
                localTimestamp = time;
                if(status == TransformStatus.Enabled) {
                    //localQuality = 1.0f;
                    ////TODO: needs to be tested!!!
                    localQuality = Mathf.Clamp01(tracking100PerCentQualityLimit / (float)error);
                }
                //if(status == TransformStatus.PartiallyOutOfVolume) {
                //    localQuality = 0.0f;
                //}
                //if(status == TransformStatus.OutOfVolume) {
                //    localQuality = 0.0f;
                //}
            }
        }




        return new TrackingData(pos.x / 1000.0f, pos.y / 1000.0f, pos.z / 1000.0f, rot.x, rot.y, rot.z, rot.w, localTimestamp, localQuality, 0);
    }

    void OnApplicationQuit() {
        cancel = true;
        //TokenSource.Cancel(false);
        bool taskcompleted = trackingTask.Wait(3000);
        if(!taskcompleted) {
            Debug.Log("Waited 3s, but task is not finished. You might want to restart the app.");
        }
        else {
            Debug.Log("NDIThread closed fine.");
        }


    }

    void Start() {
        //TokenSource = new System.Threading.CancellationTokenSource();
        romsBytes = new List<byte[]>();

        if(roms.Count == 0) {
            Debug.Log("roms empty. That does not sound good. They need to be specified.");
        }

        foreach(TextAsset asset in roms) {
            romsBytes.Add(asset.bytes);
        }

        if(romsBytes.Count == 0) {
            Debug.Log("romsBytes empty. That does not sound good. They need to be loaded from roms.");
        }

        //if(useZeroConfig) {
        //    host = GetZeroConfigIP();
        //}

        // Starting tracking in a separate task. Apparently it cannot run on the main thread, as there are some Unity
        // Limitations of async and await tasks:
        // https://docs.unity3d.com/2021.2/Documentation/Manual/overview-of-dot-net-in-unity.html
        trackingTask = System.Threading.Tasks.Task.Run(() => Run());

    }

    //So that the current time (at framerate of the application) is available to the tracking thread as well
    private float currentTime;


    void Update() {
        printThreadLogNewMessages();
        //if (cancel) {
        //Debug.Log("Cancel");
        //TokenSource.Cancel(false);
        //}
        currentTime = Time.time;



    }

    private void printThreadLogNewMessages() {
        lock(threadLog) {
            foreach(string msg in threadLog) {
                Debug.Log(msg);
            }
            threadLog.Clear();
        }
    }


    private bool InitializePorts(Capi cAPI) {
        foreach(byte[] romBytes in romsBytes) { // only tested with one rom so far. Hope it also works with multiple trackers
            Port tool = cAPI.PortHandleRequest();
            if(tool == null) {
                log("Could not get available port for tool.");
            }
            else {
                //old
                //bool sromSuccess = tool.LoadSROM(path);
                //new
                bool sromSuccess = tool.LoadSROMBytes(romBytes);

                if(!sromSuccess) {
                    log("Could not load SROM file for tool.");
                    return false;
                }
            }
        }

        // Initialize all ports not currently initialized
        var ports = cAPI.PortHandleSearchRequest(PortHandleSearchType.NotInit);
        foreach(var port in ports) {
            if(!port.Initialize()) {
                log("Could not initialize port " + port.PortHandle + ".");
                return false;
            }

            if(!port.Enable()) {
                log("Could not enable port " + port.PortHandle + ".");
                return false;
            }
        }

        // List all enabled ports
        log("Enabled Ports:");
        ports = cAPI.PortHandleSearchRequest(PortHandleSearchType.Enabled);
        foreach(var port in ports) {
            port.GetInfo();
            log(port.ToString());
        }

        return true;
    }

    private static bool IsBX2Supported(string apiRevision) {
        // Refer to the API guide for how to interpret the APIREV response
        char deviceFamily = apiRevision[0];
        int majorVersion = int.Parse(apiRevision.Substring(2, 3));

        // As of early 2017, the only NDI device supporting BX2 is the Vega
        // Vega is a Polaris device with API major version 003
        if(deviceFamily == 'G' && majorVersion >= 3) {
            return true;
        }

        return false;
    }

    private void log(string msg) {
        lock(threadLog) {
            threadLog.Add(msg);
        }
    }

    //private static string GetZeroConfigIP() {
    //    IReadOnlyList<IZeroconfHost> results = ZeroconfResolver.ResolveAsync("_ndi._tcp.local.").GetAwaiter().GetResult();
    //    foreach(var result in results) {
    //        return result.IPAddress;
    //    }
    //    return null;
    //}

    private int[] previousFrameNumber;
    private void Run() {
        Capi cAPI;
        //TODO: Only the TCP part is tested (Poaris Vega), not the serial connection part (Polaris Spectra)
        if(host.StartsWith("COM") || host.StartsWith("/dev")) {
            cAPI = new NDI.CapiSample.CapiSerial(host);
        }
        else {
            cAPI = new NDI.CapiSample.CapiTcp(host);
        }
        log("C# CAPI Sample v" + Capi.GetVersion());


        if(!cAPI.Connect()) {
            return;
        }
        log("Connected");

        // Get the API Revision this will tell us if BX2 is supported.
        string revision = cAPI.GetAPIRevision();
        log("Revision:" + revision);
        if(IsBX2Supported(revision)) {
            log("BX2");
        }
        else {
            if(preferTXoverBX1) {
                log("TX");
            }
            else {
                log("BX1");
            }
        }

        if(!cAPI.Initialize()) {
            log("Could not initialize.");
            return;
        }

        // The Frame Frequency may not be possible to set on all devices, so an error response is okay.
        cAPI.SetUserParameter("Param.Tracking.Frame Frequency", "60");
        cAPI.SetUserParameter("Param.Tracking.Track Frequency", "2");

        // Read the final values
        log(cAPI.GetUserParameter("Param.Tracking.Frame Frequency"));
        log(cAPI.GetUserParameter("Param.Tracking.Track Frequency"));

        // Initialize tool ports
        if(!InitializePorts(cAPI)) {
            return;
        }

        if(!cAPI.TrackingStart()) {
            log("Could not start tracking.");
            return;
        }

        //BX1 stuff
        previousFrameNumber = new int[romsBytes.Count];
        for(int i = 0; i < previousFrameNumber.Length; i++) {
            previousFrameNumber[i] = -1;
        }
        //end BX1 stuff

        try {

            while(!cancel) {
                if(!cAPI.IsConnected) {
                    log("Disconnected while tracking.");
                    break;
                }
                if(IsBX2Supported(revision)) {
                    //log("BX2");
                    // Track data using the BX2 Command
                    List<Tool> tools = cAPI.SendBX2("--6d=tools");
                    for(int i = 0; i < tools.Count; i++) {
                        Tool t = tools[i];
                        //log(t.ToString());
                        if(IsBX2Supported(revision)) {
                            if(t.dataIsNew) {
                                //log(t.ToString());
                                string trackerName = t.transform.toolHandle.ToString();
                                NDI.CapiSample.Data.Vector3 pos = t.transform.position;
                                NDI.CapiSample.Data.Quaternion rot = t.transform.orientation;
                                TrackingData trD = TrackingDataFromNDIFormat((float)pos.x, (float)pos.y, (float)pos.z, (float)rot.qx, (float)rot.qy, (float)rot.qz, (float)rot.q0, (double)t.timespec_s + (double)t.timespec_ns / 1e9, t.transform.status, t.transform.error);
                                timeOffset = currentTime - (double)t.timespec_s - (double)t.timespec_ns / 1e9;
                                if(trackedObjects.ContainsKey(trackerName)) {
                                    trackedObjects[trackerName] = trD;
                                }
                                else {
                                    trackedObjects.Add(trackerName, trD);

                                }
                            }
                        }
                    }
                }
                else {
                    if(preferTXoverBX1) {
                        //log("TX");
                        string tx = cAPI.GetTrackingDataTX();
                        //Debug.Log(tx);
                        List<Capi.ToolTrackingData> TX_Tools = cAPI.ParseTXResponse(tx);
                        //trackingTask.Wait(50);
                        //Debug.Log(TX_Tools.Count);
                        for(int i = 0; i < TX_Tools.Count; i++) {
                            Capi.ToolTrackingData t = TX_Tools[i];
                            //log(t.ToString());
                            //Debug.Log(t.ToolHandle + ", " + t.Status);
                            if(t.IsValid) {
                                if(t.FrameNumber > previousFrameNumber[i]) {
                                    previousFrameNumber[i] = (int)t.FrameNumber;
                                    //log(t.ToString());
                        
                                    string trackerName = t.ToolHandle.ToString();
                                    NDI.CapiSample.Data.Vector3 pos = t.Position;
                                    NDI.CapiSample.Data.Quaternion rot = t.Quaternion;
                                    float errorVal = t.TrackingError;
                                    NDI.CapiSample.Data.TransformStatus status = TransformStatus.Enabled;
                                    if(t.Status.Equals("MISSING")) {
                                        status = TransformStatus.ToolMissing;
                                    }
                                    else if(t.Status.Equals("DISABLED")) {
                                        status = TransformStatus.TrackingNotEnabled;
                                    }
                        
                                    TrackingData trD = TrackingDataFromNDIFormat((float)pos.x, (float)pos.y, (float)pos.z, (float)rot.qx, (float)rot.qy, (float)rot.qz, (float)rot.q0, (double)currentTime, status, errorVal);
                                    timeOffset = 0;
                                    if(trackedObjects.ContainsKey(trackerName)) {
                                        trackedObjects[trackerName] = trD;
                                    }
                                    else {
                                        trackedObjects.Add(trackerName, trD);
                        
                                    }
                                }
                            }
                        }
                    }
                    else {
                        //log("BX1");
                        // I read a similar line in https://github.com/dgblack/UnityNDIPolaris/blob/main/NDIPolaris/NDIPolarisStreamer.cs, who did a similar project
                        List<Tool> tools = cAPI.SendBX();

                        //Debug.Log(tools.Count);
                        for(int i = 0; i < tools.Count; i++) {
                            Tool t = tools[i];
                            //log(t.ToString());




                            if(t.frameNumber > previousFrameNumber[i]) {
                                previousFrameNumber[i] = t.frameNumber;
                                //log(t.ToString());
                                string trackerName = t.transform.toolHandle.ToString();
                                NDI.CapiSample.Data.Vector3 pos = t.transform.position;
                                NDI.CapiSample.Data.Quaternion rot = t.transform.orientation;
                                TrackingData trD = TrackingDataFromNDIFormat((float)pos.x, (float)pos.y, (float)pos.z, (float)rot.qx, (float)rot.qy, (float)rot.qz, (float)rot.q0, (double)currentTime, t.transform.status, tracking100PerCentQualityLimit);
                                timeOffset = 0;
                                if(trackedObjects.ContainsKey(trackerName)) {
                                    trackedObjects[trackerName] = trD;
                                }
                                else {
                                    trackedObjects.Add(trackerName, trD);

                                }
                            }
                        }
                    }

                }

            }
        }
        catch(System.Exception e) {
            Debug.LogError(e.Message + e.StackTrace);
        }
        finally {

            if(!cAPI.TrackingStop()) {
                log("Could not stop tracking.");
                //return;
            }
            log("TrackingStopped");

            if(!cAPI.Disconnect()) {
                log("Could not disconnect.");
                //return;
            }
            log("Disconnected");
        }
    }
}