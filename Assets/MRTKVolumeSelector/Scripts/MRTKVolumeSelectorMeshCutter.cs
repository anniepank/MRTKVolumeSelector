using EzySlice;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections.Specialized;

public class MRTKVolumeSelectorMeshCutter : MonoBehaviour
{

    public GameObject Plane;
    public GameObject OriginalMesh;
    public GameObject Intersection;

    private bool IsVertexInAlignedAxisCube(Vector3 v)
    {
        return v.x >= -0.5f && v.x <= 0.5f &&
            v.y >= -0.5f && v.y <= 0.5f &&
            v.z >= -0.5f && v.z <= 0.5f;
    }

    private bool IsVertexInCube(Vector3 v, GameObject cube)
    {
        var vertexInCubeCoordinateSystem = cube.transform.InverseTransformPoint(v);
        return IsVertexInAlignedAxisCube(vertexInCubeCoordinateSystem);
    }

    private List<int> FindIndicesToLeave(Vector3[] vertices, GameObject selectionCube)
    {
        var result = new List<int>();
        for (var i = 0; i < vertices.Length; i++)
        {
            if (IsVertexInCube(vertices[i], selectionCube))
            {
                result.Add(i);
            }
        }

        return result;
    }

    

    /*

    /// <summary>
    /// Replace the mesh with tempMesh.
    /// </summary>
    Mesh ReplaceMesh(Mesh mesh, TempMesh tempMesh, MeshCollider collider = null)
    {
        var res = Instantiate(mesh);
        res.SetVertices(tempMesh.vertices);
        res.SetTriangles(tempMesh.triangles, 0);
        res.SetNormals(tempMesh.normals);
        // mesh.SetUVs(0, tempMesh.uvs);

        //mesh.RecalculateNormals();
        res.RecalculateTangents();

        if (collider != null && collider.enabled)
        {
            collider.sharedMesh = mesh;
            collider.convex = true;
        }

        return res;
    }
    */
    void OnDrawGizmosSelected()
    {
        var plane1 = new UnityEngine.Plane(new Vector3(1f, 0f, 0f), new Vector3(-0.79f, -0.68f, 0.85f));
        // new Vector3(-0.79f, -0.68f, 0.85f));
        // new Vector3(0.04f, 0.68f, 0.85f));
        // Draws a blue line from this transform to the target
        Gizmos.color = Color.blue;
        var n = 5;
        for (var i = -n; i < n; i++)
        {
            for (var j = -n; j < n; j++)
            {
                for (var k = -n; k < n; k++)
                {
                    var projection = plane1.ClosestPointOnPlane(new Vector3(i, j, k));
                    Gizmos.DrawLine(plane1.ClosestPointOnPlane(new Vector3(0, 0, 0)), projection);
                }

            }
        }
    }
    /*
    public Mesh CutMeshToSelectionWithPlanes(Mesh mesh, GameObject selectionArea)
    {
        var newMesh = mesh;
        var n2 = new Vector3(1, 0, 0);
        n2 = selectionArea.transform.TransformDirection(n2);
        var point2 = new Vector3(0.5f, 0, 0);
        point2 = selectionArea.transform.TransformPoint(point2);
        var plane2 = new Plane(n2, point2);

        newMesh = CutMeshWithPlane(newMesh, ref plane2);

      
        return newMesh;
        var n1 = new Vector3(1, 0, 0);
        n1 = selectionArea.transform.InverseTransformDirection(n1);
        var point1 = new Vector3(0.5f, 0, 0);
        point1 = selectionArea.transform.TransformPoint(point1);
        var plane1 = new Plane(n1, point1);

        newMesh = CutMeshWithPlane(mesh, ref plane1);

        return newMesh;
    }

    public Mesh CutMeshWithPlane(Mesh mesh, ref Plane plane)
    {
        var meshCutter = new MeshCutter(256);
        var res = meshCutter.SliceMesh(mesh, ref plane);
        if (!res)
        {
            return null;
        }
        var newMesh = meshCutter.NegativeMesh;
        return ReplaceMesh(mesh, newMesh);
    }
    */

