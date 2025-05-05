using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways] //Always, since we might want to record the synthesis process in-game
public class ModelSynthesis : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Tileset tileset;
    [SerializeField] private int width;
    [SerializeField] private int length;
    [SerializeField] private int height;
    public int seed;
    
    [Header("Animation")]
    [SerializeField] private bool animate;
    [SerializeField] float timeToAnimate = 10f;
    [SerializeField] GameObject poof;

    private HashSet<Possibility>[,,] possibilities;
    private Transform parentTransform;

    private List<Tile> tiles => tileset.Tiles;
    private Tile border => tileset.Border;

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

        public Quaternion GetRotation()
        {
            return rotation switch
            {
                Rotation.zero => Quaternion.Euler(0, 0, 0),
                Rotation.ninety => Quaternion.Euler(0, -90, 0),
                Rotation.oneEighty => Quaternion.Euler(0, -180, 0),
                Rotation.twoSeventy => Quaternion.Euler(0, -270, 0),
            };
        }
        
        public override bool Equals(object obj)
        {
            if (obj is not Possibility other) return false;
            return tile == other.tile && rotation == other.rotation;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(tile, (int)rotation);
        }
    }
    
    /// <summary>
    /// Checks whether a coordinate is inside the grid area
    /// </summary>
    /// <returns></returns>
    public bool InGrid(int x, int y, int z) => x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < length;

    public void BeginSynthesis(int seed)
    {
        //If in editor, never animate
        if (animate && Application.isEditor && !Application.isPlaying)
        {
            animate = false;
            Debug.LogError("animate changed to FALSE; you are in editor mode.");
        }
        
        if(width <= 0 || height <= 0 || length <= 0)
        {
            Debug.LogError("Width, height and length must be greater than 0");
            return;
        }
        
        InitializePossibilities();

        Random.InitState(seed);
        Synthesise();
    }

    /// <summary>
    /// Synthesises the model by iterating through each tile in the grid and placing possible tiles to create a scene
    /// </summary>
    private void Synthesise()
    {
        parentTransform = new GameObject("Room").transform;
        
        MassPropagate();
        
        for (int y = 0; y < height; y++)
        {
            Transform layerTransform = new GameObject($"Layer{y}").transform;
            layerTransform.SetParent(parentTransform);
            for (int z = 0; z < length; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (possibilities[x, y, z].Count == 0)
                    {
                        Debug.LogError($"No possibilities left at ({x}, {y}, {z})");
                        return;
                    }

                    if (possibilities[x, y, z].First().placed)
                    {
                        continue;
                    }
                    
                    Possibility newTile = Observe(x, y, z);
                    if (newTile.tile.IsCustomSize)
                    {
                        PropagateMultitile(x, y, z, newTile);
                    }
                    else
                    {
                        Propagate(x, y, z);
                    }
                    PlaceTile(x, y, z, newTile,layerTransform);
                }
            }
        }
        if(animate) StartCoroutine(nameof(AnimatePlaceTiles));
    }
    
    /// <summary>
    /// Initial propagation over the entire grid
    /// </summary>
    private void MassPropagate()
    {
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                for(int z = 0; z < length; z++)
                {
                    Propagate(x, y, z, true);
                }
            }
        }
    }

    /// <summary>
    /// Randomly selects a tile from the possibilities at the given coordinates
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private Possibility Observe(int x, int y, int z)
    {
        if (!InGrid(x, y, z)) return null;
        Possibility observed = PossibilityBasedOnWeight(x, y, z);
        if (observed.tile.IsCustomSize) //If multitile, we need to observe the other grid point the tile fills
        {
            for (int i = 0; i < observed.tile.customSize.x; i++)
            {
                for (int j = 0; j < observed.tile.customSize.y; j++)
                {
                    for (int k = 0; k < observed.tile.customSize.z; k++)
                    {
                        var p = new Possibility(observed.tile, observed.rotation)
                        {
                            placed = true,
                            root = i == 0 && j == 0 && k == 0
                        };
                        possibilities[x + i, y + j, z + k].Clear();
                        possibilities[x + i, y + j, z + k].Add(p);
                    }
                }
            }
        }
        else
        {
            observed.placed = true;
            possibilities[x, y, z].Clear();
            possibilities[x, y, z].Add(observed);
        }
        return observed;
    }

    private Possibility PossibilityBasedOnWeight(int x, int y, int z)
    {
        var pos = possibilities[x, y, z].ToArray().Where(p => p.root).ToArray();
        float totalWeight = pos.Sum(p => p.tile.weight);
        float randomWeight = Random.Range(0, totalWeight);
        float runningWeight = 0;
        foreach (var p in pos)
        {
            runningWeight += p.tile.weight;
            if (randomWeight <= runningWeight)
            {
                return p;
            }
        }

        throw new Exception("Failed to pick a possibility");
    }

    private void PropagateMultitile(int x, int y, int z, Possibility multiTile)
    {
        var initialTiles = new List<(int x, int y, int z)>();
        // Above
        {
            int j = y + multiTile.tile.customSize.y;
            for (int i = x - 1; i < x + multiTile.tile.customSize.x; i++)
            {
                for (int k = z - 1; k < z + multiTile.tile.customSize.z; k++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // Below
        {
            int j = y - 1;
            for (int i = x - 1; i < x + multiTile.tile.customSize.x; i++)
            {
                for (int k = z - 1; k < z + multiTile.tile.customSize.z; k++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // North
        {
            int k = z + multiTile.tile.customSize.z;
            for (int i = x - 1; i < x + multiTile.tile.customSize.x; i++)
            {
                for (int j = z; j < y + multiTile.tile.customSize.y - 1; j++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // South
        {
            int k = z - 1;
            for (int i = x - 1; i < x + multiTile.tile.customSize.x; i++)
            {
                for (int j = z; j < y + multiTile.tile.customSize.y - 1; j++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // East
        {
            int i = x + multiTile.tile.customSize.x;
            for (int j = z; j < y + multiTile.tile.customSize.y - 1; j++)
            {
                for (int k = z; k < z + multiTile.tile.customSize.z - 1; k++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // West
        {
            int i = x - 1;
            for (int j = z; j < y + multiTile.tile.customSize.y - 1; j++)
            {
                for (int k = z; k < z + multiTile.tile.customSize.z - 1; k++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        Propagate(x, y, z, initialTiles);
    }

    private void Propagate(int x, int y, int z, bool propagateFromSelf = false)
    {
        var initialTiles = new List<(int x, int y, int z)>();
        if (!propagateFromSelf)
        {
            foreach (Direction d in DirectionExtensions.GetDirections()) //Add each neighbour to queue
            {
                (int dx, int dy, int dz) = d.ToOffset();
                int nx = x + dx;
                int ny = y + dy;
                int nz = z + dz;

                if (!InGrid(nx, ny, nz) || possibilities[nx, ny, nz].First().placed) continue;
                initialTiles.Add((nx, ny, nz));
            }
        }
        else
        {
            initialTiles.Add((x, y, z)); // Start with the initial tile
        }
        Propagate(x, y, z, initialTiles);
    }
    
    private void Propagate(int x, int y, int z, List<(int x, int y, int z)> initialTiles)
    {
        if (!InGrid(x, y, z)) return;
        
        Stack<(int x, int y, int z)> q = new Stack<(int x, int y, int z)>(initialTiles);

        int originalX = x;
        int originalY = y;
        int originalZ = z;
        
        while (q.Count > 0)
        {
            (x, y, z) = q.Pop();

            int countBefore = possibilities[x, y, z].Count; //Used to check if we need to propagate again

            foreach (Direction d in DirectionExtensions.GetDirections())
            {
                (int dx, int dy, int dz) = d.ToOffset();
                int nx = x + dx;
                int ny = y + dy;
                int nz = z + dz;

                if (!InGrid(nx, ny, nz)) //Handle border
                {
                    HashSet<Possibility> allowedByThis = new HashSet<Possibility>();
                    foreach (var possibility in possibilities[x, y, z])
                    {
                        if (possibility.tile.GetAllowed(d, possibility.rotation).Contains(border))
                        {
                            allowedByThis.Add(possibility);
                        }
                    }
                    possibilities[x, y, z].IntersectWith(allowedByThis);
                }
                else // Normal tile
                {
                    HashSet<Possibility> allowedByThis = new HashSet<Possibility>();
                    foreach (var possibility in possibilities[x, y, z]) //Check if the tile at (x,y,z) allows the tile at (nx,ny,nz)
                    {
                        if (possibility.tile.IsCustomSize && possibilities[nx, ny, nz].Contains(possibility) &&
                            possibilities[nx, ny, nz].Count != 1)
                        {
                            allowedByThis.Add(possibility);
                        }
                        if (possibilities[nx, ny, nz].Intersect(PossibilitiesFromTiles(possibility.tile.GetAllowed(d,possibility.rotation))).Any())
                        {
                            allowedByThis.Add(possibility);
                        }
                    }
                    possibilities[x, y, z].IntersectWith(allowedByThis);
                    
                    HashSet<Tile> allowedByNeighbour = new HashSet<Tile>();
                    foreach (var possibility in possibilities[nx, ny, nz]) //Check if the tile at (nx,ny,nz) allows the tile at (x,y,z)
                    {
                        if (possibility.tile.IsCustomSize && possibilities[nx, ny, nz].Count != 1)
                        {
                            allowedByNeighbour.Add(possibility.tile);
                        }
                        allowedByNeighbour.UnionWith(possibility.tile.GetAllowed(d.GetOpposite(),possibility.rotation));
                    }
                    HashSet<Possibility> allowedByNeighbourPossibilities = PossibilitiesFromTiles(allowedByNeighbour);
                    
                    if(d == Direction.BELOW && possibilities[nx,ny,nz].ElementAt(0).tile.sameRotationWhenStacked) //Enforce above tiles follow below rotation. Tiles below have always already been placed.
                    {
                        //We remove any possibility that doesn't have the same rotation as the tile below
                        for (int i = allowedByNeighbourPossibilities.Count - 1; i >= 0; i--)
                        {
                            Possibility p = allowedByNeighbourPossibilities.ElementAt(i);
                            if (p.tile.allowRotation && possibilities[nx, ny, nz].ElementAt(0).rotation != p.rotation)
                            {
                                allowedByNeighbourPossibilities.Remove(p);
                            }
                        }
                    }
                    
                    possibilities[x, y, z].IntersectWith(allowedByNeighbourPossibilities);
                }
            }
            
            //Multi-tile tiles
            HashSet<Possibility> toRemove = new HashSet<Possibility>();
            foreach (Possibility p in possibilities[x, y, z])
            {
                if (!p.tile.IsCustomSize) continue;
                
                bool failed = false;
                for (int i = 0; i < p.tile.customSize.x; i++)
                {
                    for (int j = 0; j < p.tile.customSize.y; j++)
                    {
                        for (int k = 0; k < p.tile.customSize.z; k++)
                        {
                            //If in grid and possibilities contains same tile as non-root
                            if(!InGrid(x+i, y+j, z+k) || !possibilities[x+i,y+j,z+k].Contains(p) ||
                               (possibilities[x + i, y + j, z + k].Count == 1 && possibilities[x+i,y+j,z+k].First().placed))
                            {
                                p.root = false;
                                failed = true;
                                break;
                            }
                        }
                        if (failed) break;
                    }
                    if (failed) break;
                }

                if (!p.root)
                {
                    bool foundRoot = false;
                    for (int i = 0; i < p.tile.customSize.x; i++)
                    {
                        for (int j = 0; j < p.tile.customSize.y; j++)
                        {
                            for (int k = 0; k < p.tile.customSize.z; k++)
                            {
                                if (InGrid(x-i,y-j,z-k) && possibilities[x - i, y - j, z - k].Any(p2 => p2.Equals(p) && p2.root))
                                {
                                    foundRoot = true;
                                    break;
                                }
                            }
                            if (foundRoot) break;
                        }
                        if (foundRoot) break;
                    }

                    if (!foundRoot)
                    {
                        toRemove.Add(p);
                    }
                }
            }
            possibilities[x, y, z].ExceptWith(toRemove);
            
            if (possibilities[x, y, z].Count == 0)
            {
                throw new Exception($"No possibilities left at ({x}, {y}, {z}). Originally propagating from ({originalX}, {originalY}, {originalZ}).");
            }
            
            // Check if any possibilities have been removed - if so propagate on neighbours
            if (countBefore > possibilities[x, y, z].Count)
            {
                foreach (Direction d in DirectionExtensions.GetDirections())
                {
                    (int dx, int dy, int dz) = d.ToOffset();
                    int nx = x + dx;
                    int ny = y + dy;
                    int nz = z + dz;

                    if (!InGrid(nx, ny, nz) || possibilities[nx, ny, nz].First().placed) continue;
                    
                    q.Push((nx, ny, nz));
                }
            }
        }
    }

    private HashSet<Possibility> PossibilitiesFromTiles(HashSet<Tile> tiles)
    {
        //Returns all posibliites with all rotations for relevant tiles
        HashSet<Possibility> _possibilities = new HashSet<Possibility>();
        foreach (Tile tile in tiles)
        {
            if (tile.allowRotation)
            {
                _possibilities.Add(new Possibility(tile, Rotation.zero));
                _possibilities.Add(new Possibility(tile, Rotation.ninety));
                _possibilities.Add(new Possibility(tile, Rotation.oneEighty));
                _possibilities.Add(new Possibility(tile, Rotation.twoSeventy));
            }
            else
            {
                _possibilities.Add(new Possibility(tile, Rotation.zero));
            }
        }
        return _possibilities;
    }


    private void InitializePossibilities()
    {
        possibilities = new HashSet<Possibility>[width, height, length];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < length; z++)
                {
                    HashSet<Possibility> allPossibleTiles = PossibilitiesFromTiles(new HashSet<Tile>(tiles));
                    possibilities[x, y, z] = new HashSet<Possibility>(allPossibleTiles);
                }
            }
        }
    }

    /// <summary>
    /// Places a tile (prefab) at the given coordinates.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="tile"></param>
    private void PlaceTile(int x, int y, int z, Possibility possibility, Transform parent)
    {
        if(possibility.tile.dontInstantiate) return; //If the tile is not supposed to be instantiated, we don't
        Vector3 placement = new Vector3(x, y, z) + new Vector3(possibility.tile.customSize.x-1,0, possibility.tile.customSize.z-1) / 2f;
        Tile newTile = Instantiate(possibility.tile, placement, possibility.GetRotation());
        newTile.transform.SetParent(parent);
        if(possibility.tile.allowFreeRotation) newTile.transform.eulerAngles = new Vector3(0, Random.Range(0,360), 0);
        if(animate) newTile.gameObject.SetActive(false);
    }
    
    private IEnumerator AnimatePlaceTiles()
    {
        float timePerChild = timeToAnimate / (height*length*width);
        foreach (Transform layer in parentTransform)
        {
            foreach (Transform child in layer)
            {
                child.gameObject.SetActive(true);
                Instantiate(poof, child.position, Quaternion.identity);
                yield return new WaitForSeconds(timePerChild);
            }
        }
    }
}
