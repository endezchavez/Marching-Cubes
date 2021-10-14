using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float jumpForce = 5f;
    private Rigidbody rb;
    private Vector3 movement;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        currentSpeed = walkSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveInput = InputManager.Instance.GetMovementInput();
        moveInput = moveInput.normalized;
        movement = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (InputManager.Instance.PlayerJumpedThisFrame())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + (movement * Time.deltaTime * walkSpeed));
    }
}
