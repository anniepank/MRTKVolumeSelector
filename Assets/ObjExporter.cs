using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

public class ObjExporter
{

    public static string MeshToString(Mesh m)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("g ").Append("Room").Append("\n");
        foreach (Vector3 v in m.vertices)
        {
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.normals)
        {
            sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        // for (int material = 0; material < m.subMeshCount; material++)
        // {
        sb.Append("\n");
        // sb.Append("usemtl ").Append(mats[material].name).Append("\n");
        // sb.Append("usemap ").Append(mats[material].name).Append("\n");

        int[] triangles = m.GetTriangles(0); // material
        for (int i = 0; i < triangles.Length; i += 3)
        {
            sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
        }
        // }
        return sb.ToString();
    }
}