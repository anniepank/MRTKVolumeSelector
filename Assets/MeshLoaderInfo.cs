using Dummiesman;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshLoaderInfo
{
    public Dictionary<string, OBJObjectBuilder> Builder;
    public OBJLoader Loader;

    public MeshLoaderInfo(Dictionary<string, OBJObjectBuilder> builder, OBJLoader loader)
    {
        this.Loader = loader;
        this.Builder = builder;
    }

    public MeshLoaderInfo()
    {

    }
}
