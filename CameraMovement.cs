using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IrfanQavi.PlayerController {

    [RequireComponent(typeof(Camera))]
    public class CameraMovement : MonoBehaviour {

        [SerializeField] private float _xSensitivity = 30f;
        [SerializeField] private float _ySensitivity = 25f;
        [SerializeField] private Transform _orientation;
        [SerializeField] private InputActionAsset _inputActions;

        // Constants
        private const string ACTION_MAP_NAME = "Player";
        private const string LOOK_ACTION_NAME = "Look";

        // Local Fields
        private float _xRotation;
        private float _yRotation;

        private InputAction _lookAction;
        private Vector2 _look;

        private void Awake() {

            // Assign Input Actions
            _lookAction = _inputActions.FindActionMap(ACTION_MAP_NAME).FindAction(LOOK_ACTION_NAME);

            // Assign the look vector
            _lookAction.performed += context => _look = context.ReadValue<Vector2>();
            _lookAction.canceled += context => _look = Vector2.zero;

        }

        private void Start() {

            Cursor.lockState = CursorLockMode.Locked;

        }

        private void OnEnable() { _lookAction.Enable(); }

        private void OnDisable() { _lookAction.Disable(); }

        private void Update() {

            // Get Mouse Input
            float mouseX = _look.x * _xSensitivity * Time.deltaTime;
            float mouseY = _look.y * _ySensitivity * Time.deltaTime;

            // Calculate Rotations
            _xRotation -= mouseY;
            _yRotation += mouseX;

            // Clamp the X Rotation
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

            // Apply the rotations
            transform.localRotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
            _orientation.localRotation = Quaternion.Euler(0f, _yRotation, 0f);

        }

    }

}
