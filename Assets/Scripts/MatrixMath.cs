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

public class MatrixMath {

    public static Matrix4x4 ConvertRHCS2LHCS(Matrix4x4 rhcsMatrix) {
        Matrix4x4 result =  new Matrix4x4();
        result[0, 0] = rhcsMatrix[0, 0];
        result[0, 2] = rhcsMatrix[0, 1];
        result[0, 1] = rhcsMatrix[0, 2];
        result[0, 3] = rhcsMatrix[0, 3];

        result[2, 0] = rhcsMatrix[1, 0];
        result[2, 2] = rhcsMatrix[1, 1];
        result[2, 1] = rhcsMatrix[1, 2];
        result[2, 3] = rhcsMatrix[1, 3];

        result[1, 0] = rhcsMatrix[2, 0];
        result[1, 2] = rhcsMatrix[2, 1];
        result[1, 1] = rhcsMatrix[2, 2];
        result[1, 3] = rhcsMatrix[2, 3];

        result[3, 0] = rhcsMatrix[3, 0];
        result[3, 2] = rhcsMatrix[3, 1];
        result[3, 1] = rhcsMatrix[3, 2];
        result[3, 3] = rhcsMatrix[3, 3];

        return result;
    }

    public static Vector3 PositionFromMatrix(Matrix4x4 m) {
        return m.GetColumn(3);
    }

    public static Vector3 ScaleFromMatrix(Matrix4x4 matrix) {
        Vector3 scale = new Vector3(
            matrix.GetColumn(0).magnitude,
            matrix.GetColumn(1).magnitude,
            matrix.GetColumn(2).magnitude
            );
        if(Vector3.Cross(matrix.GetColumn(0), matrix.GetColumn(1)).normalized != (Vector3)matrix.GetColumn(2).normalized) {
            scale.x *= -1;
        }
        return scale;
    }

    public static Quaternion RotationFromMatrix(Matrix4x4 matrix) {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public static Matrix4x4 createMatrix4x4From16Values(double m00, double m01, double m02, double m03, double m10, double m11, double m12, double m13, double m20, double m21, double m22, double m23, double m30, double m31, double m32, double m33) {
        Matrix4x4 m = new Matrix4x4();
        m.m00 = (float)m00;
        m.m01 = (float)m01;
        m.m02 = (float)m02;
        m.m03 = (float)m03;
        m.m10 = (float)m10;
        m.m11 = (float)m11;
        m.m12 = (float)m12;
        m.m13 = (float)m13;
        m.m20 = (float)m20;
        m.m21 = (float)m21;
        m.m22 = (float)m22;
        m.m23 = (float)m23;
        m.m30 = (float)m30;
        m.m31 = (float)m31;
        m.m32 = (float)m32;
        m.m33 = (float)m33;
        return m;
    }
}
