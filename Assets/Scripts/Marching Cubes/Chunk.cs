using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [HideInInspector]
    public Vector3Int coord;

    new MeshRenderer renderer;
    MeshFilter filter;
    new MeshCollider collider;
    [HideInInspector]
    public Mesh mesh;

    public void DestroyOrDisable()
    {
        if (Application.isPlaying)
        {
            mesh.Clear();
            gameObject.SetActive(false);
        }
        else
        {
            DestroyImmediate(gameObject, false);
        }
    }

    public void InitChunk(Material material, bool generateCollider)
    {
        renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<MeshRenderer>();
        }

        filter = GetComponent<MeshFilter>();
        if (filter == null)
        {
            filter = gameObject.AddComponent<MeshFilter>();
        }

        if (generateCollider)
        {
            collider = GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<MeshCollider>();
            }
        }

        if(collider != null && !generateCollider)
        {
            DestroyImmediate(collider);
        }
        

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.name = "Procedural Mesh";
        filter.sharedMesh = mesh;
        renderer.material = material;

        if (generateCollider)
        {
            collider.sharedMesh = mesh;
        }

    }

    public void UpdateCollider()
    {
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}
