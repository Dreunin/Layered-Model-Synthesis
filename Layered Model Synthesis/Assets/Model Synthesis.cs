using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[ExecuteAlways] //Always, since we might want to record the synthesis process in-game
public class ModelSynthesis : MonoBehaviour
{
    [SerializeField] private List<Tile> tiles;
    [SerializeField] bool synthesise;
    [SerializeField] private int width;
    [SerializeField] private int length;
    [SerializeField] private int height;
    [SerializeField] private bool animate;
    [SerializeField] float delayBetweenTilePlacement = 0.1f;
    [SerializeField] GameObject poof;
    [SerializeField] private Tile border;

    private HashSet<Possibility>[,,] possibilities;
    private Transform parentTransform;

    public class Possibility
    {
        public Tile tile;
        public Rotation rotation;

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

    void Update()
    {
        if (synthesise)
        {
            synthesise = false;
            BeginSynthesis();
        }
    }
    
    /// <summary>
    /// Checks whether a coordinate is inside the grid area
    /// </summary>
    /// <returns></returns>
    public bool InGrid(int x, int y, int z) => x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < length;

    private void BeginSynthesis()
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
                    
                    Possibility newTile = Observe(x, y, z);
                    Propagate(x, y, z);
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
        Possibility observed = possibilities[x, y, z].ElementAt(Random.Range(0, possibilities[x, y, z].Count));
        possibilities[x, y, z].Clear();
        possibilities[x, y, z].Add(observed);
        return observed;
    }


    private void Propagate(int x, int y, int z, bool propagateFromSelf = false)
    {
        if (!InGrid(x, y, z)) return;
        
        Stack<(int x, int y, int z)> q = new Stack<(int x, int y, int z)>();
        if (!propagateFromSelf)
        {
            foreach (Direction d in DirectionExtensions.GetDirections()) //Add each neighbour to queue
            {
                (int dx, int dy, int dz) = d.ToOffset();
                int nx = x + dx;
                int ny = y + dy;
                int nz = z + dz;

                if (!InGrid(nx, ny, nz)) continue;
                q.Push((nx, ny, nz));
            }
        }
        else
        {
            q.Push((x, y, z)); // Start with the current tile
        }
        
        while (q.Count > 0)
        {
            (x, y, z) = q.Pop();
            if (possibilities[x, y, z].Count == 0)
            {
                Debug.LogError($"No possibilities left when trying to propagate at {x}/{y}/{z}");
                return;
            }

            int countBefore = possibilities[x, y, z].Count;

            foreach (Direction d in DirectionExtensions.GetDirections())
            {
                (int dx, int dy, int dz) = d.ToOffset();
                int nx = x + dx;
                int ny = y + dy;
                int nz = z + dz;

                if (!InGrid(nx, ny, nz)) //Handle border
                {
                    /*HashSet<Tile> allowedByThis = new HashSet<Tile>();
                    foreach (var tile in possibilities[x, y, z])
                    {
                        if (tile.GetAllowed(d).Contains(border))
                        {
                            allowedByThis.Add(tile);
                        }
                    }
                    possibilities[x, y, z].IntersectWith(allowedByThis);*/
                
                    HashSet<Possibility> allowedByNeighbour = PossibilitiesFromTiles(border.GetAllowed(d.GetOpposite()));
                    possibilities[x, y, z].IntersectWith(allowedByNeighbour);
                }
                else // Normal tile
                {
                    HashSet<Possibility> allowedByThis = new HashSet<Possibility>();
                    foreach (var possibility in possibilities[x, y, z])
                    {
                        if (possibilities[nx, ny, nz].Intersect(PossibilitiesFromTiles(possibility.tile.GetAllowed(d,possibility.rotation))).Any())
                        {
                            allowedByThis.Add(possibility);
                        }
                    }
                    possibilities[x, y, z].IntersectWith(allowedByThis);
                    
                    HashSet<Tile> allowedByNeighbour = new HashSet<Tile>();
                    foreach (var possibility in possibilities[nx, ny, nz])
                    {
                        allowedByNeighbour.UnionWith(possibility.tile.GetAllowed(d.GetOpposite(),possibility.rotation));
                    }
                    possibilities[x, y, z].IntersectWith(PossibilitiesFromTiles(allowedByNeighbour));
                }
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

                    if (!InGrid(nx, ny, nz)) continue;
                    
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
        List<Possibility> allPossibleTiles = InitialPossibilities();
        possibilities = new HashSet<Possibility>[width, height, length];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < length; z++)
                {
                    possibilities[x, y, z] = new HashSet<Possibility>(allPossibleTiles);
                }
            }
        }
    }

    public List<Possibility> InitialPossibilities()
    {
        //For each tile, create a possibility for each rotation if allowRotation is true
        List<Possibility> allPossibleTiles = new List<Possibility>();
        foreach (Tile tile in tiles)
        {
            if (tile.allowRotation)
            {
                allPossibleTiles.Add(new Possibility(tile, Rotation.zero));
                allPossibleTiles.Add(new Possibility(tile, Rotation.ninety));
                allPossibleTiles.Add(new Possibility(tile, Rotation.oneEighty));
                allPossibleTiles.Add(new Possibility(tile, Rotation.twoSeventy));
            }
            else
            {
                allPossibleTiles.Add(new Possibility(tile, Rotation.zero));
            }
        }

        return allPossibleTiles;
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
        Tile newTile = Instantiate(possibility.tile, new Vector3(x, y, z), possibility.GetRotation());
        newTile.transform.SetParent(parent);
        if(animate) newTile.gameObject.SetActive(false);
    }
    
    private IEnumerator AnimatePlaceTiles()
    {
        foreach (Transform child in parentTransform)
        {
            child.gameObject.SetActive(true);
            Instantiate(poof, child.position, Quaternion.identity);
            yield return new WaitForSeconds(delayBetweenTilePlacement);
        }
    }
}
