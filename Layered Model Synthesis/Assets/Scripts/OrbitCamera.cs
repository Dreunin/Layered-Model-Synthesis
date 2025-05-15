using System;
using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Vector3 focus = Vector3.zero;
    public Vector3 offset = Vector3.zero;
    public float revolutionTime = 5f;

    private float angle = 0f;
    
    public void OnValidate()
    {
        UpdatePosition();
    }

    // Update is called once per frame
    void Update()
    {
       angle += 2 * Mathf.PI / revolutionTime * Time.deltaTime;
       UpdatePosition();
    }

    private void UpdatePosition()
    {
        var distance = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
        var angledOffset = new Vector3(Mathf.Sin(angle) * distance, offset.y, Mathf.Cos(angle) * distance);
        transform.position = focus + angledOffset;
        transform.LookAt(focus);
    }
}
