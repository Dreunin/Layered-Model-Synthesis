using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    
    public PerformanceMeasurement totalPM = new PerformanceMeasurement("Total");
    public PerformanceMeasurement initializationPM = new PerformanceMeasurement("Initialization");
    public PerformanceMeasurement massPropagatePM = new PerformanceMeasurement("Mass Propagation");
    public PerformanceMeasurement propagatePM = new PerformanceMeasurement("Propagation");
    public PerformanceMeasurement propagate2PM = new PerformanceMeasurement("Propagation 2");
    public PerformanceMeasurement constrainPM = new PerformanceMeasurement("Constrain");
    public PerformanceMeasurement observePM = new PerformanceMeasurement("Observe");
    public PerformanceMeasurement propagateMultitilePM = new PerformanceMeasurement("Multi Tile Propagation");
    public PerformanceMeasurement multiTileFitPM = new PerformanceMeasurement("Multi Tile Fit");
    public PerformanceMeasurement multiTilePlacePM = new PerformanceMeasurement("Multi Tile Place");
    public PerformanceMeasurement rootInRangePM = new PerformanceMeasurement("Root In Range");
    public PerformanceMeasurement addToQueuePM = new PerformanceMeasurement("Add to Queue");
    public PerformanceMeasurement propagateNeighboursPM = new PerformanceMeasurement("Propagate Neighbours");
    public PerformanceMeasurement popPM = new PerformanceMeasurement("Pop");
    public PerformanceMeasurement stackPM = new PerformanceMeasurement("Stack Initialization");
    public int tilesPropagated = 0;

    public ModelSynthesis(Tileset tileset, int width, int length, int height, int seed)
    {
        this.tileset = tileset;
        this.width = width;
        this.length = length;
        this.height = height;
        
        random = new Random(seed);
        initializationPM.Start();
        InitializePossibilities();
        initializationPM.Stop();
    }
    
    /// <summary>
    /// Checks whether a coordinate is inside the grid area
    /// </summary>
    private bool InGrid(int x, int y, int z) => x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < length;
    
    /// <summary>
    /// Checks whether a tile is placed at the given coordinates
    /// </summary>
    private bool IsPlaced(int x, int y, int z) => possibilities[x, y, z].Count > 0 && possibilities[x, y, z].First().placed;

    /// <summary>
    /// Clears all other possibilities from the grid point and places the given tile at the given coordinates.
    /// </summary>
    public void PlaceTile(int x, int y, int z, Possibility possibility)
    {
        if (possibility.tile.IsCustomSize) //If multitile, we need to observe the other grid point the tile fills
        {
            Vector3Int size = possibility.tile.GetRotatedSize(possibility.rotation);
            foreach (var (i, j, k) in Util.Iterate3D(size))
            {
                var p = new Possibility(possibility.tile, possibility.rotation)
                {
                    placed = true,
                    root = i == 0 && j == 0 && k == 0
                };
                possibilities[x + i, y + j, z + k].Clear();
                possibilities[x + i, y + j, z + k].Add(p);
            }
        }
        else
        {
            possibility.placed = true;
            possibilities[x, y, z].Clear();
            possibilities[x, y, z].Add(possibility);
        }
    }

    /// <summary>
    /// Synthesises the model by iterating through each tile in the grid and placing possible tiles to create a scene
    /// </summary>
    public void Synthesise()
    {
        totalPM.Start();
        massPropagatePM.Start();
        MassPropagate();
        massPropagatePM.Stop();

        foreach (var (x, y, z) in Util.Iterate3D(width, height, length))
        {
            if (possibilities[x, y, z].Count == 0)
            {
                throw new Exception($"No possibilities left at ({x}, {y}, {z})");
            }

            if (IsPlaced(x, y, z)) continue;
                    
            observePM.Start();
            Possibility newTile = Observe(x, y, z);
            observePM.Stop();
            
            PropagateFromNeighbours(x, y, z, newTile);
            OnPlaceTile?.Invoke(new Vector3Int(x, y, z), newTile);
        }
        
        totalPM.Stop();
        OnFinish?.Invoke();
    }
    
    /// <summary>
    /// Initial propagation over the entire grid
    /// </summary>
    private void MassPropagate()
    {
        foreach (var (x, y, z) in Util.Iterate3D(width, height, length))
        {
            if (IsPlaced(x, y, z)) continue;
            PropagateFromSelf(x, y, z);
        }
    }

    /// <summary>
    /// Randomly selects a tile from the possibilities at the given coordinates
    /// </summary>
    private Possibility Observe(int x, int y, int z)
    {
        if (!InGrid(x, y, z)) return null;
        Possibility observed = PossibilityBasedOnWeight(x, y, z);
        PlaceTile(x, y, z, observed);
        return observed;
    }

    /// <summary>
    /// Randomly selects a possibility based on the weight of the available tiles at the given coordinates.
    /// Used to select a tile to place in the grid.
    /// The weight is used to determine the probability of selecting a tile. The higher the weight, the more likely it is to be selected.
    /// </summary>
    private Possibility PossibilityBasedOnWeight(int x, int y, int z)
    {
        var pos = possibilities[x, y, z].Where(p => p.root).ToArray();
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
    /// Runs propagate from the neighbours around tile (x, y, z).
    /// </summary>
    private void PropagateFromNeighbours(int x, int y, int z, Possibility possibility)
    {
        propagatePM.Start();
        propagateNeighboursPM.Start();
        var initialTiles = new List<(int x, int y, int z)>();
        foreach (Direction d in DirectionExtensions.GetDirections())
        {
            foreach (var (nx, ny, nz) in GetNeighbours(x, y, z, possibility, d))
            {
                if (!InGrid(nx, ny, nz) || IsPlaced(nx, ny, nz)) continue;
                initialTiles.Add((nx, ny, nz));
            }
        }
        propagateNeighboursPM.Stop();
        Propagate(x, y, z, initialTiles);
        propagatePM.Stop();
    }

    /// <summary>
    /// Runs propagate from tile (x, y, z).
    /// </summary>
    private void PropagateFromSelf(int x, int y, int z)
    {
        propagatePM.Start();
        Propagate(x, y, z, new List<(int, int, int)> { (x, y, z) });
        propagatePM.Stop();
    }
    
    /// <summary>
    /// Core propagate method
    /// </summary>
    /// <param name="initialTiles">The initial tiles to start propagating from.</param>
    /// <exception cref="Exception">if there are no possibilities left. Either due to a bug in code or an error in the tileset.</exception>
    private void Propagate(int x, int y, int z, List<(int x, int y, int z)> initialTiles)
    {
        if (!InGrid(x, y, z)) return;
        
        propagate2PM.Start();
        stackPM.Start();
        Stack<(int x, int y, int z)> q = new Stack<(int x, int y, int z)>(initialTiles);
        stackPM.Stop();
        
        int originalX = x;
        int originalY = y;
        int originalZ = z;
        
        while (q.Count > 0)
        {
            popPM.Start();
            (x, y, z) = q.Pop();
            popPM.Stop();
            
            tilesPropagated++;

            int countBefore = possibilities[x, y, z].Count; // Used to check if we need to propagate again

            constrainPM.Start();
            ConstrainByNeighbours(x, y, z);
            constrainPM.Stop();
            
            // Multi-tile tiles
            propagateMultitilePM.Start();
            foreach (var p in possibilities[x, y, z].Where(p => p.tile.IsCustomSize && p.root))
            {
                p.root = DoesMultiTileFit(x, y, z, p) && CanMultiTileBePlaced(x, y, z, p);
            }

            possibilities[x, y, z].RemoveWhere(p => p.tile.IsCustomSize && !p.root && !IsRootInRange(x, y, z, p));
            
            if (possibilities[x, y, z].Count == 0)
            {
                throw new Exception($"No possibilities left at ({x}, {y}, {z}). Originally propagating from ({originalX}, {originalY}, {originalZ}).");
            }
            propagateMultitilePM.Stop();
            
            // Check if any possibilities have been removed - if so propagate on neighbours
            addToQueuePM.Start();
            if (countBefore > possibilities[x, y, z].Count)
            {
                foreach (Direction d in DirectionExtensions.GetDirections())
                {
                    (int dx, int dy, int dz) = d.ToOffset();
                    int nx = x + dx;
                    int ny = y + dy;
                    int nz = z + dz;

                    if (!InGrid(nx, ny, nz) || IsPlaced(nx, ny, nz)) continue;
                    
                    q.Push((nx, ny, nz));
                }
            }
            addToQueuePM.Stop();
        }
        propagate2PM.Stop();
    }
    
    /// <summary>
    /// Core propagation check that refines the list of possibilities at (x, y, z) based on the possibilities in the neighbours.
    /// </summary>
    private void ConstrainByNeighbours(int x, int y, int z)
    {
        foreach (Direction d in DirectionExtensions.GetDirections())
        {
            (int dx, int dy, int dz) = d.ToOffset();
            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            if (!InGrid(nx, ny, nz)) // Handle border
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
                // Check if the tile at (x,y,z) allows the tile at (nx,ny,nz)
                HashSet<Possibility> allowedByThis = new HashSet<Possibility>();
                foreach (var possibility in possibilities[x, y, z]) 
                {
                    if (possibility.tile.IsCustomSize && possibilities[nx, ny, nz].Contains(possibility) &&
                        possibilities[nx, ny, nz].Count != 1)
                    {
                        allowedByThis.Add(possibility);
                    }
                    if (possibilities[nx, ny, nz].Select(p => p.tile).Intersect(possibility.tile.GetAllowed(d,possibility.rotation)).Any())
                    {
                        allowedByThis.Add(possibility);
                    }
                }
                possibilities[x, y, z].IntersectWith(allowedByThis);
                
                // Check if the tile at (nx,ny,nz) allows the tile at (x,y,z)
                HashSet<Tile> allowedByNeighbour = new HashSet<Tile>();
                foreach (var possibility in possibilities[nx, ny, nz]) 
                {
                    if (possibility.tile.IsCustomSize && possibilities[nx, ny, nz].Count != 1)
                    {
                        allowedByNeighbour.Add(possibility.tile);
                    }
                    allowedByNeighbour.UnionWith(possibility.tile.GetAllowed(d.GetOpposite(),possibility.rotation));
                }
                HashSet<Possibility> allowedByNeighbourPossibilities = PossibilitiesFromTiles(allowedByNeighbour);
                
                // Enforce above tiles follow below rotation.
                if (d == Direction.BELOW && IsPlaced(nx, ny, nz) && possibilities[nx, ny, nz].First().tile.sameRotationWhenStacked)
                {
                    allowedByNeighbourPossibilities.RemoveWhere(p =>
                        p.tile.allowRotation && possibilities[nx, ny, nz].First().rotation != p.rotation);
                }
                
                possibilities[x, y, z].IntersectWith(allowedByNeighbourPossibilities);
            }
        }
    }

    /// <summary>
    /// Checks if a multi-tile can fit if placed at the given coordinates.
    /// </summary>
    private bool DoesMultiTileFit(int x, int y, int z, Possibility possibility)
    {
        multiTileFitPM.Start();
        Vector3Int size = possibility.tile.GetRotatedSize(possibility.rotation);
        foreach (var (i, j, k) in Util.Iterate3D(size))
        {
            //If in grid and possibilities contains same tile as non-root
            if(!InGrid(x + i, y + j, z + k) || !possibilities[x + i, y + j, z + k].Contains(possibility) ||
               IsPlaced(x + i, y + j, z + k))
            {
                multiTileFitPM.Stop();
                return false;
            }
        }

        multiTileFitPM.Stop();
        return true;
    }
    
    /// <summary>
    /// Checks if a multi-tile can be placed at the given coordinates.
    /// </summary>
    private bool CanMultiTileBePlaced(int x, int y, int z, Possibility possibility)
    {
        multiTilePlacePM.Start();
        foreach (var direction in DirectionExtensions.GetDirections())
        {
            foreach (var (nx, ny, nz) in GetNeighbours(x, y, z, possibility, direction))
            {
                if (!InGrid(nx, ny, nz)) //Handle border
                {
                    if (!possibility.tile.GetAllowed(direction, possibility.rotation).Contains(border))
                    {
                        multiTilePlacePM.Stop();
                        return false;
                    }
                }
                else // Normal tile
                {
                    if (!possibility.tile.GetAllowed(direction, possibility.rotation)
                            .Any(t => possibilities[nx, ny, nz].Select(p => p.tile).Contains(t)))
                    {
                        multiTilePlacePM.Stop();
                        return false;
                    }

                    if (!possibilities[nx, ny, nz].Any(p =>
                            p.tile.GetAllowed(direction.GetOpposite(), p.rotation).Contains(possibility.tile)))
                    {
                        multiTilePlacePM.Stop();
                        return false;
                    }
                }
            }
        }

        multiTilePlacePM.Stop();
        return true;
    }

    /// <summary>
    /// Checks whether a root is in range of a given multi-tile possibility.
    /// </summary>
    private bool IsRootInRange(int x, int y, int z, Possibility possibility)
    {
        rootInRangePM.Start();
        Vector3Int size = possibility.tile.GetRotatedSize(possibility.rotation);
        foreach (var (i, j, k) in Util.Iterate3D(size))
        {
            if (InGrid(x - i, y - j, z - k) && possibilities[x - i, y - j, z - k].Any(p2 => p2.Equals(possibility) && p2.root))
            {
                rootInRangePM.Stop();
                return true;
            }
        }

        rootInRangePM.Stop();
        return false;
    }

    /// <summary>
    /// Returns all posibliites with all rotations for relevant tiles
    /// </summary>
    private HashSet<Possibility> PossibilitiesFromTiles(HashSet<Tile> tiles)
    {
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

    /// <summary>
    /// Initializes the possibilities array with all possible tiles for each grid point.
    /// </summary>
    private void InitializePossibilities()
    {
        possibilities = new HashSet<Possibility>[width, height, length];
        foreach (var (x, y, z) in Util.Iterate3D(width, height, length))
        {
            HashSet<Possibility> allPossibleTiles = PossibilitiesFromTiles(new HashSet<Tile>(tiles));
            possibilities[x, y, z] = new HashSet<Possibility>(allPossibleTiles);
        }
    }

    /// <summary>
    /// Returns coordinates of all neighbours in a given direction. 
    /// </summary>
    private List<(int x, int y, int z)> GetNeighbours(int x, int y, int z, Possibility source, Direction direction)
    {
        if (!source.tile.IsCustomSize)
        {
            (int dx, int dy, int dz) = direction.ToOffset();
            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            return new List<(int x, int y, int z)> { (nx, ny, nz) };
        }
        
        // Multi tile
        Vector3Int size = source.tile.GetRotatedSize(source.rotation);
        var neighbours = new List<(int x, int y, int z)>();
        
        if (direction == Direction.ABOVE) {
            int j = y + size.y;
            for (int i = x; i < x + size.x; i++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    neighbours.Add((i, j, k));
                }
            }
        }
        
        if (direction == Direction.BELOW) {
            int j = y - 1;
            for (int i = x; i < x + size.x; i++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    neighbours.Add((i, j, k));
                }
            }
        }
        
        if (direction == Direction.NORTH) {
            int k = z + size.z;
            for (int i = x; i < x + size.x; i++)
            {
                for (int j = y; j < y + size.y; j++)
                {
                    neighbours.Add((i, j, k));
                }
            }
        }
        
        if (direction == Direction.SOUTH) {
            int k = z - 1;
            for (int i = x; i < x + size.x; i++)
            {
                for (int j = y; j < y + size.y; j++)
                {
                    neighbours.Add((i, j, k));
                }
            }
        }
        
        if (direction == Direction.EAST) {
            int i = x + size.x;
            for (int j = y; j < y + size.y; j++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    neighbours.Add((i, j, k));
                }
            }
        }
        
        if (direction == Direction.WEST) {
            int i = x - 1;
            for (int j = y; j < y + size.y; j++)
            {
                for (int k = z; k < z + size.z; k++)
                {
                    neighbours.Add((i, j, k));
                }
            }
        }

        return neighbours;
    }
}
