using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public GameObject SelectionArea;
    public MRTKVolumeSelectorMeshCutter MeshCutter;
    public CameraManager CameraManager;
    public GameObject Output;
    public GameObject CT;

    private Vector3 firstCameraPosition;
    private MeshLoaderInfo meshModelLoaderInfo;
    public bool CTObjLoadingIsReady = false;
    public Quaternion CTRotation = new Quaternion();
    public Vector3 CTPosition = new Vector3();
    public Vector3 CTScale = new Vector3(1, 1, 1);

    public object locker = new object();
    private int frame_n = 0;

    public void Update()
    {
        UpdateCTState();
    }

    public void DoCut()
    {
        var mesh = MeshCutter.GetSpatialMesh();
        // MeshCutter.SaveMesh(mesh, "combined");

       MeshCutter.CutMeshToSelectionWithPlanes(mesh, Output, SelectionArea);
    }

    public void UpdateCTState()
    {
        lock (locker)
        {
            if (CTObjLoadingIsReady)
            {
                CT.transform.localRotation = CTRotation;
                CT.transform.localPosition += CTPosition;

                var loader = meshModelLoaderInfo.Loader;
                var mesh = loader.CustomCombinator(meshModelLoaderInfo.Builder);
                var obj = Instantiate(mesh);
                obj.transform.localRotation = CTRotation;
                // obj.transform.localPosition += CTPosition;
                obj.transform.localPosition = firstCameraPosition;
                obj.transform.localScale = new Vector3(1f, 1f, 1f); // MeshCutter.CTScale;
                obj.transform.GetChild(0).transform.localScale = new Vector3(1, 1, 1);

                CT.GetComponent<MeshFilter>().mesh = obj.transform.GetChild(0).GetComponent<MeshFilter>().mesh;
                // Destroy(obj);
                // Destroy(mesh);

                CTObjLoadingIsReady = false;
            }
        }
      
    }

    private async Task InsertCT(ServerService serverService)
    {
        var fileService = new FileSystemService();

        var ctObj = await serverService.GetCT();
        await fileService.SaveObjFileAsync("ct.obj", Encoding.UTF8.GetBytes(ctObj));
        var fileStream = await fileService.GetReadFileStreamAsync("ct.obj");
        var loader = new Dummiesman.OBJLoader();
        var builder = loader.CustomLoad(fileStream);
        meshModelLoaderInfo = new MeshLoaderInfo(builder, loader);
        lock (locker)
        {
            CTRotation = serverService.GetCTRotation();
            CTPosition = serverService.GetCTPosition();
            CTObjLoadingIsReady = true;
        }
    }

    public void SaveCameraPosition()
    {
        var mesh = MeshCutter.GetSpatialMesh();
        var serverService = new ServerService();
        serverService.SendMesh(mesh, "room_mesh");
        CameraManager.StopFrameCollection = false;
        Task.Factory.StartNew(async () => {

#if !UNITY_EDITOR && UNITY_WSA

            await CameraManager.CollectFramesAsync();
            var result = "";
            var i = 0;
            foreach (var frame in CameraManager.Frames)
            {
                await serverService.SendFrame(frame, frame_n.ToString());
                var projectionMatrix = CameraManager.ProjectionMatrices[CameraManager.Frames.IndexOf(frame)];
                var c_position = CameraManager.GetCameraPosition(projectionMatrix);
                if (i == 0)
                {
                    firstCameraPosition = c_position;
                }
                i++;
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
            serverService.SendString(result, "externals");
            frame_n = 0;
#endif
            await InsertCT(serverService);
        });
        
    }


    public void StopPhotoCollection()
    {
        CameraManager.StopFrameCollection = true;
    }

}
