using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
#if !UNITY_EDITOR
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.Web.Http;
using System.Runtime.InteropServices.WindowsRuntime;

#else
using System.Net.Http;
#endif


public class ServerService
{
    private HttpClient httpClient;
    private Quaternion CTRotation;
    private Vector3 CTPosition;

    public ServerService()
    {
        httpClient = new HttpClient();
    }

    public async Task<string> GetCT()
    {
        return await TryGetAsync();
    }

    public Quaternion GetCTRotation()
    {
        return CTRotation;
    }

    public Vector3 GetCTPosition()
    {
        return CTPosition;
    }

    public void SendString(string str, string filename)
    {
        Task.Factory.StartNew(() => TryPostJsonAsync(str, filename));
    }

    public async Task SendFrame(byte[] data, string filename)
    {
        await TryPostBytesAsync(data, filename);
    }

    public void SendMesh(Mesh mesh, string filename)
    {
        var stringMesh = ObjExporter.MeshToString(mesh);
        SendString(stringMesh, filename);
    }


    public void SaveMesh(Mesh mesh, string filename)
    {
        var stringMesh = ObjExporter.MeshToString(mesh);
        SendString(stringMesh, filename);
    }

    public async Task TryPostBytesAsync(byte[] data, string filename)
    {
        try
        {
#if !UNITY_EDITOR
            var byteContent = new HttpBufferContent(data.AsBuffer());
#else
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


    private async Task<string> TryGetAsync()
    {
        try
        {
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

            return responseString.Split(new string[] { "CT_SCAN" }, StringSplitOptions.None)[1];

        }
        catch (Exception ex)
        {
            // Write out any exceptions. return null; // ?
        }
        return "";
    }
}
