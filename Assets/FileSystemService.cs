using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
#if !UNITY_EDITOR
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.Web.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel;
using System.IO;
using System.Text;
using Dummiesman;
#endif

public class FileSystemService
{
    private string tempFolderName = "H:\\App\\tmp\\";

#if !UNITY_EDITOR && UNITY_WSA
    private readonly StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
#endif

    public async Task SaveObjFileAsync(string filename, byte[] obj)
    {
#if !UNITY_EDITOR && UNITY_WSA

        var file = await _localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
        using (var destination = await file.OpenStreamForWriteAsync())
        {
            await destination.WriteAsync(obj, 0, obj.Length);
        }
#else   
        File.WriteAllBytes(tempFolderName + filename, obj);
#endif
    }

    public async Task<Stream> GetReadFileStreamAsync(string filename)
    {
#if !UNITY_EDITOR && UNITY_WSA
        var file = await _localFolder.GetFileAsync(filename);
        return await file.OpenStreamForReadAsync();
#else
        return File.OpenRead(tempFolderName + filename);
#endif
    }
}
