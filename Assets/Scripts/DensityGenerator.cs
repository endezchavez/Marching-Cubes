using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityGenerator : MonoBehaviour
{
    const int threadGroupSize = 8;

    public MeshGenerator meshGenerator;
    public NoiseSettings settings;
    public ComputeShader densityShader;

    public bool autoUpdate;

    [HideInInspector]
    public bool noiseSettingsFoldout;

    public ComputeBuffer GenerateDensityValues(ComputeBuffer densityBuffer, Vector3 numPointsPerAxis, Vector3 centre, float spacing, Vector3 offset)
    {
        
        int numThreadsPerXAxis = Mathf.CeilToInt(numPointsPerAxis.x / (float)threadGroupSize);
        int numThreadsPerYAxis = Mathf.CeilToInt(numPointsPerAxis.y / (float)threadGroupSize);
        int numThreadsPerZAxis = Mathf.CeilToInt(numPointsPerAxis.z / (float)threadGroupSize);

        // Noise parameters
        var prng = new System.Random(settings.seed);
        var offsets = new Vector3[settings.numOctaves];
        float offsetRange = 1000;
        for (int i = 0; i < settings.numOctaves; i++)
        {
            offsets[i] = new Vector3((float)prng.NextDouble() * 2 - 1, (float)prng.NextDouble() * 2 - 1, (float)prng.NextDouble() * 2 - 1) * offsetRange;
        }

        var offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
        offsetsBuffer.SetData(offsets);

        densityShader.SetBuffer(densityShader.FindKernel("CSMain"), "densityValues", densityBuffer);
        densityShader.SetVector("numPointsPerAxis", new Vector3(numPointsPerAxis.x, numPointsPerAxis.y, numPointsPerAxis.z));
        densityShader.SetVector("centre", new Vector3(centre.x, centre.y, centre.z));
        densityShader.SetFloat("spacing", spacing);
        densityShader.SetVector("offset", new Vector3(offset.x, offset.y, offset.z));
        densityShader.SetBuffer(0, "offsets", offsetsBuffer);
        densityShader.SetInt("octaves", Mathf.Max(1, settings.numOctaves));
        densityShader.SetFloat("lacunarity", settings.lacunarity);
        densityShader.SetFloat("persistence", settings.persistence);
        densityShader.SetFloat("noiseScale", settings.noiseScale);
        densityShader.SetFloat("worldHeightLimit", settings.worldHeightLimit);
        densityShader.SetVector("noiseOffset", new Vector3(settings.offset.x, settings.offset.y, settings.offset.z));

        densityShader.Dispatch(densityShader.FindKernel("CSMain"), numThreadsPerXAxis, numThreadsPerYAxis, numThreadsPerZAxis);

        offsetsBuffer.Release();

        return densityBuffer;
    }

    public void OnNoiseSettingsUpdated()
    {
        if (autoUpdate)
        {
            UpdateTerrain();
        }
    }

    public void UpdateTerrain()
    {
        meshGenerator.GenerateTerrain();
    }


}
