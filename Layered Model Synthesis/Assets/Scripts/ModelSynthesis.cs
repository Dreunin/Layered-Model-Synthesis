using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class ModelSynthesis
{
    private Tileset tileset;
    private int width;
    private int length;
    private int height;

    private HashSet<Possibility>[,,] possibilities;

    private List<Tile> tiles => tileset.Tiles;
    private Tile border => tileset.Border;

    private Random random;

    public event Action<Vector3Int, Possibility> OnPlaceTile;
    public event Action OnFinish;

    public ModelSynthesis(Tileset tileset, int width, int length, int height, int seed)
    {
        this.tileset = tileset;
        this.width = width;
        this.length = length;
        this.height = height;
        
        random = new Random(seed);
        InitializePossibilities();
    }
    
    /// <summary>
    /// Checks whether a coordinate is inside the grid area
    /// </summary>
    /// <returns></returns>
    bool InGrid(int x, int y, int z) => x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < length;

    /// <summary>
    /// Synthesises the model by iterating through each tile in the grid and placing possible tiles to create a scene
    /// </summary>
    public void Synthesise()
    {
        MassPropagate();
        
        for (int y = 0; y < height; y++)
        {
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
                    
                    OnPlaceTile?.Invoke(new Vector3Int(x, y, z), newTile);
                }
            }
        }
        
        OnFinish?.Invoke();
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
            Vector3Int vector = observed.tile.GetRotatedSize(observed.rotation);
            for (int i = 0; i < vector.x; i++)
            {
                for (int j = 0; j < vector.y; j++)
                {
                    for (int k = 0; k < vector.z; k++)
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

    /// <summary>
    /// Randomly selects a possibility based on the weight of the available tiles at the given coordinates.
    /// Used to select a tile to place in the grid.
    /// The weight is used to determine the probability of selecting a tile. The higher the weight, the more likely it is to be selected.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private Possibility PossibilityBasedOnWeight(int x, int y, int z)
    {
        var pos = possibilities[x, y, z].ToArray().Where(p => p.root).ToArray();
        float totalWeight = pos.Sum(p => p.tile.weight);
        double randomWeight = random.NextDouble() * totalWeight;
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

    /// <summary>
    /// Propagates around multi-tile tiles. Ensures proper updates to the grid after multi-tile placement. 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="multiTile"></param>
    private void PropagateMultitile(int x, int y, int z, Possibility multiTile)
    {
        Vector3Int size = multiTile.tile.GetRotatedSize(multiTile.rotation);
        var initialTiles = new List<(int x, int y, int z)>();
        // Above
        {
            int j = y + size.y;
            for (int i = x; i < x + size.x; i++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // Below
        {
            int j = y - 1;
            for (int i = x; i < x + size.x; i++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // North
        {
            int k = z + size.z;
            for (int i = x; i < x + size.x; i++)
            {
                for (int j = y; j < y + size.y; j++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // South
        {
            int k = z - 1;
            for (int i = x; i < x + size.x; i++)
            {
                for (int j = y; j < y + size.y; j++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // East
        {
            int i = x + size.x;
            for (int j = y; j < y + size.y; j++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        // West
        {
            int i = x - 1;
            for (int j = y; j < y + size.y; j++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    if (!InGrid(i, j, k) || possibilities[i, j, k].First().placed) continue;
                    initialTiles.Add((i, j, k));
                }
            }
        }
        
        Propagate(x, y, z, initialTiles);
    }

    /// <summary>
    /// Overloaded version of Propagate that determines which initial tiles to propagate from based on the propgateFromSelf parameter.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="propagateFromSelf">If false: propagate from each neighbour tile - if true: propagate from (x, y, z)</param>
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
    
    /// <summary>
    /// Core propagate method
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="initialTiles">The initial tiles to start propagating from.</param>
    /// <exception cref="Exception">if there are no possibilities left. Either due to a bug in code or an error in the tileset.</exception>
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
                    
                    if(d == Direction.BELOW && possibilities[nx,ny,nz].First().placed && possibilities[nx,ny,nz].First().tile.sameRotationWhenStacked) //Enforce above tiles follow below rotation. Tiles below have always already been placed.
                    {
                        //We remove any possibility that doesn't have the same rotation as the tile below
                        for (int i = allowedByNeighbourPossibilities.Count - 1; i >= 0; i--)
                        {
                            Possibility p = allowedByNeighbourPossibilities.ElementAt(i);
                            if (p.tile.allowRotation && possibilities[nx, ny, nz].First().rotation != p.rotation)
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
                Vector3Int size = p.tile.GetRotatedSize(p.rotation);
                
                if (p.root)
                {
                    bool failed = false;
                    for (int i = 0; i < size.x; i++)
                    {
                        for (int j = 0; j < size.y; j++)
                        {
                            for (int k = 0; k < size.z; k++)
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

                    if (!failed)
                    {
                        p.root = CanMultiTileBePlaced(x, y, z, p);
                    }
                }
                
                if (!p.root)
                {
                    bool foundRoot = false;
                    for (int i = 0; i < size.x; i++)
                    {
                        for (int j = 0; j < size.y; j++)
                        {
                            for (int k = 0; k < size.z; k++)
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
                throw new Exception($"No possibilities left at ({x}, {y}, {z}). Originally propagating from ({originalX}, {originalY}, {originalZ}). Tried to place {possibilities[originalX, originalY, originalZ].First().tile.name}.");
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
    
    /// <summary>
    /// Checks if a multi-tile can be placed at the given coordinates.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="multiTile"></param>
    /// <returns></returns>
    private bool CanMultiTileBePlaced(int x, int y, int z, Possibility multiTile)
    {
        bool CheckDirection(int i, int j, int k, Direction d)
        {
            if (!InGrid(i, j, k)) //Handle border
            {
                if (!multiTile.tile.GetAllowed(d, multiTile.rotation).Contains(border))
                {
                    return false;
                }
            }
            else // Normal tile
            {
                if (!multiTile.tile.GetAllowed(d, multiTile.rotation)
                        .Any(t => possibilities[i, j, k].Select(p => p.tile).Contains(t)))
                {
                    return false;
                }

                if (!possibilities[i, j, k].Any(p =>
                        p.tile.GetAllowed(d.GetOpposite(), p.rotation).Contains(multiTile.tile)))
                {
                    return false;
                }
            }

            return true;
        }
        
        Vector3Int size = multiTile.tile.GetRotatedSize(multiTile.rotation);
        // Above
        {
            int j = y + size.y;
            for (int i = x; i < x + size.x; i++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    if (!CheckDirection(i, j, k, Direction.ABOVE)) return false;
                }
            }
        }
        
        // Below
        {
            int j = y - 1;
            for (int i = x; i < x + size.x; i++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    if (!CheckDirection(i, j, k, Direction.BELOW)) return false;

                    if (!InGrid(i, j, k)) continue;
                    Possibility below = possibilities[i, j, k].First();
                    if (below.placed && below.tile.sameRotationWhenStacked && below.rotation != multiTile.rotation)
                    {
                        return false;
                    }
                }
            }
        }
        
        // North
        {
            int k = z + size.z;
            for (int i = x; i < x + size.x; i++)
            {
                for (int j = y; j < y + size.y; j++)
                {
                    if (!CheckDirection(i, j, k, Direction.NORTH)) return false;
                }
            }
        }
        
        // South
        {
            int k = z - 1;
            for (int i = x; i < x + size.x; i++)
            {
                for (int j = y; j < y + size.y; j++)
                {
                    if (!CheckDirection(i, j, k, Direction.SOUTH)) return false;
                }
            }
        }
        
        // East
        {
            int i = x + size.x;
            for (int j = y; j < y + size.y; j++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    if (!CheckDirection(i, j, k, Direction.EAST)) return false;
                }
            }
        }
        
        // West
        {
            int i = x - 1;
            for (int j = y; j < y + size.y; j++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    if (!CheckDirection(i, j, k, Direction.WEST)) return false;
                }
            }
        }

        return true;
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
}
