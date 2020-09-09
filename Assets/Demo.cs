using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public GameObject SelectionArea;
    public MRTKVolumeSelectorMeshCutter MeshCutter;
    public CameraManager CameraManager;

    public GameObject Output;

    private int frame_n = 0;
    public void DoCut()
    {
        var mesh = MeshCutter.GetSpatialMesh();
        // MeshCutter.SaveMesh(mesh, "combined");

       MeshCutter.CutMeshToSelectionWithPlanes(mesh, Output, SelectionArea);
    }

    public void SaveCameraPosition()
    {
        var mesh = MeshCutter.GetSpatialMesh();
        MeshCutter.SaveMesh(mesh, "combined");
        CameraManager.StopFrameCollection = false;
        Task.Factory.StartNew(async () => {
            await CameraManager.CollectFramesAsync();
            var result = "";
            foreach (var frame in CameraManager.Frames)
            {
                await MeshCutter.TryPostBytesAsync(frame, frame_n.ToString());
                var projectionMatrix = CameraManager.ProjectionMatrices[CameraManager.Frames.IndexOf(frame)];
                var c_position = CameraManager.GetCameraPosition(projectionMatrix);
                /*
                 MeshCutter.Save("(" + c_position.x.ToString("0.00000") + ", " + c_position.y.ToString("0.00000") + ", " + c_position.z.ToString("0.00000") 
                     + ")\n" + c_rotation.m00.ToString("0.00000") + "\t" + c_rotation.m01.ToString("0.00000") + "\t" + c_rotation.m02.ToString("0.00000") +  
                     "\n" + c_rotation.m10.ToString("0.00000") + "\t" + c_rotation.m11.ToString("0.00000") + "\t" + c_rotation.m12.ToString("0.00000")  
                     + "\n" + c_rotation.m20.ToString("0.00000") + "\t" + c_rotation.m21.ToString("0.00000") + "\t" + c_rotation.m22.ToString("0.00000")
                     // + ", " + c_rotation.z.ToString("0.00000") + ")",
                     ,"camera_info_" + frame_n);
                */
                
                var c_rotation = CameraManager.GetCameraRotationQuaternion(projectionMatrix);
                /*
                c_rotation.x = c_rotation.x * 180 / Mathf.PI;
                c_rotation.y = c_rotation.y * 180 / Mathf.PI;
                c_rotation.z = c_rotation.z * 180 / Mathf.PI;
                result += "\"frame" + frame_n + ".png\" " + c_position.x.ToString("0.00000") + " " + c_position.y.ToString("0.00000") + " " + c_position.z.ToString("0.00000") + " " +
                     c_rotation.x.ToString("0.00000") + " " + c_rotation.y.ToString("0.00000") + " " + c_rotation.z.ToString("0.00000") + "\n";
                */

                var euler = c_rotation.eulerAngles;
                result += "\"frame" + frame_n + ".png\" " + c_position.x.ToString("0.00000") + " " + c_position.y.ToString("0.00000") + " " + c_position.z.ToString("0.00000") + " " +
                     euler.x.ToString("0.00000") + " " + euler.y.ToString("0.00000") + " " + euler.z.ToString("0.00000") + "\n";


                /*
                var c_rotation = CameraManager.GetCameraRotation();
                result += "\"frame" + frame_n + ".png\" " + c_position.x.ToString("0.00000") + " " + c_position.y.ToString("0.00000") + " " + c_position.z.ToString("0.00000") + " " +
                    c_rotation.m00.ToString("0.00000") + " " + c_rotation.m01.ToString("0.00000") + " " + c_rotation.m02.ToString("0.00000") + " " +
                    c_rotation.m10.ToString("0.00000") + " " + c_rotation.m11.ToString("0.00000") + " " + c_rotation.m12.ToString("0.0  0000") + " " +
                    c_rotation.m20.ToString("0.00000") + " " + c_rotation.m21.ToString("0.00000") + " " + c_rotation.m22.ToString("0.00000") + "\n";
                */
                frame_n += 1;


            }
            MeshCutter.Save(result, "externals");
        });
        //Task.Factory.StartNew(async () => MeshCutter.TryPostBytesAsync(await CameraManager.GetImage(), "frame"));
        
    }


    public void StopPhotoCollection()
    {
        CameraManager.StopFrameCollection = true;
    }
}
