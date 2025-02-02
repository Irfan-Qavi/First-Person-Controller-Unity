using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IrfanQavi.PlayerController {

    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovement : MonoBehaviour {

        [Header("Basic Movement")]
        [SerializeField] private float _walkSpeed = 7f;
        [SerializeField] private float _sprintSpeed = 10f;
        [SerializeField] private Transform _orientation;
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Ground")]
        [SerializeField] private float _groundDrag = 5f;
        [SerializeField] private float _groundCheckRadius = .5f;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private LayerMask _groundMask;

        [Header("Jumping")]
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private float _jumpCoolDown = .5f;
        [SerializeField] private float _airMultiplier = .4f;
        [SerializeField] private float _airDrag = 3f;

        [Header("Slope Movement")]
        [SerializeField] private float _minSlopeAngle = 5f;
        [SerializeField] private float _maxSlopeAngle = 45f;

        // Constants
        private const string ACTION_MAP_NAME = "Player";
        private const string MOVE_ACTION_NAME = "Move";
        private const string JUMP_ACTION_NAME = "Jump";
        private const string SPRINT_ACTION_NAME = "Sprint";

        // Private Fields
        private float _moveSpeed;
        private float _horizontalInput;
        private float _verticalInput;
        private Vector3 _moveDirection;
        private Rigidbody _rigidbody;

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private Vector2 _move;

        private bool _isJumping = false;
        private bool _isReadyToJump = true;
        private bool _isGrounded;
        public bool IsJumping { get { return _isJumping; } }

        private RaycastHit _slopeHit;
        private bool _isExitingSlope;

        // Enum for creating the current movement states
        private MovementState _currentState;
        public enum MovementState { Walking, Sprinting, Air }
        public MovementState CurrentState { get { return _currentState; } }

        private void Awake() {

            // Assign Input Actions
            _moveAction = _inputActions.FindActionMap(ACTION_MAP_NAME).FindAction(MOVE_ACTION_NAME);
            _jumpAction = _inputActions.FindActionMap(ACTION_MAP_NAME).FindAction(JUMP_ACTION_NAME);
            _sprintAction = _inputActions.FindActionMap(ACTION_MAP_NAME).FindAction(SPRINT_ACTION_NAME);
            
            // Assign the move vector
            _moveAction.performed += context => _move = context.ReadValue<Vector2>();
            _moveAction.canceled += context => _move = Vector2.zero;

        }

        private void Start() {

            // Assign References
            _rigidbody = GetComponent<Rigidbody>();

            // Freeze the rotation
            _rigidbody.freezeRotation = true;

        }

        private void OnEnable() {

            _moveAction.Enable();
            _jumpAction.Enable();
            _sprintAction.Enable();

        }

        private void OnDisable() {

            _moveAction.Disable();
            _jumpAction.Disable();
            _sprintAction.Disable();

        }

        private void Update() {

            // Call the methods
            GetInput();
            ControlSpeed();
            HandleStates();

            // Perform a ground check
            _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundCheckRadius, _groundMask);

            // Handle Drag
            _rigidbody.linearDamping = _isGrounded ? _groundDrag : _airDrag;

            // Handle Jumping
            if (_jumpAction.inProgress && _isReadyToJump && _isGrounded) {
                
                // Set the booleans
                _isReadyToJump = false;
                _isJumping = true;

                // Perform the jump and reset it after sometime
                Jump();
                Invoke(nameof(ResetJump), _jumpCoolDown);
                
            }

        }

        private void FixedUpdate() {

            // Call the method move player
            MovePlayer();
            
        }

        private void GetInput() {

            // Set the appropriate values
            _horizontalInput = _move.x;
            _verticalInput = _move.y;

        }

        private void MovePlayer() {

            // Calculate Movement Direction
            _moveDirection = _orientation.right * _horizontalInput + _orientation.forward * _verticalInput;

            // Move the Player on Slope
            if (OnSlope() && !_isExitingSlope) {

                _rigidbody.AddForce(_moveSpeed * 20f * GetSlopeMoveDirection(), ForceMode.Force);

                // Push the player to keep him constantly on Slope
                if (_rigidbody.linearVelocity.y > 0) {

                    _rigidbody.AddForce(Vector3.down * 80f, ForceMode.Force);

                }

            }

            // Move the Player on Ground
            else if (_isGrounded) {
    
                _rigidbody.AddForce(_moveSpeed * 10f * _moveDirection, ForceMode.Force);

            }

            // Move the Player in Air
            else {

                _rigidbody.AddForce(_moveSpeed * _airMultiplier * 10f * _moveDirection, ForceMode.Force);

            }

            // Turn Gravity off while on Slope
            _rigidbody.useGravity = !OnSlope();

        }

        private void ControlSpeed() {

            // Limiting Speed on Slopes
            if (OnSlope() && _rigidbody.linearVelocity.magnitude > _moveSpeed) {

                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _moveSpeed;

            }

            // Limiting Speed on Ground or in Air
            else {

                // Get flat velocity
                Vector3 flatVelocity = new(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

                // Control Speed if needed
                if (flatVelocity.magnitude > _moveSpeed) {

                    // Calculate Limited Velocity and apply it
                    Vector3 limitedVelocity = flatVelocity.normalized * _moveSpeed;
                    _rigidbody.linearVelocity = new Vector3(limitedVelocity.x, _rigidbody.linearVelocity.y, limitedVelocity.z);

                }

            }

        }

        private void HandleStates() {

            // Sprinting Mode
            if (_isGrounded && _sprintAction.inProgress) {

                _currentState = MovementState.Sprinting;
                _moveSpeed = _sprintSpeed;

            }

            // Walking Mode
            else if (_isGrounded) {

                _currentState = MovementState.Walking;
                _moveSpeed = _walkSpeed;

            }
            
            // Air Mode
            else {

                _currentState = MovementState.Air;

            }

        }

        private void Jump() {

            // Set Exiting Slope to True
            _isExitingSlope = true;

            // Reset Y Velocity
            _rigidbody.linearVelocity = new(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

            // Add Jump Force
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);

        }

        private void ResetJump() {
            
            // Set the jump and slope values
            _isReadyToJump = true;
            _isJumping = false;
            _isExitingSlope = false;
            
        }

        private bool OnSlope() {

            // Perform a raycast
            if (Physics.Raycast(_groundCheck.position, Vector3.down, out _slopeHit, _groundCheckRadius)) {

                float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);

                // Return true if the angle is less than max slope angle but more than min slope angle
                return angle <= _maxSlopeAngle && angle >= _minSlopeAngle;

            }

            // Else, return false
            return false;

        }

        private Vector3 GetSlopeMoveDirection() {

            return Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;

        }

    }

}
