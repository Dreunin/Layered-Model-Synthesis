using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class SynthesisController : MonoBehaviour
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
    
    public void BeginSynthesis(int seed)
    {
        //If in editor, never animate
        if (animate && Application.isEditor && !Application.isPlaying)
        {
            animate = false;
            Debug.LogError("\"animate\" changed to FALSE; you are in editor mode.");
        }
        
        if(width <= 0 || height <= 0 || length <= 0)
        {
            Debug.LogError("width, height and length must be greater than 0");
            return;
        }

        var room = new GameObject($"Room {seed}").transform;
        var startTime = DateTime.Now;
        
        var modelSynthesis = new ModelSynthesis(tileset, width, length, height, seed);
        modelSynthesis.OnPlaceTile += (position, possibility) => PlaceTile(position, possibility, room);
        modelSynthesis.Synthesise();
        
        double timeTaken = (DateTime.Now - startTime).TotalSeconds;
        if(animate) StartCoroutine(AnimatePlaceTiles(room));
        Debug.Log($"Model Synthesis complete. Took {(int)timeTaken} seconds.");
    }
    
    /// <summary>
    /// Places a tile (prefab) at the given coordinates.
    /// </summary>
    private void PlaceTile(Vector3Int position, Possibility possibility, Transform room)
    {
        if(possibility.tile.dontInstantiate) return; //If the tile is not supposed to be instantiated, we don't
        
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
        if(possibility.tile.allowFreeRotation) newTile.transform.eulerAngles = new Vector3(0, Random.Range(0,360), 0);
        if(animate) newTile.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Simple animation of tile placement accompanied by VFX. 
    /// </summary>
    private IEnumerator AnimatePlaceTiles(Transform room)
    {
        float timePerChild = timeToAnimate / (height*length*width);
        foreach (Transform layer in room)
        {
            foreach (Transform tile in layer)
            {
                tile.gameObject.SetActive(true);
                StartCoroutine(Enlarge(tile.gameObject));
                Instantiate(poof, tile.position, Quaternion.identity);
                yield return new WaitForSeconds(timePerChild);
            }
        }
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
}