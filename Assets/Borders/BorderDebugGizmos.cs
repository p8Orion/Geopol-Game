using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BorderDebugGizmos : MonoBehaviour
{
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;
        var mesh = meshFilter.sharedMesh;
        var verts = mesh.vertices;
        var tris = mesh.triangles;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < tris.Length; i += 3)
        {
            if (tris[i] < verts.Length && tris[i+1] < verts.Length && tris[i+2] < verts.Length)
            {
                Gizmos.DrawLine(transform.TransformPoint(verts[tris[i]]), transform.TransformPoint(verts[tris[i+1]]));
                Gizmos.DrawLine(transform.TransformPoint(verts[tris[i+1]]), transform.TransformPoint(verts[tris[i+2]]));
                Gizmos.DrawLine(transform.TransformPoint(verts[tris[i+2]]), transform.TransformPoint(verts[tris[i]]));
            }
        }
        Gizmos.color = Color.magenta;
        foreach (var v in verts)
        {
            Gizmos.DrawSphere(transform.TransformPoint(v), 100f); // tamaño gigante para que se vea
        }
    }
#endif
} 