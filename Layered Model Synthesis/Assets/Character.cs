using UnityEngine;

public class Character : MonoBehaviour
{
    public float Speed = 5f;
    private CharacterController controller;
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private double gravity;
    void Update()
    {
        Transform cameraTransform = Camera.main.transform;
        Vector3 move = (cameraTransform.right * Input.GetAxis("Horizontal") + cameraTransform.forward * Input.GetAxis("Vertical"));
        move.y = 0; // Ensure movement is only on the XZ plane
        controller.Move(move.normalized * (Speed * Time.deltaTime));
        
        gravity -= 9.81 * Time.deltaTime;
        controller.Move( new Vector3(0, (float)gravity, 0) );
        if ( controller.isGrounded ) gravity = 0;
    }
}
