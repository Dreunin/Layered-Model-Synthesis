using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

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

    private HashSet<Tile>[,,] possibilities;
    private Transform parentTransform;
    
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
                    
                    Tile newTile = Observe(x, y, z);
                    Propagate(x, y, z);
                    PlaceTile(x, y, z, newTile);
                }
            }
        }
        if(animate) StartCoroutine(nameof(RandomlyPlaceTiles));
    }

    

    /// <summary>
    /// Randomly selects a tile from the possibilities at the given coordinates
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private Tile Observe(int x, int y, int z)
    {
        if (!InGrid(x, y, z)) return null;
        Tile observed = possibilities[x, y, z].ElementAt(Random.Range(0, possibilities[x, y, z].Count));
        possibilities[x, y, z].Clear();
        possibilities[x, y, z].Add(observed);
        return observed;
    }


    private void Propagate(int x, int y, int z)
    {
        if (!InGrid(x, y, z)) return;
        
        Stack<(int x, int y, int z)> q = new Stack<(int x, int y, int z)>();
        foreach (Direction d in DirectionExtensions.GetDirections()) //Add each neighbour to queue
        {
            (int dx, int dy, int dz) = d.ToOffset();
            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            if (!InGrid(nx, ny, nz)) continue;
            q.Push((nx, ny, nz));
        }
        
        while (q.Count > 0)
        {
            (x, y, z) = q.Pop();
            if (possibilities[x, y, z].Count == 0)
            {
                Debug.LogError("No possibilities left when trying to propagate");
                return;
            }

            int countBefore = possibilities[x, y, z].Count;

            foreach (Direction d in DirectionExtensions.GetDirections())
            {
                (int dx, int dy, int dz) = d.ToOffset();
                int nx = x + dx;
                int ny = y + dy;
                int nz = z + dz;

                if (!InGrid(nx, ny, nz)) continue;

                HashSet<Tile> allowedByThis = new HashSet<Tile>();
                foreach (var tile in possibilities[x, y, z])
                {
                    if (possibilities[nx, ny, nz].Intersect(tile.GetAllowed(d)).Any())
                    {
                        allowedByThis.Add(tile);
                    }
                }
                possibilities[x, y, z].IntersectWith(allowedByThis);
                
                HashSet<Tile> allowedByNeighbour = new HashSet<Tile>();
                foreach (var tile in possibilities[nx, ny, nz])
                {
                    allowedByNeighbour.UnionWith(tile.GetAllowed(d.GetOpposite()));
                }
                possibilities[x, y, z].IntersectWith(allowedByNeighbour);
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


    private void InitializePossibilities()
    {
        possibilities = new HashSet<Tile>[width, height, length];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < length; z++)
                {
                    possibilities[x, y, z] = new HashSet<Tile>(tiles);
                }
            }
        }
    }
    
    private void PlaceTile(int x, int y, int z, Tile tile)
    {
        Tile newTile = Instantiate(tile, new Vector3(x, y, z), Quaternion.identity);
        newTile.transform.SetParent(parentTransform);
        if(animate) newTile.gameObject.SetActive(false);
    }
    
    private IEnumerator RandomlyPlaceTiles()
    {
        foreach (Transform child in parentTransform)
        {
            child.gameObject.SetActive(true);
            Instantiate(poof, child.position, Quaternion.identity);
            yield return new WaitForSeconds(delayBetweenTilePlacement);
        }
    }
}
