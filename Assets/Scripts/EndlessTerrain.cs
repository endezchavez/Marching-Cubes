using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public GameObject chunkPrefab;
    public const float maxViewDst = 100;
    public Transform viewer;

    public static Vector2 viewerPos;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    

    private void Start()
    {
        chunkSize = 32;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
    }

    private void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }

        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    TerrainChunk chunk = terrainChunkDictionary[viewedChunkCoord];
                    chunk.UpdateTerrainChunk();
                    if (chunk.IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(chunk);
                    }
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize - 1, this.transform, chunkPrefab));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObj;
        Vector2 position;
        Bounds bounds;
        public TerrainChunk(Vector2 coord, int size, Transform parent, GameObject chunkPrefab)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            //meshObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObj = Instantiate(chunkPrefab);
            meshObj.transform.position = positionV3;
            meshObj.transform.localScale = Vector3.one;
            meshObj.transform.parent = parent;
            meshObj.GetComponent<MeshGenerator>().GenerateTerrain();
            SetVisible(false);
        }

        public void UpdateTerrainChunk()
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            bool visible = viewerDstFromNearestEdge <= maxViewDst;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObj.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObj.activeSelf;
        }
        
    }
}
