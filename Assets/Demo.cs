using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public GameObject SelectionArea;
    public MRTKVolumeSelectorMeshCutter MeshCutter;

    public GameObject Output;
    public void DoCut()
    {
        var mesh = MeshCutter.GetSpatialMesh();
        MeshCutter.CutMeshToSelectionWithPlanes(mesh, Output, SelectionArea);
    }
    /*
    public MeshFilter Output;

    public void DoCut()
    {
        var mesh = MeshCutter.GetSpatialMesh();
        Output.mesh = MeshCutter.CutMeshToSelection(mesh, SelectionArea);
    }
    */
}
