using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class GeneratePath : MonoBehaviour
{
    public bool generate;
    public Tile pathPrefab;
    public int pathLength = 10;
    public int maxWidth = 10;
    public int yoffset = 1;

    public bool[,] grid;
    [FormerlySerializedAs("clearpath")] public bool clearPath;

    private void Update()
    {
        if (generate)
        {
            generate = false;
            GenerateNewPath();
        }

        if (clearPath)
        {
            clearPath = false;
            Transform path = GameObject.Find("Path").transform;
            if (path != null)
            {
                foreach (Transform child in path)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    private void GenerateNewPath()
    {
        grid = new bool[maxWidth,pathLength];
        //Never go beyond the max width
        //Go either left, right or straight
        
        Transform path = GameObject.Find("Path").transform == null ? new GameObject("Path").transform :  GameObject.Find("Path").transform;

        transform.position = new Vector3(0, yoffset, (int)pathLength/2);
        
        int length = 0;
        
        while(length+1 < maxWidth)
        {
            //Get a random number between 0 and 2
            int random = UnityEngine.Random.Range(0, 3);
            Vector3Int direction = Vector3Int.zero;
            if (random == 0)
            {
                direction = Vector3Int.forward;
            }
            else if (random == 1)
            {
                direction = Vector3Int.back;
            }
            else
            {
                direction = Vector3Int.right;
                length++;
            }

            //Check if the path is within the max width
            if (Mathf.Abs(transform.position.z + direction.z) < length)
            {
                //Check if the tile is already placed
                if((int)transform.position.z + direction.z < 0 || (int)transform.position.z + direction.z >= pathLength)
                    continue;
                if(grid[(int)transform.position.x + direction.x, (int)transform.position.z+direction.z]) 
                    continue;
                
                GameObject go = new GameObject("PathTile");
                go.transform.SetParent(path);
                PreplacedTile ppt = go.AddComponent<PreplacedTile>();
                ppt.gridPosition = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
                ppt.tile = pathPrefab;
                grid[(int)transform.position.x, (int)transform.position.z] = true;
                
                transform.position += direction;
                
            }
        }
        
    }
}
