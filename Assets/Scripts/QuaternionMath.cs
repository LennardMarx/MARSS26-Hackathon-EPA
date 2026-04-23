// ========================================================================
// 
// If not explicitly stated: Copyright (C) 2024, all rights reserved,
// Alexander Winkler
// Email alexander.winkler@tum.de
// Computer Aided Medical Procedures and Augmented Reality
// Technische Universität München
// Boltzmannstr. 3, 85748 Garching b. München, Germany
//
// and
// Alexander Winkler
// Email alexander.winkler@med.uni-muenchen.de
// Department of General, Visceral, and Transplantation Surgery
// Hospital of the LMU Munich
// Ludwig-Maximilians-Universität (LMU), Munich, Germany
//
// ========================================================================


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaternionMath {

    //Get an average (mean) from more than two quaternions (with two, slerp would be used).
    //Note: this only works if all the quaternions are relatively close together.
    //Usage:
    //-Cumulative is an external Vector4 which holds all the added x y z and w components.
    //-newRotation is the next rotation to be added to the average pool
    //-firstRotation is the first quaternion of the array to be averaged
    //-addAmount holds the total amount of quaternions which are currently added
    //This function returns the current average quaternion
    public static Quaternion AverageQuaternion(ref Vector4 cumulative, Quaternion newRotation, Quaternion firstRotation, int addAmount) {

        float w = 0.0f;
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;

        //Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
        //q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
        if(!AreQuaternionsClose(newRotation, firstRotation)) {

            newRotation = InverseSignQuaternion(newRotation);
        }

        //Average the values
        float addDet = 1f/(float)addAmount;
        cumulative.w += newRotation.w;
        w = cumulative.w * addDet;
        cumulative.x += newRotation.x;
        x = cumulative.x * addDet;
        cumulative.y += newRotation.y;
        y = cumulative.y * addDet;
        cumulative.z += newRotation.z;
        z = cumulative.z * addDet;

        //note: if speed is an issue, you can skip the normalization step
        return NormalizeQuaternion(x, y, z, w);
    }


    public static Quaternion AverageQuaternion(List<Quaternion> quaternions) {
        Vector4 cumulative = Vector4.zero;
        Quaternion averageRot = Quaternion.identity;
        int counter = 1;
        foreach (Quaternion item in quaternions) {
            averageRot = AverageQuaternion(ref cumulative, item, quaternions[0], counter);
            counter++;
        }
        return averageRot;
    }

    public static Quaternion AverageQuaternion(Quaternion[] quaternions) {
        Vector4 cumulative = Vector4.zero;
        Quaternion averageRot = Quaternion.identity;
        int counter = 1;
        foreach (Quaternion item in quaternions) {
            averageRot = AverageQuaternion(ref cumulative, item, quaternions[0], counter);
            counter++;
        }
        return averageRot;
    }

    public static Quaternion NormalizeQuaternion(float x, float y, float z, float w) {

        float lengthD = 1.0f / (w*w + x*x + y*y + z*z);
        w *= lengthD;
        x *= lengthD;
        y *= lengthD;
        z *= lengthD;

        if(w < 0) {
            return InverseSignQuaternion(new Quaternion(x, y, z, w));
        }

        return new Quaternion(x, y, z, w);
    }

    //Changes the sign of the quaternion components. This is not the same as the inverse.
    public static Quaternion InverseSignQuaternion(Quaternion q) {

        return new Quaternion(-q.x, -q.y, -q.z, -q.w);
    }

    //Returns true if the two input quaternions are close to each other. This can
    //be used to check whether or not one of two quaternions which are supposed to
    //be very similar but has its component signs reversed (q has the same rotation as
    //-q)
    public static bool AreQuaternionsClose(Quaternion q1, Quaternion q2) {

        float dot = Quaternion.Dot(q1, q2);

        if(dot < 0.0f) {

            return false;
        }

        else {

            return true;
        }
    }

    public static Vector3 ToEulerAngle(Quaternion q) {
        float yaw, pitch, roll = 0.0f;
        // roll (x-axis rotation)
        float sinr = +2.0f * (q.w * q.x + q.y * q.z);
        float cosr = +1.0f - 2.0f * (q.x * q.x + q.y * q.y);
        roll = Mathf.Atan2(sinr, cosr);

        // pitch (y-axis rotation)
        float sinp = +2.0f * (q.w * q.y - q.z * q.x);
        if (Mathf.Abs(sinp) >= 1) {
            pitch = sinp >= 0 ? Mathf.PI / 2.0f : -Mathf.PI / 2.0f;// use 90 degrees if out of range
        }
        else {
            pitch = Mathf.Asin(sinp);
        }

        // yaw (z-axis rotation)
        float siny = +2.0f * (q.w * q.z + q.x * q.y);
        float cosy = +1.0f - 2.0f * (q.y * q.y + q.z * q.z);
        yaw = Mathf.Atan2(siny, cosy);
        float conversion = 180.0f / Mathf.PI;
        return new Vector3(yaw * conversion, pitch * conversion, roll * conversion);

    }

    public static Quaternion convertRHCS2LHCS(Quaternion q) {

        Quaternion result = new  Quaternion(-q.x, -q.z, -q.y, q.w);

        return result;
        
    }


}
