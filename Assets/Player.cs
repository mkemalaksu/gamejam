using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [Tooltip("2D Vector composite (WASD) or left analog stick")]
    [SerializeField] private InputActionProperty moveAction;

    [Header("Fall Settings")]
    [Tooltip("Maximum constant fall speed to reach")]
    [SerializeField] private float terminalFallSpeed = 45f;
    [Tooltip("Acceleration when entering free fall")]
    [SerializeField] private float fallAcceleration = 35f;

    [Header("Airborne Horizontal Control")]
    [Tooltip("Horizontal dodge speed")]
    [SerializeField] private float horizontalMoveSpeed = 16f;
    [Tooltip("Agility of changing direction and stopping")]
    [SerializeField] private float movementResponsiveness = 18f;

    [Header("Camera")]
    [Tooltip("Automatically finds the main camera if left empty")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector2 currentHorizontalVelocity;
    private float currentVerticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        moveAction.action?.Enable();
    }

    private void OnDisable()
    {
        moveAction.action?.Disable();
    }

    private void Update()
    {
        
        // 1. Vertical Fall Calculation (Locks to terminal speed)
        currentVerticalSpeed = Mathf.MoveTowards(
            currentVerticalSpeed,
            terminalFallSpeed,
            fallAcceleration * Time.deltaTime
        );

        // 2. Read Vector2 Input via New Input System
        Vector2 rawInput = moveAction.action != null 
            ? moveAction.action.ReadValue<Vector2>() 
            : Vector2.zero;

        if (rawInput.sqrMagnitude > 1f)
        {
            rawInput.Normalize();
        }

        // 3. Horizontal Acceleration and Quick Maneuver Response
        currentHorizontalVelocity = Vector2.MoveTowards(
            currentHorizontalVelocity,
            rawInput * horizontalMoveSpeed,
            movementResponsiveness * Time.deltaTime
        );

        // 4. Project Movement onto the Camera's Horizontal Plane
        Vector3 moveDirection;
        if (cameraTransform != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);

            // Resolves locking when the camera reaches a perpendicular angle
            if (camForward.sqrMagnitude < 0.001f)
            {
                camForward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
            }
            camForward = camForward.normalized;

            Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            moveDirection = (camRight * currentHorizontalVelocity.x) + (camForward * currentHorizontalVelocity.y);
        }
        else
        {
            moveDirection = (transform.right * currentHorizontalVelocity.x) + (transform.forward * currentHorizontalVelocity.y);
        }

        // 5. Apply Final Movement
        Vector3 finalVelocity = moveDirection + (Vector3.down * currentVerticalSpeed);
        controller.Move(finalVelocity * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.65f)
        {
            currentVerticalSpeed = 0f;
        }
    }
}
