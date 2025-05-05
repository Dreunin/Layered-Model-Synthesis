using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Tileset is a collection of Tiles.
/// Each Tile in the tileset can be placed using the Synthesiser.
/// Any Tile can be added to any Tileset, but to be a part of multiple Tilesets, a tile should then have multiple Tile components attached (unless one Tileset is a subset of another). 
/// </summary>

[CreateAssetMenu(fileName = "Tileset", menuName = "Scriptable Objects/Tileset")]
public class Tileset : ScriptableObject
{
    [SerializeField] private List<Tile> tiles;
    [SerializeField] private Tile border;
    
    public List<Tile> Tiles { get => tiles; set => tiles = value; }
    public Tile Border { get => border; set => border = value; }
}