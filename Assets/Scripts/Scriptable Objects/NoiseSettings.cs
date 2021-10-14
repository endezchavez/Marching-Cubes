using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseSettings : ScriptableObject
{
    public int seed;
    public Vector3 offset;
    [Range(1, 8)]
    public int numOctaves = 4;
    public float lacunarity = 2;
    public float persistence = .5f;
    public float noiseScale = 1;
    public float worldHeightLimit = 10f;
}
