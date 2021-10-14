using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainSettings : ScriptableObject
{
    public bool smoothTerrain = true;

    [Range(0, 1)]
    public float surfaceLevel;
}
