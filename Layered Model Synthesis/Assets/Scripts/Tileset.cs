using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Tileset", menuName = "Scriptable Objects/Tileset")]
public class Tileset : ScriptableObject
{
    [SerializeField] private List<Tile> tiles;
    [SerializeField] private Tile border;
    
    public List<Tile> Tiles { get => tiles; set => tiles = value; }
    public Tile Border { get => border; set => border = value; }

    private void OnValidate()
    {
        foreach (Tile tile in tiles)
        {
            if (tile.tileset != this)
            {
                Debug.LogWarning($"Tileset {name} is set to contain {tile.name}, but the tile is not set to be in this tileset");
            }
        }
    }
}
