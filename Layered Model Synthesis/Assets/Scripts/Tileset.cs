using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tileset", menuName = "Scriptable Objects/Tileset")]
public class Tileset : ScriptableObject
{
    [SerializeField] private List<Tile> tiles;
    [SerializeField] private Tile border;
    
    public List<Tile> Tiles { get => tiles; set => tiles = value; }
    public Tile Border { get => border; set => border = value; }
}