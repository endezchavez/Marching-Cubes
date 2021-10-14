using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
   public static float Perlin3D(float x, float y, float z)
    {
        // use unity's PerlinNoise 2D
        // youtube video time 0:25sec = https://www.youtube.com/watch?v=TZFv493D7jo&t=25s  
        // youtube video time 0:20sec = https://www.youtube.com/watch?v=Aga0TBJkchM

        float AB = Mathf.PerlinNoise(x, y);         // get all three(3) permutations of noise for x,y and z
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);

        float BA = Mathf.PerlinNoise(y, x);         // and their reverses
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        float ABC = AB + BC + AC + BA + CB + CA;    // and return the average
        return ABC / 6f;
    }
}
