using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public enum VoxelSize{
        _1 = 1,
        _2 = 2,
        _4 = 4,
        _8 = 8,
        _16 = 16,
        _32 = 32,
        _64 = 64
    };


    const int threadGroupSize = 8;
    public bool showChunkBoundsGizmo;
    public bool showVoxelBoundsGizmo;
    public DensityGenerator densityGenerator;
    public Transform viewer;
    public float viewDistance;
    public bool autoUpdate = true;
    public bool generateColliders = true;
    public ComputeShader marchShader;
    public ComputeShader noiseTextureShader;
    public Material material;
    public Vector3 chunkSize = new Vector3(32, 32, 32);
    public VoxelSize voxelSize;

    public bool fixedMapSize;
    [ConditionalHide(nameof(fixedMapSize), true)]
    public Vector3Int numChunks = Vector3Int.one;

    public TerrainSettings settings;
    [HideInInspector]
    public bool terrainSettingsFoldout;

    //Chunks
    GameObject chunkHolder;
    const string chunkHolderName = "Chunks Holder";
    List<Chunk> chunks;
    Dictionary<Vector3Int, Chunk> existingChunks;
    Queue<Chunk> recycleableChunks;

    new MeshRenderer renderer;
    new MeshCollider collider;
    MeshFilter filter;
    Mesh mesh;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    float[,,] densityValues;

    //Compute Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer triCountBuffer;
    ComputeBuffer densityValuesBuffer;

    Vector3 chunkCentre;

    void Awake()
    {
        if(Application.isPlaying && !fixedMapSize)
        {
            InitVariableChunkStructures();

            var oldChunks = FindObjectsOfType<Chunk>();
            
            for (int i = oldChunks.Length - 1; i >=0; i--)
            {
                Destroy(oldChunks[i].gameObject);
            }
            

        }
    }

    private void Update()
    {
        if(Application.isPlaying && !fixedMapSize)
        {
            GenerateTerrain();
        }
    }

    public void GenerateTerrain()
    {
        CreateBuffers();

        if (fixedMapSize)
        {
            InitChunks();
            UpdateAllChunkMeshes();
        }
        else
        {
            if (Application.isPlaying)
            {
                InitVisibleChunks();
            }
        }

        if (!Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    void InitChunks()
    {
        CreateChunkHolder();
        chunks = new List<Chunk>();
        List<Chunk> oldChunks = new List<Chunk>(FindObjectsOfType<Chunk>());

        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    bool doesChunkAlreadyExist = false;

                    //If chunk already exists, add it to the chunks list and rmove it from the old chunks list
                    for (int i = 0; i < oldChunks.Count; i++)
                    {
                        if(oldChunks[i].coord == coord)
                        {
                            chunks.Add(oldChunks[i]);
                            oldChunks.RemoveAt(i);
                            doesChunkAlreadyExist = true;
                            break;
                        }
                    }

                    if (!doesChunkAlreadyExist)
                    {
                        Chunk newChunk = CreateChunk(coord);
                        chunks.Add(newChunk);
                    }

                    chunks[chunks.Count - 1].InitChunk(material, generateColliders);
                }
            }
        }

        // Delete all unused chunks
        for (int i = 0; i < oldChunks.Count; i++)
        {
            oldChunks[i].DestroyOrDisable();
        }
    }

    void InitVisibleChunks()
    {
        if (chunks == null)
        {
            return;
        }

        CreateChunkHolder();

        Vector3 viewerPos = viewer.position;
        Vector3Int viewrCoord = new Vector3Int(Mathf.RoundToInt(viewerPos.x / chunkSize.x), Mathf.RoundToInt(viewerPos.y / chunkSize.y), Mathf.RoundToInt(viewerPos.z / chunkSize.z));

        int maxChunksInView = Mathf.CeilToInt(viewDistance / chunkSize.x);
        float sqrViewDistance = viewDistance * viewDistance;

        // Go through all existing chunks and flag for recyling if outside of max view dst
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            Chunk chunk = chunks[i];
            Vector3 centre = CentreFromCoord(chunk.coord);
            Vector3 viewerOffset = viewerPos - centre;
            Vector3 o = new Vector3(Mathf.Abs(viewerOffset.x), Mathf.Abs(viewerOffset.y), Mathf.Abs(viewerOffset.z)) - Vector3.one * chunkSize.x / 2;
            float sqrDst = new Vector3(Mathf.Max(o.x, 0), Mathf.Max(o.y, 0), Mathf.Max(o.z, 0)).sqrMagnitude;
            if (sqrDst > sqrViewDistance)
            {
                existingChunks.Remove(chunk.coord);
                recycleableChunks.Enqueue(chunk);
                chunks.RemoveAt(i);
            }
        }

        for (int x = -maxChunksInView; x <= maxChunksInView; x++)
        {
            for (int y = -maxChunksInView; y <= maxChunksInView; y++)
            {
                for (int z = -maxChunksInView; z <= maxChunksInView; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z) + viewrCoord;

                    if (existingChunks.ContainsKey(coord))
                    {
                        continue;
                    }

                    Vector3 centre = CentreFromCoord(coord);
                    Vector3 viewerOffset = viewerPos - centre;
                    Vector3 o = new Vector3(Mathf.Abs(viewerOffset.x), Mathf.Abs(viewerOffset.y), Mathf.Abs(viewerOffset.z)) - Vector3.one * chunkSize.x / 2;
                    float sqrDst = new Vector3(Mathf.Max(o.x, 0), Mathf.Max(o.y, 0), Mathf.Max(o.z, 0)).sqrMagnitude;

                    // Chunk is within view distance and should be created (if it doesn't already exist)
                    if (sqrDst <= sqrViewDistance)
                    {

                        Bounds bounds = new Bounds(CentreFromCoord(coord), Vector3.one * chunkSize.x);
                        if (IsVisibleFrom(bounds, Camera.main))
                        {
                            if (recycleableChunks.Count > 0)
                            {
                                Chunk chunk = recycleableChunks.Dequeue();
                                chunk.coord = coord;
                                existingChunks.Add(coord, chunk);
                                chunks.Add(chunk);
                                UpdateChunkMesh(chunk);
                            }
                            else
                            {
                                Chunk chunk = CreateChunk(coord);
                                chunk.coord = coord;
                                chunk.InitChunk(material, generateColliders);
                                existingChunks.Add(coord, chunk);
                                chunks.Add(chunk);
                                UpdateChunkMesh(chunk);
                            }
                        }
                    }
                }
            }
        }
    }

    void InitVariableChunkStructures()
    {
        recycleableChunks = new Queue<Chunk>();
        chunks = new List<Chunk>();
        existingChunks = new Dictionary<Vector3Int, Chunk>();
    }

    public bool IsVisibleFrom(Bounds bounds, Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    void CreateChunkHolder()
    {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (chunkHolder == null)
        {
            if (GameObject.Find(chunkHolderName))
            {
                chunkHolder = GameObject.Find(chunkHolderName);
            }
            else
            {
                chunkHolder = new GameObject(chunkHolderName);
            }
        }
    }

    void UpdateAllChunkMeshes()
    {
        foreach(Chunk chunk in chunks)
        {
            UpdateChunkMesh(chunk);
        }
    }

   

    void UpdateChunkMesh(Chunk chunk)
    {
        Vector3Int coord = chunk.coord;
        chunkCentre  = CentreFromCoord(coord);
        float pointSpacing = ((float)voxelSize / chunkSize.x) * chunkSize.x;

        Vector3 offset = new Vector3(chunkSize.x / 2, chunkSize.x / 2, chunkSize.x / 2);

        int pointsPerXAxis = Mathf.CeilToInt(chunkSize.x / (int)voxelSize + 1);
        int pointsPerYAxis = Mathf.CeilToInt(chunkSize.y / (int)voxelSize + 1);
        int pointsPerZAxis = Mathf.CeilToInt(chunkSize.z / (int)voxelSize + 1);

        Vector3 pointsPerAxis = new Vector3(pointsPerXAxis, pointsPerYAxis, pointsPerZAxis);
        densityGenerator.GenerateDensityValues(densityValuesBuffer, pointsPerAxis, chunkCentre, pointSpacing, offset);

        int kernelIndex = marchShader.FindKernel("March");
        int numThreadsPerXAxis = Mathf.CeilToInt(pointsPerXAxis - 1 / (float)threadGroupSize);
        int numThreadsPerYAxis = Mathf.CeilToInt(pointsPerYAxis - 1 / (float)threadGroupSize);
        int numThreadsPerZAxis = Mathf.CeilToInt(pointsPerZAxis - 1 / (float)threadGroupSize);
        triangleBuffer.SetCounterValue(0);
        densityValuesBuffer.SetCounterValue(0);
        marchShader.SetBuffer(kernelIndex, "triangles", triangleBuffer);
        marchShader.SetBuffer(kernelIndex, "densityValues", densityValuesBuffer);
        marchShader.SetVector("numPointsPerAxis", new Vector3(pointsPerAxis.x, pointsPerAxis.y, pointsPerAxis.z));
        marchShader.SetFloat("isoLevel", settings.surfaceLevel);
        marchShader.SetBool("smoothTerrain", settings.smoothTerrain);

        marchShader.Dispatch(kernelIndex, numThreadsPerXAxis, numThreadsPerYAxis, numThreadsPerZAxis);

        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        float[] testResults = new float[(int)((chunkSize.x + 1) * (chunkSize.y) + 1 * (chunkSize.z + 1))];

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }

        Mesh mesh = chunk.mesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();

        chunk.UpdateCollider();


    }


    Chunk CreateChunk(Vector3Int coord)
    {
        GameObject chunk = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk>();
        newChunk.coord = coord;
        return newChunk;
    }

    void CreateBuffers()
    {
        //int densityPointsSize = sizeof(float) * densityValues.Length;
        int numPoints = ((int)chunkSize.x + 1) * ((int)chunkSize.y + 1) * ((int)chunkSize.z + 1);
        int maxTriangleCount = (int)chunkSize.x * (int)chunkSize.y * (int)chunkSize.z * 5;
        if (!Application.isPlaying || (densityValuesBuffer == null || numPoints != densityValuesBuffer.count))
        {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }
            triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            densityValuesBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        }

    }

    void ReleaseBuffers()
    {
        if(triangleBuffer != null)
        {
            triangleBuffer.Release();
            triCountBuffer.Release();
            densityValuesBuffer.Release();

        }
    }

    Vector3 CentreFromCoord(Vector3Int coord)
    {
        // Centre entire map at origin
        if (fixedMapSize)
        {
            Vector3 totalBounds = (Vector3)numChunks * chunkSize.x;
            return -totalBounds / 2 + (Vector3)coord * chunkSize.x + Vector3.one * chunkSize.x / 2;
        }

        return new Vector3(coord.x, coord.y, coord.z) * chunkSize.x;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    public void OnTerrainSettingsUpdated()
    {
        if (autoUpdate)
        {
            GenerateTerrain();

        }
    }

    void OnDrawGizmos()
    {
        if (showChunkBoundsGizmo)
        {
            Gizmos.color = Color.black;

            List<Chunk> chunks = (this.chunks == null) ? new List<Chunk>(FindObjectsOfType<Chunk>()) : this.chunks;
            foreach (var chunk in chunks)
            {
                Bounds bounds = new Bounds(CentreFromCoord(chunk.coord), Vector3.one * chunkSize.x);
                //Gizmos.color = boundsGizmoCol;
                Gizmos.DrawWireCube(CentreFromCoord(chunk.coord), Vector3.one * chunkSize.x);
            }

            Gizmos.color = Color.red;

            Gizmos.DrawCube(chunkCentre, Vector3.one);
        }

        if (showVoxelBoundsGizmo)
        {
            Gizmos.color = Color.blue;

            List<Chunk> chunks = (this.chunks == null) ? new List<Chunk>(FindObjectsOfType<Chunk>()) : this.chunks;
            foreach (var chunk in chunks)
            {
                //float3 pos = centre + id * spacing - offset;
                float pointSpacing = ((float)voxelSize / chunkSize.x) * chunkSize.x;

                Vector3 offset = new Vector3(chunkSize.x / 2 - (int)voxelSize/2, chunkSize.x / 2 - (int)voxelSize / 2, chunkSize.x / 2 - (int)voxelSize / 2);

                for (int z = 0; z < chunkSize.z / (int)voxelSize; z++)
                {
                    for (int y = 0; y < chunkSize.y / (int)voxelSize; y++)
                    {
                        for (int x = 0; x < chunkSize.z / (int)voxelSize; x++)
                        {
                            Vector3 pos = CentreFromCoord(chunk.coord) + new Vector3(x, y, z) * pointSpacing - offset;
                            Bounds bounds = new Bounds(pos, Vector3.one * (int)voxelSize);
                            //Gizmos.color = boundsGizmoCol;
                            Gizmos.DrawWireCube(pos, Vector3.one * (int)voxelSize);
                        }
                    }
                }

                
            }

            Gizmos.color = Color.red;

            Gizmos.DrawCube(chunkCentre, Vector3.one);
        }
    }



}
