using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public GameObject SelectionArea;
    public MRTKVolumeSelectorMeshCutter MeshCutter;
    public MeshFilter Output;

    public void DoCut()
    {
        Output.mesh = MeshCutter.CutMeshToSelection(MeshCutter.GetSpatialMesh(), SelectionArea);    
    }
}
