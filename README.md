# MRTKVolumeSelector
![Demo](https://github.com/anniepank/MRTKVolumeSelector/raw/master/Docs/demo.gif)
![Demo](https://github.com/anniepank/MRTKVolumeSelector/raw/master/Docs/demo_2.gif)
# Prerequisites
- Unity 2018.4.9.f1
- MixedrealityToolkit v2
- TextMesh Pro

# Getting Started
- Download latest release from [Releases page](/releases)
- Add MRTK unitypackage in you project from this [tutorial](https://microsoft.github.io/MixedRealityToolkit-Unity/Documentation/GettingStartedWithTheMRTK.html) 
- Add the `MRTKVolumeSelector/SelectionArea` prefab into the scene

After selection is done, you can check the position of the SelectionArea or you can use the supplied `MRTKVolumeSelector/MRTKVolumeSelectorMeshCutter` to cut the selected part of spatial awareness mesh:

```cs
var meshCutter = GetComponent<MRTKVolumeSelectorMeshCutter>();
Output.mesh = meshCutter.CutMeshToSelection(meshCutter.GetSpatialMesh(), SelectionArea);    
```

# Demo
You can check out the working demo scene by cloning this repository. 

# Author
Hanna Pankova ([@anniepank](https://github.com/anniepank))

# License
MIT
 
