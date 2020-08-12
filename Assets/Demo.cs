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
        MeshCutter.SaveMesh(mesh, "combined");

       MeshCutter.CutMeshToSelectionWithPlanes(mesh, Output, SelectionArea);
    }

    public void SaveCameraPosition()
    {
        var c_position = CameraManager.GetCameraPosition();
        if (c_position != null)
        {
            MeshCutter.Save(c_position.ToString(), "camera_position_" + frame_n.ToString());
            var mesh = MeshCutter.GetSpatialMesh();
            MeshCutter.SaveMesh(mesh, "combined");

            Task.Factory.StartNew(async () => MeshCutter.TryPostBytesAsync(await CameraManager.GetImage(), "frame"));

            frame_n += 1;
        }
        
    }

    public void StopPhotoCollection()
    {

    }
}
