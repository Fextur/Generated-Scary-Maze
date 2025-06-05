using UnityEngine;

public class FPSController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3.0f;
    [SerializeField] private float _jumpHeight = 8.0f;

    [Header("Look")]
    [SerializeField] private float _lookSensitivity = 2.0f;
    [SerializeField] private float _maxLookAngle = 80f;

    [Header("Physics")]
    [SerializeField] private float _gravity = 9.8f;
    [SerializeField] private float _coyoteTime = 0.1f;
    [SerializeField] private float _jumpBufferTime = 0.1f;

    [Header("Footstep Audio")]
    [SerializeField] private AudioClip _footstepSound;
    [SerializeField] private float _footstepVolume = 0.5f;

    private CharacterController _controller;
    private Camera _playerCamera;
    private float _verticalLookRotation = 0f;
    private Vector3 _verticalVelocity = Vector3.zero;

    private float _lastGroundedTime;
    private float _lastJumpInputTime;

    private float _originalMoveSpeed;

    private AudioSource _footstepAudioSource;
    private Vector3 _lastPosition;
    private bool _wasWalking = false;
    private bool _isInputting = false;
    private float _lastInputTime = 0f;
    private float _stopDelay = 0.25f;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _playerCamera = GetComponentInChildren<Camera>();

        if (_controller == null)
        {
            enabled = false;
            return;
        }

        _originalMoveSpeed = _moveSpeed;

        SetupFootstepAudio();

        _lastPosition = transform.position;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetupFootstepAudio()
    {
        _footstepAudioSource = gameObject.AddComponent<AudioSource>();
        _footstepAudioSource.clip = _footstepSound;
        _footstepAudioSource.volume = _footstepVolume;
        _footstepAudioSource.loop = true;
        _footstepAudioSource.playOnAwake = false;

        _footstepAudioSource.spatialBlend = 0.3f;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();

        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        bool hasMovementInput = IsGivingMovementInput();
        bool isOnGround = _controller.isGrounded;

        if (hasMovementInput)
        {
            _isInputting = true;
            _lastInputTime = Time.time;
        }
        else
        {
            _isInputting = false;
        }

        bool shouldPlayFootsteps = isOnGround && (hasMovementInput || (Time.time - _lastInputTime < _stopDelay));

        if (shouldPlayFootsteps && !_wasWalking)
        {
            StartFootstepAudio();
        }
        else if (!shouldPlayFootsteps && _wasWalking)
        {
            StopFootstepAudio();
        }

        _wasWalking = shouldPlayFootsteps;

        _lastPosition = transform.position;
    }

    private bool IsGivingMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        return Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
    }

    private void StartFootstepAudio()
    {
        if (_footstepSound != null && _footstepAudioSource != null && !_footstepAudioSource.isPlaying)
        {
            _footstepAudioSource.Play();
        }
    }

    private void StopFootstepAudio()
    {
        if (_footstepAudioSource != null && _footstepAudioSource.isPlaying)
        {
            _footstepAudioSource.Stop();
        }
    }

    void HandleMovement()
    {
        if (_controller.isGrounded)
        {
            _lastGroundedTime = Time.time;
        }

        if (Input.GetButtonDown("Jump"))
        {
            _lastJumpInputTime = Time.time;
        }

        Vector3 horizontalMovement = Vector3.zero;
        if (_controller.isGrounded)
        {
            Vector3 moveDirection = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");

            if (moveDirection.magnitude > 1f)
            {
                moveDirection.Normalize();
            }

            horizontalMovement = moveDirection * _moveSpeed;
        }

        if (Time.time - _lastGroundedTime <= _coyoteTime &&
            Time.time - _lastJumpInputTime <= _jumpBufferTime &&
            _verticalVelocity.y <= 0)
        {
            _verticalVelocity.y = _jumpHeight;
            _lastJumpInputTime = 0;
        }

        if (!_controller.isGrounded)
        {
            _verticalVelocity.y -= _gravity * Time.deltaTime;
        }

        _controller.Move((horizontalMovement + _verticalVelocity) * Time.deltaTime);
    }

    void HandleLook()
    {
        if (_playerCamera == null) return;

        transform.Rotate(_lookSensitivity * Input.GetAxis("Mouse X") * Vector3.up);

        _verticalLookRotation -= Input.GetAxis("Mouse Y") * _lookSensitivity;
        _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, -_maxLookAngle, _maxLookAngle);
        _playerCamera.transform.localRotation = Quaternion.Euler(_verticalLookRotation, 0f, 0f);
    }

    public void SetMoveSpeed(float newSpeed)
    {
        _moveSpeed = newSpeed;
    }

    public float GetOriginalMoveSpeed()
    {
        return _originalMoveSpeed;
    }

    public void ResetMoveSpeed()
    {
        _moveSpeed = _originalMoveSpeed;
    }
}