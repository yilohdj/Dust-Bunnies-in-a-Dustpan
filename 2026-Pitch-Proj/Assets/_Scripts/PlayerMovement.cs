using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float rotationSpeed = 180f;

    // Update is called once per frame
    void Update()
    {
        Vector3 rotation = new Vector3(0, Input.GetAxisRaw("Horizontal") * rotationSpeed * Time.deltaTime, 0);

        Vector3 move = new Vector3(0, 0, Input.GetAxisRaw("Vertical") * Time.deltaTime);
        move = transform.TransformDirection(move);
        characterController.Move(move * speed);
        transform.Rotate(rotation);
    }
}
