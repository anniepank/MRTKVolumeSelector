using EzySlice;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel;
using System.IO;
using System.Text;
using Dummiesman;


#if !UNITY_EDITOR
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.Web.Http;
#else
using System.Net.Http;
#endif

public class MRTKVolumeSelectorMeshCutter : MonoBehaviour
{

    public GameObject Plane;
    public GameObject OriginalMesh;
    public GameObject Intersection;

    public bool CTTransformIsReady = false;
    public bool CTObjLoadingIsReady = false;

    public object locker;
    public Quaternion CTRotation = new Quaternion();
    public Vector3 CTPosition = new Vector3();
    public Vector3 CTScale = new Vector3(1, 1, 1); // new Vector3(0.012f, 0.012f, 0.012f);

    public MeshLoaderInfo meshModelLoader = new MeshLoaderInfo();
    private byte[] ctObj;
#if !UNITY_EDITOR && UNITY_WSA
    private readonly StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
#endif

    public MRTKVolumeSelectorMeshCutter()
    {
       locker = new object();
    }
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
  
    void OnDrawGizmosSelected()
    {
        var plane1 = new UnityEngine.Plane(new Vector3(1f, 0f, 0f), new Vector3(-0.79f, -0.68f, 0.85f));
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

    public GameObject[] EzyCut(Vector3 point, Vector3 n, GameObject meshObject, GameObject selectionArea, bool intersectionOnly)
    {
        n = selectionArea.transform.TransformDirection(n);
        point = selectionArea.transform.TransformPoint(point);

        return meshObject.SliceInstantiate(new EzySlice.Plane(point, -n), intersectionOnly);
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

        foreach (var n in planes.Keys)
        {
            var point = planes[n];

            var res = EzyCut((Vector3)point, (Vector3)n, meshObject, selectionArea, true);
            MeshFilter newMeshFilter = null;
            if (res is null)
            {
                newMeshFilter = meshObject.GetComponent<MeshFilter>();

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
            if (res != null && res.Length == 2)
            {
                var listMeshFilters = new List<MeshFilter>() { newMeshFilter, meshFilter };
                var combined = CombineMeshes(listMeshFilters);
                meshFilter.mesh = combined;
            }
            meshObject.GetComponent<MeshFilter>().mesh = meshFilter.mesh;

        }
        meshObject.SetActive(true);
        SaveMesh(meshObject.GetComponent<MeshFilter>().mesh, "cut");
        Destroy(meshFilter);
    }

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

    public void SaveMesh(Mesh mesh, string filename)
    {
            var stringMesh = ObjExporter.MeshToString(mesh);
            Save(stringMesh, filename);
    }

    public void Save(string str, string filename)
    {
        Task.Factory.StartNew(() => TryPostJsonAsync(str, filename));
    }

    public async Task TryPostBytesAsync(byte[] data, string filename)
    {
        try
        {
#if !UNITY_EDITOR
            HttpClient httpClient = new HttpClient();
            var byteContent = new HttpBufferContent(data.AsBuffer());
#else
            HttpClient httpClient = new HttpClient();
            var byteContent = new ByteArrayContent(data);
#endif
            Uri uri = new Uri("http://10.10.1.134:9000/frame" + filename);

            HttpResponseMessage reponse = await httpClient.PostAsync(uri, byteContent);
            reponse.EnsureSuccessStatusCode();
            var httpResponseBody = await reponse.Content.ReadAsStringAsync();

        }
        catch (Exception ex)
        {

        }
       
    }

    private async Task TryPostJsonAsync(string obj, string filename)
    {
#if !UNITY_EDITOR
        try
        {
            HttpClient httpClient = new HttpClient();
            Uri uri = new Uri("http://10.10.1.134:9000/" + filename);
            HttpStringContent content = new HttpStringContent(
                obj,
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/json");
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(uri,
content);

            httpResponseMessage.EnsureSuccessStatusCode();
            var httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            // Write out any exceptions.
        }
#endif
    }

    public async Task<string> TryGetAsync()
    {
        try
        {
            HttpClient httpClient = new HttpClient();
            Uri uri = new Uri("http://10.10.1.134:9000/getCTTransform");
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(uri);
            httpResponseMessage.EnsureSuccessStatusCode();
            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            var transformString = responseString.Split(new string[] { "CT_SCAN" }, StringSplitOptions.None)[0];
            var lineRotation = transformString.Split('\n')[0];
            var linePosition = transformString.Split('\n')[1];
            
            var qX = float.Parse(lineRotation.Split(' ').ToList()[0]);
            var qY = float.Parse(lineRotation.Split(' ').ToList()[1]);
            var qZ = float.Parse(lineRotation.Split(' ').ToList()[2]);
            var qW = float.Parse(lineRotation.Split(' ').ToList()[3]);

            var x = float.Parse(linePosition.Split(' ').ToList()[0]);
            var y = float.Parse(linePosition.Split(' ').ToList()[1]);
            var z = float.Parse(linePosition.Split(' ').ToList()[2]);

            var res = new Dictionary<string, List<float>>();
            lock (locker)
            {
                CTRotation = new Quaternion(qX, qY, qZ, qW);
                CTPosition = new Vector3(x, y, z);
                CTTransformIsReady = true;
            }

            return responseString.Split(new string[] { "CT_SCAN" }, StringSplitOptions.None)[1];

        }
        catch (Exception ex)
        {
            // Write out any exceptions.
        }
        return "";
        }

    public async Task SaveObjFileAsync(string filename, byte[] obj)
    {
#if !UNITY_EDITOR && UNITY_WSA

        var file = await _localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
        using (var destination = await file.OpenStreamForWriteAsync())
        {
            await destination.WriteAsync(obj, 0, obj.Length);
        }
#else   
        File.WriteAllBytes("H:\\App\\tmp\\" + filename, obj);
#endif
    }

    private async Task<Stream> GetReadFileStreamAsync(string filename)
    {
#if !UNITY_EDITOR && UNITY_WSA
        var file = await _localFolder.GetFileAsync(filename);
        return await file.OpenStreamForReadAsync();
#else
        return File.OpenRead("H:\\App\\tmp\\" + filename);
#endif
    }

    public async Task InsertCT() {

        var ctObj = await TryGetAsync();
        await SaveObjFileAsync("ct.obj", Encoding.UTF8.GetBytes(ctObj));
        var fileStream = await GetReadFileStreamAsync("ct.obj");
        var loader = new Dummiesman.OBJLoader();
        var builder = loader.CustomLoad(fileStream);
        meshModelLoader = new MeshLoaderInfo(builder, loader);
        lock(locker)
        {
            CTObjLoadingIsReady = true;
        }
    }

    public Mesh CombineMeshes(List<MeshFilter> meshFilters)
    {
        var combine = new CombineInstance[meshFilters.Count];

        int i = 0;
        while (i < meshFilters.Count)
        {
            combine[i].mesh = meshFilters[i].mesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            i++;
        }
        // return Combine(combine);

        var mesh = new Mesh();
        if (combine.Length > 0)
            mesh.CombineMeshes(combine);
        return mesh;
    }

    public Mesh Combine(IEnumerable<CombineInstance> meshes)
    {
        var vs = new List<Vector3>();
        var ts = new List<int>();
        int vOffset = 0;
        foreach (var ci in meshes)
        {
            var mVs = ci.mesh.vertices;
            for (int i = 0; i < mVs.Length; i++)
            {
                vs.Add(ci.transform * mVs[i]);
            }

            var mTs = ci.mesh.triangles;
            for (int i = 0; i < mTs.Length; i++)
            {
                ts.Add(vOffset + mTs[i]);
            }

            vOffset += mVs.Length;
        }
        var mesh = new Mesh();
        mesh.SetVertices(vs);
        mesh.SetTriangles(ts, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

}
