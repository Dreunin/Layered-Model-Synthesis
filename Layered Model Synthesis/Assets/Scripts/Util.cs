using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    public static IEnumerable<(int x, int y, int z)> Iterate3D(int width, int height, int length)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < length; z++)
                    yield return (x, y, z);
    }
    
    public static IEnumerable<(int x, int y, int z)> Iterate3D(Vector3Int dimensions) => Iterate3D(dimensions.x, dimensions.y, dimensions.z);
}