using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegisterCT : MonoBehaviour
{
    private Matrix4x4 transformationMatrix;

    public static Vector3 ExtractTranslationFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 translate;
        translate.x = matrix.m03;
        translate.y = matrix.m13;
        translate.z = matrix.m23;
        return translate;
    }

    public static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix)
    {
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

    public Vector3 ExtractScaleFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }

    private Quaternion rotate3dflowToWorld(float rotation_x_3dflow, float rotation_y_3dflow, float rotation_z_3dflow)
    {
        var x_rot = Quaternion.AngleAxis(-rotation_x_3dflow, new Vector3(1, 0, 0));
        var y_rot = Quaternion.AngleAxis(-rotation_z_3dflow, new Vector3(0, 1, 0));
        var z_rot = Quaternion.AngleAxis(-rotation_y_3dflow, new Vector3(0, 0, 1));
        var rotation = x_rot * z_rot * y_rot;
        // rotation = rotation * Quaternion.AngleAxis(90, new Vector3(1, 0, 0));
        return rotation;

    }

    private Quaternion leftToRight(float rotation_x_3dflow, float rotation_y_3dflow, float rotation_z_3dflow)
    {
        var x_rot = Quaternion.AngleAxis(-rotation_x_3dflow, new Vector3(1, 0, 0));
        var y_rot = Quaternion.AngleAxis(rotation_y_3dflow, new Vector3(0, 1, 0));
        var z_rot = Quaternion.AngleAxis(rotation_z_3dflow, new Vector3(0, 0, 1));
        var rotation = x_rot * y_rot * z_rot;
        // rotation = rotation * Quaternion.AngleAxis(90, new Vector3(1, 0, 0));
        return rotation;

    }



    public void SetTransformFromMatrix(ref Matrix4x4 matrix)
    {
        var translation =  ExtractTranslationFromMatrix(ref matrix); 
        var rotation = matrix.rotation;
        /*
        transform.localRotation = rotate3dflowToWorld(rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z);
        transform.localPosition = new Vector3(-translation.x, translation.z, translation.y);
        */
        transform.localRotation = leftToRight(rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z);
        transform.localPosition = translation;

        // transform.localScale = ExtractScaleFromMatrix(ref transformedMatrix);
    }

    void Start()
    {
        transformationMatrix = new Matrix4x4();
        /* // no initial transformations
        transformationMatrix.SetRow(0, new Vector4(0.5195136f, -0.78994086f, 0.32572851f, 0.01120653f));
        transformationMatrix.SetRow(1, new Vector4(-0.85353408f, -0.46200015f, 0.24090544f, 0.11277414f));
        transformationMatrix.SetRow(2, new Vector4(-0.03981443f, -0.40317404f, -0.91425681f, 1.67416149f));
        transformationMatrix.SetRow(3, new Vector4(0, 0, 0, 1));
        */
        transformationMatrix.SetRow(0, new Vector4(0.41231283f, -0.3196773f, 0.85311462f, -0.04894491f));
        transformationMatrix.SetRow(1, new Vector4(0.07662024f, -0.92093084f, -0.38212004f, 1.66940049f));
        transformationMatrix.SetRow(2, new Vector4(0.90781467f, 0.22291884f, -0.35521785f, 0.11230378f));
        transformationMatrix.SetRow(3, new Vector4(0, 0, 0, 1));
        SetTransformFromMatrix(ref transformationMatrix);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(transform.localRotation.eulerAngles);

    }
}
