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
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        controller.Move(move * (Speed * Time.deltaTime));
        
        gravity -= 9.81 * Time.deltaTime;
        controller.Move( new Vector3(0, (float)gravity, 0) );
        if ( controller.isGrounded ) gravity = 0;
    }
}
