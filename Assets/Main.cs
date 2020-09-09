using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Dummiesman;

public class Main : MonoBehaviour
{
    public GameObject Room;
    public GameObject CT;
    public GameObject Model3DFlow;
    public GameObject Group;

    private string folder = @"H:\tmp\photogrammetry\workflow\";
    private float ctScaleFactor = 0.012f;

    private Dictionary<string, Vector3> GetCoordinates(string filepath)
    {
        var positions = new Dictionary<string, Vector3>();
        var lines = new List<string>(File.ReadLines(filepath));
        for (var i=0; i < lines.Count; i++)
        {
            if (!lines[i].Contains("#"))
            {
                var split = lines[i].Split(' ');
                var coordinates = new Vector3(
                    float.Parse(split[1]),
                    float.Parse(split[2]),
                    float.Parse(split[3])
                    );
                positions.Add(split[0], coordinates);
            }
        }
        return positions;
    }
    
    public float GetScaleFactor(Dictionary<string, Vector3> realCoordinates, Dictionary<string, Vector3> flowCoordinates) 
    {
        // not all of the real coordinates might be in the export file from 3dflow,
        // while it sometimes uses less frames than were given in the input
        var numberOfDistances = 0;
        var averageScale = 0f;
        foreach(var flowCoordinate1 in flowCoordinates)
        {
            var frame1 = flowCoordinate1.Key;
            foreach (var flowCoordinate2 in flowCoordinates)
            {
                var frame2 = flowCoordinate2.Key;

                if (frame1 != frame2 && realCoordinates.ContainsKey(frame1) && flowCoordinates.ContainsKey(frame2))
                {
                    var distanceReal = Vector3.Distance(realCoordinates[frame1], realCoordinates[frame2]);
                    var distanceFlow = Vector3.Distance(flowCoordinates[frame1], flowCoordinates[frame2]);

                    averageScale += distanceReal / distanceFlow;
                    numberOfDistances += 1;
                }
            }
        }

        averageScale /= numberOfDistances;
        return averageScale;
    }

