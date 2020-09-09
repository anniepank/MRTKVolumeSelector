using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class InsertCameraPositions : MonoBehaviour
{
    public GameObject Group;
    private Quaternion difference = new Quaternion();

    private GameObject create3dflowCameraCube(string name, Vector3 position, Quaternion rotation)
    {
        var cube_camera_3dflow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube_camera_3dflow.name = name;
        // !!! change y with y and add scale
        //cube_camera_3dflowcube_camera_3dflow.transform.position = new Vector3(x * scaleFactor, z * scaleFactor, y * scaleFactor);
        cube_camera_3dflow.transform.position = position;
        cube_camera_3dflow.transform.rotation = rotation;
        cube_camera_3dflow.transform.localScale = new Vector3(0.04f, 0.04f, 0.2f);
        cube_camera_3dflow.GetComponent<Renderer>().material = Resources.Load("Materials/Red", typeof(Material)) as Material;
        return cube_camera_3dflow;
    }

    private GameObject createUnityCameraCube(string name, Vector3 position, Quaternion rotation)
    {
        var cube_camera_real = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube_camera_real.name = name;
        cube_camera_real.transform.position = position;
        cube_camera_real.transform.rotation = rotation * Quaternion.AngleAxis(180, new Vector3(0, 1, 0));
        cube_camera_real.transform.localScale = new Vector3(0.04f, 0.04f, 0.2f);
        cube_camera_real.GetComponent<Renderer>().material = Resources.Load("Materials/Green", typeof(Material)) as Material;
        return cube_camera_real;
    }

    private Quaternion rotate3dflowToWorld(float rotation_x_3dflow, float rotation_y_3dflow, float rotation_z_3dflow)
    {
        var x_rot = Quaternion.AngleAxis(-rotation_x_3dflow, new Vector3(1, 0, 0));
        var y_rot = Quaternion.AngleAxis(-rotation_z_3dflow, new Vector3(0, 1, 0));
        var z_rot = Quaternion.AngleAxis(-rotation_y_3dflow, new Vector3(0, 0, 1));
        var rotation = x_rot * z_rot * y_rot; 
        rotation = rotation * Quaternion.AngleAxis(90, new Vector3(1, 0, 0));
        return rotation;

    }

    private Quaternion rotateWithout90Degrees(float rotation_x_3dflow, float rotation_y_3dflow, float rotation_z_3dflow)
    {
        var x_rot = Quaternion.AngleAxis(-rotation_x_3dflow, new Vector3(1, 0, 0));
        var y_rot = Quaternion.AngleAxis(-rotation_z_3dflow, new Vector3(0, 1, 0));
        var z_rot = Quaternion.AngleAxis(-rotation_y_3dflow, new Vector3(0, 0, 1));
        var rotation = x_rot * z_rot * y_rot;
        return rotation;
    }

    private GameObject insert3dflowCamera(string name, Vector3 position, Quaternion rotation)
    {
        var camera = new GameObject();
        camera.AddComponent<Camera>();
        camera.name = name;
        camera.transform.position = position;
        camera.transform.rotation = rotation;
        return camera;
    }


    void Start()
    {
#if UNITY_EDITOR
        var camerasReal = new List<GameObject>();
        var cameras3DFlow = new List<GameObject>();

        var camera3DFlow = new GameObject();
        var cameraTarget = new GameObject();

        var folder = @"H:\tmp\photogrammetry\workflow";
        string[] lines3DFlowPositionsFile = File.ReadAllLines(folder + @"\export\externals.txt", Encoding.UTF8);
        string[] linesRealPositionsFile = File.ReadAllLines(folder + @"\externals.txt", Encoding.UTF8);
        var scaleFactor = 0.095f;
        for(var i = 0; i < lines3DFlowPositionsFile.Length; i++)
        {
            // REAL
            var split = linesRealPositionsFile[i].Split(' ');
            var x = float.Parse(split[1]);
            var y = float.Parse(split[2]);
            var z = float.Parse(split[3]);

            var rotationXRealCamera = float.Parse(split[4]);
            var rotationYRealCamera = float.Parse(split[5]);
            var rotationZRealCamera = float.Parse(split[6]);

            var realCameraPosition = new Vector3(x, y, z);
            var realCameraRotation = Quaternion.Euler(rotationXRealCamera, rotationYRealCamera, rotationZRealCamera);

            var cubeCameraReal = createUnityCameraCube("green_camera_" + split[0], realCameraPosition, realCameraRotation);
            camerasReal.Add(cubeCameraReal);

            // 3D FLOW
            var line = lines3DFlowPositionsFile[i];
            split = line.Split(' ');
            x = float.Parse(split[1]);
            y = float.Parse(split[2]);
            z = float.Parse(split[3]);

            var rotationX3DFlow = float.Parse(split[4]);
            var rotationY3DFlow = float.Parse(split[5]);
            var rotationZ3DFlow = float.Parse(split[6]);

            var cameraPosition3DFlow = new Vector3(x * scaleFactor, z * scaleFactor, y * scaleFactor);
            var newRotation = rotate3dflowToWorld(rotationX3DFlow, rotationY3DFlow, rotationZ3DFlow);

            var cubeCamera3DFlow = create3dflowCameraCube("red_camera_" + split[0], cameraPosition3DFlow, newRotation);
            cameras3DFlow.Add(cubeCamera3DFlow);
            cubeCamera3DFlow.transform.parent = Group.transform;
          
            if (i == 5)
            {
                camera3DFlow = cubeCamera3DFlow;
                cameraTarget = cubeCameraReal;
                var quaternion3DFlow = rotate3dflowToWorld(rotationX3DFlow, rotationY3DFlow, rotationZ3DFlow);
                var quaternionReal = cubeCameraReal.transform.rotation;
                difference = quaternionReal * Quaternion.Inverse(quaternion3DFlow);
            }
        }
        /*
        foreach (var camera in cameras_3dflow) if (camera != camera_3dflow)
        {
           // MatchOneObject(camera, camera_3dflow, camera_target);

            camera.transform.rotation = difference * camera.transform.rotation;
        }
        */
        //model.transform.rotation = difference * model.transform.rotation;
        MatchCameras(camera3DFlow, cameraTarget, difference);
        //MatchOneObject(model, camera_3dflow, camera_target);
#endif
    }

    public void MatchCameras(GameObject cameraInGroup, GameObject targetCamera, Quaternion difference)
    {
        Group.transform.rotation = difference * Group.transform.rotation;
        Group.transform.localPosition += targetCamera.transform.position - cameraInGroup.transform.position;
    }

    public void MatchOneObject(GameObject what, GameObject cameraInGroup, GameObject targetCamera)
    {
        what.transform.rotation = difference * what.transform.rotation;
        what.transform.localPosition += (targetCamera.transform.position - difference * cameraInGroup.transform.position);

    }
}

