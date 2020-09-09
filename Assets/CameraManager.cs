using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloLensCameraStream;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Threading.Tasks;
#if !UNITY_EDITOR
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
#endif

public class CameraManager : MonoBehaviour
{
    public int RecordingDuration = 30;
    public Vector3 CameraPosition;
    private class SampleStruct
    {
        public float[] camera2WorldMatrix, projectionMatrix;
        public byte[] data;
    }
    private IntPtr _spatialCoordinateSystemPtr;

    private HoloLensCameraStream.Resolution _resolution;
    private VideoCapture _videoCapture;
    private byte[] _latestImageBytes;

    private int _width = 0, _height = 0;

    public List<byte[]> Frames = new List<byte[]>();
    public List<Matrix4x4> ProjectionMatrices = new List<Matrix4x4>();

    public bool StopFrameCollection = false;
    public Matrix4x4 ProjectionMatrix;
    public void StartRecording()
    {

    }
    // Start is called before the first frame update
    void Start()
    {
        //Fetch a pointer to Unity's spatial coordinate system if you need pixel mapping
        _spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
        CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);

    }

    private void OnVideoCaptureCreated(VideoCapture v)
    {
        if (v == null)
        {
            Debug.LogError("No VideoCapture found");
            return;
        }

        _videoCapture = v;

        //Request the spatial coordinate ptr if you want fetch the camera and set it if you need to 
        CameraStreamHelper.Instance.SetNativeISpatialCoordinateSystemPtr(_spatialCoordinateSystemPtr);

        _resolution = CameraStreamHelper.Instance.GetLowestResolution();
        _width = _resolution.width;
        _height = _resolution.height;
        float frameRate = CameraStreamHelper.Instance.GetHighestFrameRate(_resolution);

        _videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

        CameraParameters cameraParams = new CameraParameters();
        cameraParams.cameraResolutionHeight = _resolution.height;
        cameraParams.cameraResolutionWidth = _resolution.width;
        cameraParams.frameRate = Mathf.RoundToInt(frameRate);
        cameraParams.pixelFormat = CapturePixelFormat.BGRA32;
        cameraParams.rotateImage180Degrees = true; //If your image is upside down, remove this line.
        cameraParams.enableHolograms = false;

        // _pictureTexture = new Texture2D(_resolution.width, _resolution.height, TextureFormat.BGRA32, false);


        _videoCapture.StartVideoModeAsync(cameraParams, OnVideoModeStarted);
    }

    void OnVideoModeStarted(VideoCaptureResult result)
    {
        if (result.success == false)
        {
            Debug.LogWarning("Could not start video mode.");
            return;
        }

        Debug.Log("Video capture started.");
    }


    private void OnFrameSampleAcquired(VideoCaptureSample sample)
    {
        var newImageBytes = new byte[sample.dataLength];
        // Allocate byteBuffer
//        if (_latestImageBytes == null || _latestImageBytes.Length < sample.dataLength)
  //          _latestImageBytes = new byte[sample.dataLength];

        // Fill frame struct 
        SampleStruct s = new SampleStruct();
        sample.CopyRawImageDataIntoBuffer(newImageBytes);
        s.data = newImageBytes;

        _latestImageBytes = newImageBytes;

        // Get the cameraToWorldMatrix and projectionMatrix
        if (!sample.TryGetCameraToWorldMatrix(out s.camera2WorldMatrix) || !sample.TryGetProjectionMatrix(out s.projectionMatrix))
            return;


        ProjectionMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(s.camera2WorldMatrix);
        sample.Dispose();

    }
    public Quaternion ExtractRotation(Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public Matrix4x4 ExtractRotationMatrix(Quaternion quaternion)
    {
        return Matrix4x4.Rotate(quaternion);
    }

    public Vector3 ExtractPosition(Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }

    public Vector3 GetCameraPosition()
    {
        return ExtractPosition(ProjectionMatrix);
    }
    public Vector3 GetCameraPosition(Matrix4x4 matrix)
    {
        return ExtractPosition(matrix);
    }

    public Matrix4x4 GetCameraRotation()
    {
        return ExtractRotationMatrix(ExtractRotation(ProjectionMatrix));
    }

    public Matrix4x4 GetCameraRotation(Matrix4x4 matrix)
    {
        return ExtractRotationMatrix(ExtractRotation(matrix));
    }

    public Quaternion GetCameraRotationQuaternion()
    {
        return ExtractRotation(ProjectionMatrix);
    }

    public Quaternion GetCameraRotationQuaternion(Matrix4x4 matrix)
    {
        return ExtractRotation(matrix);
    }


#if !UNITY_EDITOR
    public async Task<byte[]> ConvertBitmapToPng(byte[] pixels, int width, int height) 
    {
        var stream = new InMemoryRandomAccessStream();
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    (uint)width, (uint)height, 96.0, 96.0, pixels);
        await encoder.FlushAsync();


        var reader = new DataReader(stream.GetInputStreamAt(0));
        var bytes = new byte[stream.Size];
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(bytes);
        return bytes;
    }

    public async Task<WriteableBitmap> GetImageBitmap()
    {
        var writebaleBitmap = new WriteableBitmap(_width, _height);
        using (Stream stream = writebaleBitmap.PixelBuffer.AsStream())
        {
            await stream.WriteAsync(_latestImageBytes, 0, _latestImageBytes.Length);
        }

        return writebaleBitmap;
    }
#endif    

    public async Task<byte[]> GetImage()
    {
#if !UNITY_EDITOR
        return await ConvertBitmapToPng(_latestImageBytes, _width, _height);
#else
        return null;
#endif
    }

    public async Task CollectFramesAsync()
    {
        Frames = new List<byte[]>();
        while (true && !StopFrameCollection)
        {
            var delayTask = Task.Delay(800);
            var frame = await GetImage();
            Frames.Add(frame);
            ProjectionMatrices.Add(ProjectionMatrix);
            await delayTask; // wait until at least 10s elapsed since delayTask created
        }
    }
}
