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
    public PerformanceMeasurement observePM = new PerformanceMeasurement("Observe");
    public PerformanceMeasurement massPropagatePM = new PerformanceMeasurement("Mass Propagation");
    public PerformanceMeasurement propagatePM = new PerformanceMeasurement("Propagation");
    public PerformanceMeasurement constrainPM = new PerformanceMeasurement("Constrain");
    public PerformanceMeasurement propagateMultitilePM = new PerformanceMeasurement("Multi Tile Propagation");
    public PerformanceMeasurement multiTileFitPM = new PerformanceMeasurement("Multi Tile Fit");
    public PerformanceMeasurement multiTilePlacePM = new PerformanceMeasurement("Multi Tile Place");
    public PerformanceMeasurement rootInRangePM = new PerformanceMeasurement("Root In Range");
    public int tilesPropagated = 0;

    public ModelSynthesis(Tileset tileset, int width, int length, int height, int seed)
    {
        this.tileset = tileset;
        this.width = width;
        this.length = length;
        this.height = height;
        
        random = new Random(seed);
        initializationPM.MeasureFunction(InitializePossibilities);;
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
        massPropagatePM.MeasureFunction(MassPropagate);

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
        var initialTiles = new List<(int x, int y, int z, Direction)>();
        foreach (Direction d in DirectionExtensions.GetDirections())
        {
            foreach (var (nx, ny, nz) in GetNeighbours(x, y, z, possibility, d))
            {
                if (!InGrid(nx, ny, nz) || IsPlaced(nx, ny, nz)) continue;
                initialTiles.Add((nx, ny, nz, d));
            }
        }

        propagatePM.MeasureFunction(() => Propagate(initialTiles));
    }

    /// <summary>
    /// Runs propagate from tile (x, y, z).
    /// </summary>
    private void PropagateFromSelf(int x, int y, int z)
    {
        var initialTiles = new List<(int x, int y, int z, Direction)>();
        foreach (Direction d in DirectionExtensions.GetDirections())
        {
            initialTiles.Add((x, y, z, d));
        }

        propagatePM.MeasureFunction(() => Propagate(initialTiles));
    }
    
    /// <summary>
    /// Core propagate method
    /// </summary>
    /// <param name="initialTiles">The initial tiles to start propagating from.</param>
    /// <exception cref="Exception">if there are no possibilities left. Either due to a bug in code or an error in the tileset.</exception>
    private void Propagate(List<(int x, int y, int z, Direction source)> initialTiles)
    {
        var q = new Stack<(int x, int y, int z, Direction source)>(initialTiles);
        //UniqueStack<(int x, int y, int z)> q = new UniqueStack<(int x, int y, int z)>(initialTiles); // UniqueStack optimization attempt
        
        while (q.Count > 0)
        {
            var (x, y, z, dir) = q.Pop();
            tilesPropagated++;

            int countBefore = possibilities[x, y, z].Count; // Used to check if we need to propagate again

            constrainPM.MeasureFunction(() => ConstrainByNeighbours(x, y, z, dir));
            
            // Multi-tile tiles
            propagateMultitilePM.Start();
            foreach (var p in possibilities[x, y, z].Where(p => p.tile.IsCustomSize && p.root))
            {
                p.root = multiTileFitPM.MeasureFunction(() => DoesMultiTileFit(x, y, z, p)) &&
                         multiTilePlacePM.MeasureFunction(() => CanMultiTileBePlaced(x, y, z, p));
            }

            possibilities[x, y, z].RemoveWhere(p => p.tile.IsCustomSize && !p.root && rootInRangePM.MeasureFunction(() => !IsRootInRange(x, y, z, p)));
            propagateMultitilePM.Stop();
            
            if (possibilities[x, y, z].Count == 0)
            {
                throw new Exception($"No possibilities left at ({x}, {y}, {z}).");
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

                    if (!InGrid(nx, ny, nz) || IsPlaced(nx, ny, nz)) continue;
                    //if (!InGrid(nx, ny, nz) || IsPlaced(nx, ny, nz) || q.Contains((nx, ny, nz))) continue; // Queue.Contains optimization attempt
                    
                    q.Push((nx, ny, nz, d));
                }
            }
        }
    }
    
    /// <summary>
    /// Core propagation check that refines the list of possibilities at (x, y, z) based on the possibilities in the neighbours.
    /// </summary>
    private void ConstrainByNeighbours(int x, int y, int z, Direction source)
    {
        Direction d = source.GetOpposite();
        
        (int dx, int dy, int dz) = d.ToOffset();
        int nx = x + dx;
        int ny = y + dy;
        int nz = z + dz;

        var current = possibilities[x, y, z];

        if (!InGrid(nx, ny, nz)) // Handle border
        {
            current.RemoveWhere(p => !p.tile.GetAllowed(d, p.rotation).Contains(border));
        }
        else // Normal tile
        {
            var neighbour = possibilities[nx, ny, nz];
            
            var allowedByNeighbour = new HashSet<Tile>();
            foreach (var p in neighbour)
            {
                foreach (var allowed in p.tile.GetAllowed(d.GetOpposite(), p.rotation))
                {
                    allowedByNeighbour.Add(allowed);
                }
            }
            
            var neighbourTiles = neighbour.Select(n => n.tile).ToHashSet();
            
            current.RemoveWhere(p =>
            {
                if (p.tile.IsCustomSize && neighbour.Contains(p) && neighbour.Count != 1) 
                    return false; // Keep because it might be part of the same multitile

                if (!neighbourTiles.Overlaps(p.tile.GetAllowed(d, p.rotation)))
                    return true; // Remove because (x, y, z) does not allow any of the possibilities at (nx, ny, nz)
                
                return !allowedByNeighbour.Contains(p.tile); // Remove if (nx, ny, nz) does not allow this possibility
            });
            
            // Enforce above tiles follow below rotation.
            if (d == Direction.BELOW && IsPlaced(nx, ny, nz) && neighbour.First().tile.sameRotationWhenStacked)
            {
                current.RemoveWhere(p =>
                    p.tile.allowRotation && neighbour.First().rotation != p.rotation);
            }
        }
    }

    /// <summary>
    /// Checks if a multi-tile can fit if placed at the given coordinates.
    /// </summary>
    private bool DoesMultiTileFit(int x, int y, int z, Possibility possibility)
    {
        Vector3Int size = possibility.tile.GetRotatedSize(possibility.rotation);
        foreach (var (i, j, k) in Util.Iterate3D(size))
        {
            //If in grid and possibilities contains same tile as non-root
            if(!InGrid(x + i, y + j, z + k) || !possibilities[x + i, y + j, z + k].Contains(possibility) ||
               IsPlaced(x + i, y + j, z + k))
            {
                return false;
            }
        }

        return true;
    }
    
    /// <summary>
    /// Checks if a multi-tile can be placed at the given coordinates.
    /// </summary>
    private bool CanMultiTileBePlaced(int x, int y, int z, Possibility possibility)
    {
        foreach (var direction in DirectionExtensions.GetDirections())
        {
            foreach (var (nx, ny, nz) in GetNeighbours(x, y, z, possibility, direction))
            {
                if (!InGrid(nx, ny, nz)) //Handle border
                {
                    if (!possibility.tile.GetAllowed(direction, possibility.rotation).Contains(border))
                    {
                        return false;
                    }
                }
                else // Normal tile
                {
                    if (!possibility.tile.GetAllowed(direction, possibility.rotation).Overlaps(possibilities[nx, ny, nz].Select(p => p.tile)))
                    {
                        return false;
                    }

                    if (!possibilities[nx, ny, nz].Any(p =>
                            p.tile.GetAllowed(direction.GetOpposite(), p.rotation).Contains(possibility.tile)))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether a root is in range of a given multi-tile possibility.
    /// </summary>
    private bool IsRootInRange(int x, int y, int z, Possibility possibility)
    {
        Vector3Int size = possibility.tile.GetRotatedSize(possibility.rotation);
        foreach (var (i, j, k) in Util.Iterate3D(size))
        {
            if (InGrid(x - i, y - j, z - k) && possibilities[x - i, y - j, z - k].Any(p2 => p2.Equals(possibility) && p2.root))
            {
                return true;
            }
        }

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
