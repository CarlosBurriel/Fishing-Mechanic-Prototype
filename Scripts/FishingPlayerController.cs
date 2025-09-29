using UnityEngine;
using UnityEngine.InputSystem;

public class FishingPlayerController : MonoBehaviour
{
    #region Public Variables
    public bool playerCanMove;
    public float playerVelocity;
    public float cameraRotationVelocity;
    #endregion

    #region Private / Hidden Variables
    private Vector2 moveInput;
    private float turnVelocity;
    private GameObject playerCamera;
    private Rigidbody playerRb;
    private bool playerIsMoving;
    public InputSystem_Actions playerInputSystem;
    #endregion

    void OnEnable()
    {
        playerInputSystem = new InputSystem_Actions();
        playerInputSystem.FishingDemo.Enable();
        playerInputSystem.FishingDemo.CharacterMovement.performed += CharacterMovement;
        playerInputSystem.FishingDemo.CharacterMovement.canceled += CharacterMovementReleased;

        playerCamera = transform.GetChild(0).gameObject;
        playerRb = GetComponent<Rigidbody>();
    }

    #region Input System
    private void CharacterMovement(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (Mathf.Abs(moveInput.magnitude) > 0) { playerIsMoving = true; }

    }
    private void CharacterMovementReleased(InputAction.CallbackContext context) { playerIsMoving = false; playerRb.isKinematic = true; }
    #endregion

    #region Character Movement
    private void Update()
    {
        if (playerIsMoving && playerCanMove) 
        {
            playerRb.isKinematic = false;
            RotatePlayer();
            playerRb.linearVelocity = transform.forward * playerVelocity;
        }
    }

    private void RotatePlayer()
    {
        float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg + playerCamera.transform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, 0.1f);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }
    #endregion
}
