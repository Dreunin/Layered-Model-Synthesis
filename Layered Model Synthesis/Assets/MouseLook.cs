using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MouseLookCharacterController : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;
    public float verticalRotationLimit = 90f;

    private CharacterController controller;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate the player (Y-axis)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the camera (X-axis)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalRotationLimit, verticalRotationLimit);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}