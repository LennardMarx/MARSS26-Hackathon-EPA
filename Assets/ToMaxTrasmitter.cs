using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

using extOSC;
using extOSC.Core;

using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using System.Security.Cryptography;

public class ToMaxTrasmitter : MonoBehaviour
{
    private OSCTransmitter _transmitter;

    // Define your OSC address; you can change it as needed.
    private const string _oscAddressAlignament = "/errors/alignment";
    private const string _oscAddressAccess = "/errors/access";
    private const string _oscAddressToggle = "/errors/toggle";

    public SpatialMappingModel spatialMapping;
    //public Transform trackedNeedle;
    private Vector3 needleInitialPos, curNeedlePos;
    private bool initialSoundOnFlag = false, currentSoundOnFlag = false;

    // unit of measure: mm  

    // Scaling values
    private const float minVal_tipEntry = 1f, maxVal_tipEntry = 25f;
    private const float minVal_toolRot = 1f, maxVal_toolRot = 30f;
    private const float minVal_toHeart = 1f, maxVal_toHeart = 30f;
    private const float minVal_toPeric = 1f, maxVal_toPeric = 60f;

    // Tool state definitions
    private const float toPeriThreshold = 5.0f, toHeartThreshold = 1.0f, entryPointAlignamentThreshold = 5.0f, toolRotationAlignamentThreshold = 3.0f;

    void Start()
    {
        // Create and add the OSCTransmitter component to this GameObject.
        _transmitter = gameObject.AddComponent<OSCTransmitter>();

        // Set the remote host (e.g., the IP address of Max/MSP)
        _transmitter.RemoteHost = "127.0.0.1";

        // Set the remote port that Max is listening on.
        _transmitter.RemotePort = 6000;


    }

    void Update()
    {
        if (_transmitter == null)
            return;

        if (spatialMapping.isAlignmentActive)
        {

            float normDistance = Mathf.Clamp01((spatialMapping.lastAlignmentDistanceMm - minVal_tipEntry) / (maxVal_tipEntry - minVal_tipEntry));
            float normRotation = Mathf.Clamp01((spatialMapping.lastAlignmentRotationError - minVal_toolRot) / (maxVal_toolRot - minVal_toolRot));

            // in this stage I want to turn on the sound only when spatialMapping.lastAlignmentDistanceMm is lower then maxVal_tipEntry

            if (spatialMapping.lastAlignmentDistanceMm < maxVal_tipEntry)
            {
                currentSoundOnFlag = true;
                currentSoundOnFlag = false;
                Debug.Log("NO PHASES");
                if (initialSoundOnFlag != currentSoundOnFlag) // send this message only once 
                {
                    var messageSoundOn = new OSCMessage(_oscAddressToggle);
                    messageSoundOn.AddValue(OSCValue.Int(1));
                    _transmitter.Send(messageSoundOn);
                }
                initialSoundOnFlag = currentSoundOnFlag;

                if (spatialMapping.lastAlignmentDistanceMm > entryPointAlignamentThreshold)
                {
                    var messageAlignament = new OSCMessage(_oscAddressAlignament);
                    messageAlignament.AddValue(OSCValue.Int(1));
                    messageAlignament.AddValue(OSCValue.Float(normDistance));
                    _transmitter.Send(messageAlignament);
                }
                else if (spatialMapping.lastAlignmentDistanceMm <= entryPointAlignamentThreshold)
                {
                    var messageAlignament = new OSCMessage(_oscAddressAlignament);
                    messageAlignament.AddValue(OSCValue.Int(2));
                    messageAlignament.AddValue(OSCValue.Float(normRotation));
                    _transmitter.Send(messageAlignament);
                }

            }


        }
        if (spatialMapping.isToTargetActive)
        {
            // sound always on during th
            var messageAccess = new OSCMessage(_oscAddressAccess);
            // definition of tool state area
            if (spatialMapping.lastTargetPeriMm > toPeriThreshold && spatialMapping.lastTargetHeartMm > toHeartThreshold)
            {
                // we are in area 1
                // Normalize target reaching values.
                float normTipPeri = Mathf.Clamp01((spatialMapping.lastTargetPeriMm - minVal_toPeric) / (maxVal_toPeric - minVal_toPeric));
                messageAccess.AddValue(OSCValue.Int(1));
                messageAccess.AddValue(OSCValue.Float(normTipPeri));
                messageAccess.AddValue(OSCValue.Int(900));
                _transmitter.Send(messageAccess);

            }

            else if (spatialMapping.lastTargetPeriMm < toPeriThreshold && spatialMapping.lastTargetPeriMm > 0 && spatialMapping.lastTargetHeartMm > toHeartThreshold)
            {
                // we are in area 2
                // Normalize target reaching values.
                float normTipPeri = Mathf.Clamp01((spatialMapping.lastTargetPeriMm - minVal_toPeric) / (maxVal_toPeric - minVal_toPeric));
                float normPeriHeart = Mathf.Clamp01((spatialMapping.lastTargetHeartMm - minVal_toHeart) / (maxVal_toHeart - minVal_toHeart));
                messageAccess.AddValue(OSCValue.Int(2));
                messageAccess.AddValue(OSCValue.Float(normTipPeri));
                messageAccess.AddValue(OSCValue.Float(normPeriHeart));
                _transmitter.Send(messageAccess);
            }

            else if (spatialMapping.lastTargetPeriMm <= 0 && spatialMapping.lastTargetHeartMm > toHeartThreshold)
            {
                // we are in area 3
                // Normalize target reaching values.
                //float normTipPeri = Mathf.Clamp01((Math.Abs(spatialMapping.lastTargetPeriMm )- minVal_totip) / (maxVal_totip - minVal_totip));
                float normPeriHeart = Mathf.Clamp01((Math.Abs(spatialMapping.lastTargetHeartMm) - minVal_toHeart) / (maxVal_toHeart - minVal_toHeart));
                messageAccess.AddValue(OSCValue.Int(3));
                messageAccess.AddValue(OSCValue.Int(900));
                messageAccess.AddValue(OSCValue.Float(normPeriHeart));
                _transmitter.Send(messageAccess);
            }

            else if (spatialMapping.lastTargetHeartMm < toHeartThreshold)
            {
                // we are in area 3
                messageAccess.AddValue(OSCValue.Int(4));
                messageAccess.AddValue(OSCValue.Int(900));
                messageAccess.AddValue(OSCValue.Int(900));
                _transmitter.Send(messageAccess);
            }

        }

    }
}