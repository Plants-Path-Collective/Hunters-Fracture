using Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Movement")]
        [SerializeField] private float moveSpeed    = 5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 15f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Camera")]
        [SerializeField] private CinemachineBrain cinemachineBrain;

        // ── Private state ─────────────────────────────────────────────────────

        private NavMeshAgent _agent;
        private Animator     _animator;

        private Vector2 _rawInput;
        private Vector3 _velocity;
        private float   _currentSpeed;

        private static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _agent    = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            // We drive rotation ourselves (camera-relative facing); the agent should only
            // constrain position to the NavMesh, never auto-rotate or auto-path on its own.
            _agent.updateRotation = false;
            _agent.updatePosition = true;

            if (cinemachineBrain == null)
                cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

            if (cinemachineBrain == null)
                Debug.LogError("[PlayerController] No CinemachineBrain found. " +
                               "Make sure the main camera has a CinemachineBrain component.");
        }

        private void OnEnable()
        {
            var move = InputManager.Instance.Overworld.Move;
            move.performed += OnMovePerformed;
            move.canceled  += OnMoveCanceled;   // Value actions sí disparan canceled
            move.started   += OnMovePerformed;  // por si performed no llega al primer frame
        }

        private void OnDisable()
        {
            if (InputManager.Instance == null) return;

            var move = InputManager.Instance.Overworld.Move;
            move.performed -= OnMovePerformed;
            move.canceled  -= OnMoveCanceled;
            move.started   -= OnMovePerformed;

            // Limpiar input al deshabilitarse para no heredar estado sucio
            _rawInput = Vector2.zero;
        }

        private void Update()
        {
            MoveAndRotate(GetCameraRelativeDirection(_rawInput));
            DriveAnimator();
        }

        // ── Input handlers ────────────────────────────────────────────────────

        private void OnMovePerformed(InputAction.CallbackContext ctx)
            => _rawInput = ctx.ReadValue<Vector2>();

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
            => _rawInput = Vector2.zero;

        // ── Movement ──────────────────────────────────────────────────────────

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f) return Vector3.zero;

            Camera activeCamera = cinemachineBrain.OutputCamera;

            Vector3 camForward = activeCamera.transform.forward;
            Vector3 camRight   = activeCamera.transform.right;
            camForward.y = 0f;
            camRight.y   = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector2 clampedInput = Vector2.ClampMagnitude(input, 1f);
            return (camForward * clampedInput.y + camRight * clampedInput.x).normalized;
        }

        private void MoveAndRotate(Vector3 worldDirection)
        {
            Vector3 targetVelocity = worldDirection * moveSpeed;
            float   rate           = worldDirection.sqrMagnitude > 0.001f ? acceleration : deceleration;

            _velocity     = Vector3.MoveTowards(_velocity, targetVelocity, rate * Time.deltaTime);
            _currentSpeed = _velocity.magnitude;

            // Agent.Move applies a relative offset while constraining the result to the
            // NavMesh — same call shape as CharacterController.Move() had, but the surface
            // (and what counts as "walkable") is now whatever's baked into the NavMesh.
            _agent.Move(_velocity * Time.deltaTime);

            if (_velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_velocity, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // ── Animator ──────────────────────────────────────────────────────────

        private void DriveAnimator()
        {
            if (_animator == null) return;
            _animator.SetFloat(AnimMoveSpeed, _currentSpeed / moveSpeed, 0.1f, Time.deltaTime);
        }
    }
}