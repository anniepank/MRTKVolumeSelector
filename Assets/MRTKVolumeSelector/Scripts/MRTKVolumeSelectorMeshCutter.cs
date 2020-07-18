using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MRTKVolumeSelectorMeshCutter : MonoBehaviour
{
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

    public Mesh CutMeshToSelection(Mesh mesh, GameObject selectionArea)
    {
        var oldTriangles = mesh.triangles;
        var oldVertices = mesh.vertices;
        var oldNormals = mesh.normals;

        var neededVertexIndices = FindIndicesToLeave(oldVertices, selectionArea);

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
}
