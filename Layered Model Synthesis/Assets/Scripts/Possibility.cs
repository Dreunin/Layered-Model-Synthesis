using System;
using UnityEngine;

/// <summary>
/// A Possibility is a tile with a rotation that is viable to place at a specific coordinates in the grid. 
/// </summary>
public class Possibility 
{
    public Tile tile;
    public Rotation rotation;
    public bool root = true;
    public bool placed = false;

    public Possibility(Tile tile, Rotation rotation)
    {
        this.tile = tile;
        this.rotation = rotation;
    }

    public Quaternion GetQuaternion()
    {
        return rotation switch
        {
            Rotation.zero => Quaternion.Euler(0, 0, 0),
            Rotation.ninety => Quaternion.Euler(0, -90, 0),
            Rotation.oneEighty => Quaternion.Euler(0, -180, 0),
            Rotation.twoSeventy => Quaternion.Euler(0, -270, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
        };
    }
    
    public override bool Equals(object obj)
    {
        if (obj is not Possibility other) return false;
        return tile == other.tile && rotation == other.rotation;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(tile, (int) rotation);
    }
}
