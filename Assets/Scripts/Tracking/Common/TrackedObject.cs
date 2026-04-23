// ============================================================================
// 
// For reseach, reference and documentation only.
// 
// If not explicitly stated: Copyright (C) 2017-2022, all rights reserved,
// Alexander Winkler
// Email alexander.winkler@tum.de
// Computer Aided Medical Procedures and Augmented Reality
// Technische Universität München
// Boltzmannstr. 3, 85748 Garching b. München, Germany
// 
// ============================================================================

using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;


public class TrackedObject : MonoBehaviour {

    public TrackingInterface Tracking;
    
    public float timeshiftSeconds = 0.0f;

    public string TrackerName;

    private string sanitizedTrackerName;

    public enum ButtonState : int { Nothing = 0, MiddleDown = 1, MiddleRelease = 2, LeftDown = 3, LeftRelease = 6, RightDown = 2, RightRelease = 4 };

    public ButtonState buttonPress = ButtonState.Nothing;

    private bool childrenInvisible = false;

    public double quality;

    public double timestamp;

    private Nito.Collections.Deque<TrackingData> history = new Nito.Collections.Deque<TrackingData>();

    public int historySize = 2;


    public bool keepPositionOnTrackingLostForever = false;

    public float timeOutAfterTrackingLost = 0.0f;
    private float timer = 0.0f;

    public float qualityLimit = 0.0f;

    private List<Renderer> childRendererEnabledList = new List<Renderer>();

    public bool timeshift = false;
    public double timeshiftOffset = 0f;

    public bool averaging3 = false;

    public float timeStampInvalidationTreshold = 2.0f;

    
    private void Start() {
        sanitizedTrackerName = TrackerName.Trim();
        foreach(Renderer rend in GetComponentsInChildren<Renderer>(false)) {
            childRendererEnabledList.Add(rend);
        }
    }


    private bool prevVisibility = false;
    private bool firstCall = true;
    private void setChildrenInvisible(bool invisible) {
        if(prevVisibility != invisible || firstCall) {
            firstCall = false;
            childrenInvisible = invisible;
            prevVisibility = invisible;
        }
    }




    private void ApplyVisibilityChanges() {
        foreach(var entry in childRendererEnabledList) {
            entry.enabled = !childrenInvisible;
        }
    }

    void Update() {

              
        if(Tracking.trackedObjects.ContainsKey(sanitizedTrackerName)) {
            TrackingData trD = Tracking.trackedObjects[sanitizedTrackerName];

            if(timeshift) {
                if(history.Count > 0) {
                    TrackingData tail = history.PeekFromBack();
                    if(trD.timestamp > tail.timestamp) {
                        if(trD.quality > 0.0) {
                            history.AddToBack(trD.Copy());
                        }
                    }
                }
                else {
                    if(trD.quality > 0.0) {
                        history.AddToBack(trD.Copy());
                    }

                }
                while(history.Count > historySize) {
                    history.RemoveFromFront();
                }
            }


            if(timeshift) {
                //look into past
                if(timeshiftOffset < 0) {
                    timeshiftOffset = 0.0;
                }
                if(history.Count > 0) {
                    double targetTimeStamp = history.PeekFromBack().timestamp - timeshiftOffset;
                    TrackingData closestTrD = history.PeekFromFront();
                    //Debug.Log(targetTimeStamp);
                    TrackingData[] historyAsArray = history.ToArray();
                    for(int i = 0; i < historyAsArray.Length; i++) {
                        if(targetTimeStamp - historyAsArray[i].timestamp <= 0) {
                            if(averaging3 && i - 1 >= 0 && i + 1 < historyAsArray.Length) {
                                Vector3 posprev = new Vector3(historyAsArray[i-1].positionX, historyAsArray[i-1].positionY, historyAsArray[i-1].positionZ);
                                Vector3 posnow = new Vector3(historyAsArray[i].positionX, historyAsArray[i].positionY, historyAsArray[i].positionZ);
                                Vector3 posafter = new Vector3(historyAsArray[i+1].positionX, historyAsArray[i+1].positionY, historyAsArray[i+1].positionZ);
                                Vector3 averagePos = ( posprev + posnow + posafter ) / 3f;

                                Quaternion[] quaternions = new Quaternion[3];
                                quaternions[0] = new Quaternion(historyAsArray[i - 1].rotationX, historyAsArray[i - 1].rotationY, historyAsArray[i - 1].rotationZ, historyAsArray[i - 1].rotationW);
                                quaternions[1] = new Quaternion(historyAsArray[i].rotationX, historyAsArray[i].rotationY, historyAsArray[i].rotationZ, historyAsArray[i].rotationW);
                                quaternions[2] = new Quaternion(historyAsArray[i + 1].rotationX, historyAsArray[i + 1].rotationY, historyAsArray[i + 1].rotationZ, historyAsArray[i + 1].rotationW);
                                //Quaternion averageRot = Quaternion.Slerp(quaternions[0], quaternions[2], 0.5f);
                                Quaternion averageRot = QuaternionMath.AverageQuaternion(quaternions);

                                float averageQuality = (historyAsArray[i - 1].quality + historyAsArray[i].quality + historyAsArray[i + 1].quality) / 3f;

                                closestTrD = new TrackingData(averagePos.x, averagePos.y, averagePos.z, averageRot.x, averageRot.y, averageRot.z, averageRot.w, historyAsArray[i].timestamp, averageQuality, historyAsArray[i].lastButtonEvent);
                            }
                            else {
                                closestTrD = historyAsArray[i].Copy();
                            }
                            //Debug.Log(i);
                            break;
                        }
                    }

                    if(closestTrD.quality > 0.0d) {
                        transform.localPosition = new Vector3(closestTrD.positionX, closestTrD.positionY, closestTrD.positionZ);
                        transform.localRotation = new Quaternion(closestTrD.rotationX, closestTrD.rotationY, closestTrD.rotationZ, closestTrD.rotationW);
                        buttonPress = (ButtonState)closestTrD.lastButtonEvent;
                        quality = closestTrD.quality;
                        timestamp = closestTrD.timestamp;
                    }
                }
            }
            else {
                if(trD.quality > 0.0d) {
                    transform.localPosition = new Vector3(trD.positionX, trD.positionY, trD.positionZ);
                    transform.localRotation = new Quaternion(trD.rotationX, trD.rotationY, trD.rotationZ, trD.rotationW);
                    buttonPress = (ButtonState)trD.lastButtonEvent;
                    quality = trD.quality;
                    timestamp = trD.timestamp;
                }
            }


            if(!keepPositionOnTrackingLostForever) {
                if(quality <= qualityLimit) {
                    timer += Time.deltaTime;
                    if(timer >= timeOutAfterTrackingLost) {
                        //disable all child renderers
                        setChildrenInvisible(true);
                    }
                }
                else {
                    timer = 0.0f;
                    //enable child renderers
                    setChildrenInvisible(false);
                }
            }

        }
        //Set qualilty of old data to 0
        //This is probably NDI specific!
        //var timeSpan = (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0));
        //double now = timeSpan.TotalSeconds;
        //Debug.Log(now);
        //Debug.Log(System.Math.Abs(now - timestamp));
        //Debug.Log(name + (Time.time - timestamp).ToString("F8"));
        if((Time.time - timestamp - Tracking.timeOffset) > timeStampInvalidationTreshold) {
            quality = 0f;
        }
    }



}