    public GameObject[] EzyCut(Vector3 point, Vector3 n, GameObject meshObject, GameObject selectionArea, bool intersectionOnly)
    {
        n = selectionArea.transform.TransformDirection(n);
        point = selectionArea.transform.TransformPoint(point);

        var res = meshObject.SliceInstantiate(new EzySlice.Plane(point, -n), intersectionOnly);
        /*
        if (res is null)
        {
            Debug.Log("nothing to cut is null");
        }
        else if (res.Length == 2)
        {
            return res; // meshObject.GetComponent<MeshFilter>().mesh = res[0].GetComponent<MeshFilter>().mesh;
            Destroy(res[0]);
            Destroy(res[1]);
        }
        return null;
        */
        return res;
    }

    public void CutMeshToSelectionWithPlanes(Mesh mesh, GameObject meshObject, GameObject selectionArea)
    {
        meshObject.GetComponent<MeshFilter>().mesh = mesh;

        var planes = new OrderedDictionary() {
            { new Vector3(1, 0, 0), new Vector3(0.5f, 0, 0) },
            { new Vector3(-1, 0, 0), new Vector3(-0.5f, 0, 0) },
            { new Vector3(0, 1, 0), new Vector3(0, 0.5f, 0) },
            { new Vector3(0, -1, 0), new Vector3(0, -0.5f, 0) },
            { new Vector3(0, 0, 1), new Vector3(0, 0, 0.5f) },
            { new Vector3(0, 0, -1), new Vector3(0, 0, -0.5f) },
        };
        var meshFilter = gameObject.AddComponent<MeshFilter>();

        var i = 0;
        foreach (var n in planes.Keys)
        {
            var point = planes[n];

            var res = EzyCut((Vector3)point, (Vector3)n, meshObject, selectionArea, true);
            MeshFilter newMeshFilter = null;
            if (res is null)
            {
                newMeshFilter = meshObject.GetComponent<MeshFilter>();
                continue;
            }
            else if (res.Length == 2)
            {
                newMeshFilter = res[0].GetComponent<MeshFilter>();
                Destroy(res[0]);
                Destroy(res[1]);
            }

            meshFilter.mesh = CutMeshToSelection(meshObject.GetComponent<MeshFilter>().mesh, (vertices) =>
            {
                var result = new List<int>();
                var worldN = selectionArea.transform.TransformDirection((Vector3)n);
                var worldPoint = selectionArea.transform.TransformPoint((Vector3)point);
                var plane = new UnityEngine.Plane(worldN, worldPoint);
                for (var j = 0; j < vertices.Length; j++)
                {
                    if (!plane.GetSide(vertices[j]))
                    {
                        result.Add(j);
                    }
                }

                return result;
            });
            if (res.Length == 2)
            {
                var listMeshFilters = new List<MeshFilter>() { newMeshFilter, meshFilter };
                var combined = CombineMeshes(listMeshFilters);
                meshFilter.mesh = combined;
            }
            meshObject.GetComponent<MeshFilter>().mesh = meshFilter.mesh;


            /*
            var allPlanes = new OrderedDictionary() {
                { new Vector3(1, 0, 0), new Vector3(0.5f, 0, 0) },
                { new Vector3(-1, 0, 0), new Vector3(-0.5f, 0, 0) },
                { new Vector3(0, 1, 0), new Vector3(0, 0.5f, 0) },
                { new Vector3(0, -1, 0), new Vector3(0, -0.5f, 0) },
                { new Vector3(0, 0, 1), new Vector3(0, 0, 0.5f) },
                { new Vector3(0, 0, -1), new Vector3(0, 0, -0.5f) },
            };
            allPlanes.RemoveAt(i);
            // allPlanes.Insert(0, n, point);

            var originalMeshFilter = OriginalMesh.GetComponent<MeshFilter>();
            var res = EzyCut((Vector3)point, (Vector3)n, OriginalMesh, selectionArea, true);
            MeshFilter newMeshFilter = new MeshFilter();
            if (res is null)
            {
                newMeshFilter = originalMeshFilter;
                continue;
            }
            else if (res.Length == 2)
            {
                newMeshFilter = res[0].GetComponent<MeshFilter>();
                Destroy(res[0]);
                Destroy(res[1]);
            }

            Intersection.GetComponent<MeshFilter>().mesh = newMeshFilter.mesh;

            var meshFilterVerticiesCut = new GameObject();
            meshFilterVerticiesCut.AddComponent<MeshFilter>();
            meshFilterVerticiesCut.GetComponent<MeshFilter>().mesh = CutMeshToSelection(meshObject.GetComponent<MeshFilter>().mesh, selectionArea);
            var listMeshFilters = new List<MeshFilter>() { newMeshFilter, meshFilterVerticiesCut.GetComponent<MeshFilter>() };
            var combined = CombineMeshes(listMeshFilters);
            
            Destroy(meshFilterVerticiesCut);
            meshObject.GetComponent<MeshFilter>().mesh = combined;

            var j = 0;
            foreach (var normal in allPlanes.Keys)
            {
                if (j != i)
                {
                    var p = allPlanes[normal];
                    var previousMesh = Intersection.GetComponent<MeshFilter>().mesh;
                    res = EzyCut((Vector3)p, (Vector3)normal, Intersection, selectionArea, false);
                    if (res is null)
                    {
                        Intersection.GetComponent<MeshFilter>().mesh = previousMesh;
                    }
                    else if (res.Length == 2)
                    {
                        Intersection.GetComponent<MeshFilter>().mesh = res[0].GetComponent<MeshFilter>().mesh;
                        Destroy(res[0]);
                        Destroy(res[1]);
                    }
                }
                j++;
            }
            i++;

            var list = new List<MeshFilter>() { Intersection.GetComponent<MeshFilter>(), meshObject.GetComponent<MeshFilter>() };
            meshObject.GetComponent<MeshFilter>().mesh = CombineMeshes(list);
        */
        }
        Destroy(meshFilter);
    }
    //  (v) => Findindi(oldVer, select)
    // ()
    public Mesh CutMeshToSelection(Mesh mesh, Func<Vector3[], List<int>> selectVertices)
    {
        var oldTriangles = mesh.triangles;
        var oldVertices = mesh.vertices;
        var oldNormals = mesh.normals;

        var neededVertexIndices = selectVertices(oldVertices);

        // update vertices and normals

        var newVertices = new Vector3[neededVertexIndices.Count];
        var newNormals = new Vector3[neededVertexIndices.Count];

        var vertexIndexMapping = new Dictionary<int, int>();
        var newVertexIndex = 0;
        foreach(var index in neededVertexIndices)
        {
            vertexIndexMapping[index] = newVertexIndex;
            newVertices[newVertexIndex] = oldVertices[index];
            newNormals[newVertexIndex++] = oldNormals[index];
        }

        // update triangles

        var newTrianglesList = new List<int>(oldTriangles.Length);

        var neededVertexIndicesSet = new HashSet<int>(neededVertexIndices);

        for (var i = 0; i < oldTriangles.Length; i += 3)
        {
            var v1 = oldTriangles[i];
            var v2 = oldTriangles[i + 1];
            var v3 = oldTriangles[i + 2];

            if (neededVertexIndicesSet.Contains(v1) && neededVertexIndicesSet.Contains(v2) && neededVertexIndicesSet.Contains(v3))
            {
                newTrianglesList.Add(vertexIndexMapping[v1]);
                newTrianglesList.Add(vertexIndexMapping[v2]);
                newTrianglesList.Add(vertexIndexMapping[v3]);
            }
        }
        var newTriangles = newTrianglesList.ToArray();

        var resultMesh = Instantiate(mesh);
        resultMesh.triangles = newTriangles;
        resultMesh.vertices = newVertices;
        resultMesh.normals = newNormals;
        resultMesh.RecalculateTangents();

        return resultMesh;
    }

    public Mesh GetSpatialMesh()
    {
        var meshObserver = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();
        var meshFilters = new List<MeshFilter>(from meshFilter in meshObserver.Meshes.Values select meshFilter.Filter);

        if (meshFilters.Count > 0)
        {
            return CombineMeshes(new List<MeshFilter>(from meshFilter in meshObserver.Meshes.Values select meshFilter.Filter));
        }
        return new Mesh();
    }

    public Mesh CombineMeshes(List<MeshFilter> meshFilters)
    {
        var combine = new CombineInstance[meshFilters.Count];

        int i = 0;
        while (i < meshFilters.Count)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            i++;
        }
        var mesh = new Mesh();
        if (combine.Length > 0)
            mesh.CombineMeshes(combine);
        return mesh;
    }

    public Vector3 FindPlaneLineIntersection(Ray ray, UnityEngine.Plane plane)
    {
        float enter;
        plane.Raycast(ray, out enter);
        return ray.GetPoint(enter);
    }

}