    public void PrepareCTFileForRegistration(string filepath, float scaleFactor)
    {
        var builder = new StringBuilder();
        var lines = new List<string>(File.ReadLines(filepath));
        
        using (var fileObj = new StreamWriter(folder + @"ct\ct_scaled_axes_switched.obj"))
        {
            using (var fileForRegistration = new StreamWriter(folder + @"ct\ct_scaled_axes_switched.xyz"))
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    if (lines[i][0] == 'v')
                    {

                        var split = lines[i].Split(' ');
                        var coordinates = new Vector3(
                            float.Parse(split[1]),
                            float.Parse(split[2]),
                            float.Parse(split[3])
                            );
                        fileObj.WriteLine("v " + (-coordinates.x * scaleFactor).ToString("0.00000") + " "
                        + (coordinates.z * scaleFactor).ToString("0.00000") + " "
                        + (coordinates.y * scaleFactor).ToString("0.00000"));

                        fileForRegistration.WriteLine((-coordinates.x * scaleFactor).ToString("0.00000") + " "
                        + (coordinates.z * scaleFactor).ToString("0.00000") + " "
                        + (coordinates.y * scaleFactor).ToString("0.00000"));
                    }
                    else
                    {
                        fileObj.WriteLine(lines[i]);
                    }
                }
            }
        }
    }

    public void Prepare3DFlowModelForRegistration(string filepath, float scaleFactor)
    {
        var builder = new StringBuilder();
        var lines = new List<string>(File.ReadLines(filepath));
        
        using (var file = new StreamWriter(folder + @"export\3dflow_scaled_axes_switched.xyz"))
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i][0] == 'v')
                {

                    var split = lines[i].Split(' ');
                    var coordinates = new Vector3(
                        float.Parse(split[1]),
                        float.Parse(split[2]),
                        float.Parse(split[3])
                        );

                    file.WriteLine((-coordinates.x * scaleFactor).ToString("0.00000") + " "
                    + (coordinates.z * scaleFactor).ToString("0.00000") + " "
                    + (coordinates.y * scaleFactor).ToString("0.00000"));
                }
            }
        }

       
    }

    public void TransformCT(string filepath, string transformationMatrixFile)
    {
        var builder = new StringBuilder();
        var matrixFileLines = new List<string>(File.ReadLines(transformationMatrixFile));
        var objLines = new List<string>(File.ReadLines(filepath));
        var r0 = matrixFileLines[0].Split(' ');
        var r1 = matrixFileLines[1].Split(' ');
        var r2 = matrixFileLines[2].Split(' ');
        var r3 = matrixFileLines[3].Split(' ');
        var transformationMatrix = new Matrix4x4();
        transformationMatrix.m00 = float.Parse(r0[0]);
        transformationMatrix.m01 = float.Parse(r0[1]);
        transformationMatrix.m02 = float.Parse(r0[2]);
        transformationMatrix.m03 = float.Parse(r0[3]);
        transformationMatrix.m10 = float.Parse(r1[0]);
        transformationMatrix.m11 = float.Parse(r1[1]);
        transformationMatrix.m13 = float.Parse(r1[3]);
        transformationMatrix.m12 = float.Parse(r1[2]);
        transformationMatrix.m20 = float.Parse(r2[0]);
        transformationMatrix.m21 = float.Parse(r2[1]);
        transformationMatrix.m22 = float.Parse(r2[2]);
        transformationMatrix.m23 = float.Parse(r2[3]);
        transformationMatrix.m30 = float.Parse(r3[0]);
        transformationMatrix.m31 = float.Parse(r3[1]);
        transformationMatrix.m32 = float.Parse(r3[2]);
        transformationMatrix.m33 = float.Parse(r3[3]);

        var sw = new Stopwatch();
        sw.Start();
        using (var file = new StreamWriter(folder + @"export\transformed_ct.obj"))
        {
            for (var i = 0; i < objLines.Count; i++)
            {
                if (objLines[i][0] == 'v')
                {

                    var split = objLines[i].Split(' ');
                    var point = new Vector3(
                        float.Parse(split[1]),
                        float.Parse(split[2]),
                        float.Parse(split[3])
                        );

                    var result = transformationMatrix.MultiplyPoint(point);
                    file.WriteLine("v " + result.x.ToString() + " " + result.y.ToString() + " " + result.z.ToString());
                } else
                {
                    file.WriteLine(objLines[i]);
                }
            }
        }

        sw.Stop();
        UnityEngine.Debug.Log("3dflow preparation time");
        UnityEngine.Debug.Log(sw.Elapsed);
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

    public void MatchCameras(Vector3 currentPotision, Vector3 targetPosition, Quaternion difference)
    {
        Group.transform.rotation = difference * Group.transform.rotation;
        Group.transform.localPosition += targetPosition - difference * currentPotision;
    }


    public void SetGroupTransform(float scaleFactor)
    {
        var camera3DFlow = new GameObject();
        var cameraTarget = new GameObject();
        var worldsRotationDifference = new Quaternion();

        string[] lines3DFlowPositionsFile = File.ReadAllLines(folder + @"\export\externals.txt", Encoding.UTF8);
        string[] linesRealPositionsFile = File.ReadAllLines(folder + @"\externals.txt", Encoding.UTF8);
        for (var i = 0; i < lines3DFlowPositionsFile.Length; i++)
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

            if (i == 5)
            {
                var quaternion3DFlow = rotate3dflowToWorld(rotationX3DFlow, rotationY3DFlow, rotationZ3DFlow);
                var quaternionReal = realCameraRotation * Quaternion.AngleAxis(180, new Vector3(0, 1, 0));
                worldsRotationDifference = quaternionReal * Quaternion.Inverse(quaternion3DFlow);

                MatchCameras(cameraPosition3DFlow, realCameraPosition, worldsRotationDifference);
            }
        }
    }

    private void run_cmd()
    {
        Process p = new Process();
        p.StartInfo = new ProcessStartInfo()
        {
            FileName = "cmd.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        p.Start();
        using (var sw = p.StandardInput)
        {
            if (sw.BaseStream.CanWrite)
            {
                sw.WriteLine(@"C:\ProgramData\Anaconda3\Scripts\activate.bat");
                sw.WriteLine("\"C:\\ProgramData\\Anaconda3\\python.exe\" \"" + folder + "registration.py\"");
            }
        }
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        UnityEngine.Debug.Log(output);
    }

    void Start()
    {
        ObjImporter objImporter = new ObjImporter();
        Room.GetComponent<MeshFilter>().mesh = objImporter.ImportFile(folder + "room_knees.obj", false);
        Model3DFlow.GetComponent<MeshFilter>().mesh = objImporter.ImportFile(folder + @"export\3dflow.obj", true);

        var realPositions = GetCoordinates(folder + "externals.txt");
        var flowPositions = GetCoordinates(folder + @"export\externals.txt");

        var scaleFactor = GetScaleFactor(realPositions, flowPositions);

        SetGroupTransform(scaleFactor);

        Model3DFlow.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        /*
        var sw = new Stopwatch();
        sw.Start();
        Prepare3DFlowModelForRegistration(folder + @"export\3dflow.obj", scaleFactor); // export\3dflow_scaled_axes_switched.xyz
        sw.Stop();
        UnityEngine.Debug.Log("3dflow preparation time");
        UnityEngine.Debug.Log(sw.Elapsed);
        */
        /*
        sw.Start();
        PrepareCTFileForRegistration(folder + @"ct\465_model_509E2E540E363C39FCC3B1981E7D1C48_knee_skin.obj", ctScaleFactor); // ct\ct_scaled_axes_switched.obj ct\ct_scaled_axes_switched.xyz
        sw.Stop();
        UnityEngine.Debug.Log("ct preparation time");
        UnityEngine.Debug.Log(sw.Elapsed);
        */
        /*
        sw.Start();
        run_cmd();
        sw.Stop();
        UnityEngine.Debug.Log("ct registration time");
        UnityEngine.Debug.Log(sw.Elapsed);
        */

        // TransformCT(folder + @"ct\ct_scaled_axes_switched.obj", folder + @"export\transformation_matrix.txt");
        var loadedObj = new OBJLoader().Load(folder + @"export\transformed_ct.obj");
        var obj = Instantiate(loadedObj);
        CT.GetComponent<MeshFilter>().mesh = obj.transform.GetChild(0).GetComponent<MeshFilter>().mesh;
        Destroy(obj);
        Destroy(loadedObj);
    }
}
