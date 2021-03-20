using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerMovementController : NetworkBehaviour {
    [SerializeField] private CharacterController controller = null;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float jumpHeight = 3f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 10f;

    [Header("General")]
    [SerializeField] private Transform playerCamera = null;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private Transform groundCheck = null;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private Vector2 previousInput;

    private Controls controls;
    private Controls Controls {
        get {
            if (controls != null) { return controls; }
            return controls = new Controls();
        }
    }
    
    public override void OnStartAuthority() {
        playerCamera.gameObject.SetActive(true);

        enabled = true;

        Cursor.lockState = CursorLockMode.Locked;

        Controls.Player.Move.performed += ctx => SetMovement(ctx.ReadValue<Vector2>());
        Controls.Player.Move.canceled += ctx => ResetMovement();
        Controls.Player.Look.performed += ctx => Look(ctx.ReadValue<Vector2>());
        Controls.Player.Jump.performed += ctx => Jump();
    }

    [ClientCallback]
    private void OnEnable() => Controls.Enable();

    [ClientCallback]
    private void OnDisable() => Controls.Disable();

    [ClientCallback]
    private void FixedUpdate() {
        Move();
    }

    [Client]
    private void SetMovement(Vector2 movement) => previousInput = movement;

    [Client]
    private void ResetMovement() => previousInput = Vector2.zero;

    [Client]
    private void Look(Vector2 lookAxis) {
        var mouseX = lookAxis.x * mouseSensitivity * Time.deltaTime;
        var mouseY = lookAxis.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    [Client]
    private void Move() {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0) {
            // Setting to -2f instead of 0f to make sure player is grounded
            velocity.y = -2f;
        }

        Vector3 direction = transform.right * previousInput.x + transform.forward * previousInput.y;
        controller.Move(direction * movementSpeed * Time.fixedDeltaTime);

        velocity.y += gravity * Time.fixedDeltaTime;
        // Multiplying with "Time.fixedDeltaTime" again because of gravity equation: delta y = 1/2 * g * t^2
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    [Client]
    private void Jump() {
        if (!isGrounded) {
            return;
        }
        // Applying equation to calculate jump distance: v = sqrt(h * -2 * g)
        velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
    }
}
