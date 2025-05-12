using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public enum AnimationMode
{
    NoAnimation, AnimateOnCompletion, AnimateOnPlacement
}

[Serializable]
public enum RoomHandlingMode
{
    Default, DeleteExisting, MovePrevious
}

[ExecuteAlways] // Makes Update also run in editor to make synthesis possible outside of playmode
public class SynthesisController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Tileset tileset;
    [SerializeField] private int width;
    [SerializeField] private int length;
    [SerializeField] private int height;
    public int seed;
    [SerializeField] private Transform roomContainer;
    [SerializeField] private RoomHandlingMode roomHandlingMode = RoomHandlingMode.Default;
    [SerializeField] private Transform preplacedTilesContainer;

    [Header("Animation")]
    [SerializeField] private AnimationMode animationMode = AnimationMode.NoAnimation;
    [SerializeField] float timeToAnimate = 10f;

    private Queue<IEnumerator> executionQueue = new(); // Queue of tasks to be executed in the Update loop (

    private void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                StartCoroutine(executionQueue.Dequeue());
            }
        }
    }

    /// <summary>
    /// Begins the synthesis process. Can be started in-editor or at runtime and handles previous rooms based on selected RoomHandlingMode.
    /// </summary>
    /// <see cref="RoomHandlingMode"/>
    public void BeginSynthesis(int seed)
    {
        //If in editor, never animate
        if (animationMode != AnimationMode.NoAnimation && Application.isEditor && !Application.isPlaying)
        {
            animationMode = AnimationMode.NoAnimation;
            Debug.LogError("\"animation mode\" changed to no animation; you are in editor mode.");
        }
        
        if(width <= 0 || height <= 0 || length <= 0)
        {
            Debug.LogError("width, height and length must be greater than 0");
            return;
        }

        // Handle previous rooms
        if (roomHandlingMode == RoomHandlingMode.DeleteExisting)
        {
            for (int i = roomContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(roomContainer.GetChild(i).gameObject);
            }
        } else if (roomHandlingMode == RoomHandlingMode.MovePrevious)
        {
            foreach (Transform otherRoom in roomContainer)
            {
                otherRoom.position += new Vector3(0, 0, length + 5);
            }
        }
        
        var room = new GameObject($"Room {seed}").transform;
        room.SetParent(roomContainer);
        var startTime = DateTime.Now;
        
        var modelSynthesis = new ModelSynthesis(tileset, width, length, height, seed);
        modelSynthesis.OnPlaceTile += (position, possibility) => AddTask(InstantiateTile(position, possibility, room));
        modelSynthesis.OnFinish += () =>
        {
            double timeTaken = (DateTime.Now - startTime).TotalSeconds;
            if(animationMode == AnimationMode.AnimateOnCompletion) AddTask(AnimatePlaceTiles(room));
            Debug.Log($"Model Synthesis complete. Took {(int)timeTaken} seconds.");
        };

        if (preplacedTilesContainer != null)
        {
            PreplaceTiles(modelSynthesis, room);
        }

        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            modelSynthesis.Synthesise();
        }).Start();
    }

    /// <summary>
    /// Finds all preplaced tiles in the preplacedTilesContainer and places them in the possibilities grid.
    /// </summary>
    private void PreplaceTiles(ModelSynthesis modelSynthesis, Transform room)
    {
        foreach (PreplacedTile preplacedTile in preplacedTilesContainer.GetComponentsInChildren<PreplacedTile>())
        {
            Possibility possibility = new Possibility(preplacedTile.tile, preplacedTile.rotation);
            modelSynthesis.PlaceTile(preplacedTile.gridPosition.x, preplacedTile.gridPosition.y, preplacedTile.gridPosition.z, possibility);
            AddTask(InstantiateTile(preplacedTile.gridPosition, possibility, room));
        }
    }

    private void AddTask(IEnumerator task)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(task);
        }
    }
    
    /// <summary>
    /// Intantiates a tile (the prefab) at the given coordinates.
    /// </summary>
    private IEnumerator InstantiateTile(Vector3Int position, Possibility possibility, Transform room)
    {
        if(possibility.tile.dontInstantiate) yield break; //If the tile is not supposed to be instantiated, we don't
        
        // Create layer if it doesn't exist
        for (int i = room.childCount; i <= position.y; i++)
        {
            Transform newLayer = new GameObject($"Layer{i}").transform;
            newLayer.SetParent(room);
        }
        Transform layer = room.GetChild(position.y);
        
        Vector3 placement = position + new Vector3(possibility.tile.GetRotatedSize(possibility.rotation).x-1,0, possibility.tile.GetRotatedSize(possibility.rotation).z-1) / 2f;
        Tile newTile = Instantiate(possibility.tile, placement, possibility.GetRotation());
        newTile.transform.SetParent(layer);
        if (possibility.tile.allowFreeRotation) newTile.transform.eulerAngles = new Vector3(0, Random.Range(0,360), 0);
        if (animationMode != AnimationMode.NoAnimation) newTile.gameObject.SetActive(false);
        if (animationMode == AnimationMode.AnimateOnPlacement) AnimateTile(newTile.transform);
    }
    
#region TilePlacementAnimation
    
    /// <summary>
    /// Simple animation of tile placement. 
    /// </summary>
    private IEnumerator AnimatePlaceTiles(Transform room)
    {
        float timePerChild = timeToAnimate / (height*length*width);
        foreach (Transform layer in room)
        {
            foreach (Transform tile in layer)
            {
                AnimateTile(tile);
                yield return new WaitForSeconds(timePerChild);
            }
        }
    }

    private void AnimateTile(Transform tile)
    {
        tile.gameObject.SetActive(true);
        StartCoroutine(Enlarge(tile.gameObject));
    }

    private IEnumerator Enlarge(GameObject tile)
    {
        Vector3 finalScale = tile.transform.localScale;
        tile.transform.localScale = Vector3.zero;

        float growDuration = 0.8f;

        // Grow (overshoot)
        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growDuration;
            float easedT = EaseOutBack(t, 1.8f);
            tile.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, finalScale, easedT);
            yield return null;
        }

        tile.transform.localScale = finalScale;
    }


    private float EaseOutBack(float t, float overshoot)
    {
        t -= 1;
        return t * t * ((overshoot + 1) * t + overshoot) + 1;
    }
#endregion
}