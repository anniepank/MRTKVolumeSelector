using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SwitchYZ : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        Switch(gameObject.GetComponent<MeshFilter>());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Switch(MeshFilter meshFilter)
    {
        var vertices = new List<Vector3>();
        meshFilter.mesh.GetVertices(vertices);
        // https://gamedev.stackexchange.com/questions/39906/why-does-unity-obj-import-flip-my-x-coordinate
        vertices = vertices.Select(v => new Vector3(v.x, v.z, v.y)).ToList();
        // vertices = vertices.Select(v => Quaternion.Euler(90, 0, 0) * (new Vector3(v.x, v.y, -v.z))).ToList();
        meshFilter.mesh.SetVertices(vertices);

        var triangles = meshFilter.mesh.triangles;

        for(var i = 0; i < triangles.Length; i+=3)
        {
            var tmp = triangles[i + 1];
            triangles[i + 1] = triangles[i + 2];
            triangles[i + 2] = tmp;
        }

        meshFilter.mesh.triangles = triangles;

        meshFilter.mesh.RecalculateNormals();
    }
}
