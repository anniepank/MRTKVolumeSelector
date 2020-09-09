using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CutObject : MonoBehaviour
{
    public MRTKVolumeSelectorMeshCutter MeshCutter;
    public GameObject Output;
    public GameObject SelectionArea;
    public GameObject BodyPartCopy;

    // Start is called before the first frame update
    void Start()
    {
        var mesh = gameObject.GetComponent<MeshFilter>().mesh;
        var localToWorldMatrix = gameObject.transform.localToWorldMatrix;
        var vertices = new List<Vector3>();
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        var meshFilterCopy = BodyPartCopy.GetComponent<MeshFilter>();
        meshFilterCopy.mesh = meshFilter.mesh;

        meshFilter.mesh.GetVertices(vertices);
        vertices = vertices.Select(v => localToWorldMatrix.MultiplyPoint(v)).ToList();

        meshFilterCopy.mesh.SetVertices(vertices);
        MeshCutter.CutMeshToSelectionWithPlanes(meshFilterCopy.mesh, Output, SelectionArea);
        var stringMesh = ObjExporter.MeshToString(Output.GetComponent<MeshFilter>().mesh);
        System.IO.File.WriteAllText(@"H:\tmp\registration\3dflow.obj", stringMesh);
    }

    private void BackObject()
    {

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
