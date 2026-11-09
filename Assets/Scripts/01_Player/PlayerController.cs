using System.Collections.Generic;
using Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Player
{
    public interface IPlayerInteractable
    {
        Transform InteractionPoint { get; }
        void Interact();
    }

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
        private readonly Dictionary<IPlayerInteractable, int> _nearbyInteractables = new();
        private InputAction _socialMoveAction;
        private InputAction _dungeonMoveAction;
        private InputAction _socialInteractAction;
        private InputAction _dungeonInteractAction;
        private bool _inputUnavailableWarningLogged;

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
            TrySubscribeToInput();
        }

        private void Start()
        {
            TrySubscribeToInput();
        }

        private void OnDisable()
        {
            if (_socialMoveAction != null) UnsubscribeFromMove(_socialMoveAction);
            if (_dungeonMoveAction != null) UnsubscribeFromMove(_dungeonMoveAction);
            if (_socialInteractAction != null) UnsubscribeFromInteract(_socialInteractAction);
            if (_dungeonInteractAction != null) UnsubscribeFromInteract(_dungeonInteractAction);

            _socialMoveAction = null;
            _dungeonMoveAction = null;
            _socialInteractAction = null;
            _dungeonInteractAction = null;

            // Limpiar input al deshabilitarse para no heredar estado sucio
            _rawInput = Vector2.zero;
            _nearbyInteractables.Clear();
        }

        private bool TrySubscribeToInput()
        {
            if (_socialMoveAction != null) return true;

            InputManager input = InputManager.Instance;
            if (input == null || input.Actions == null)
            {
                if (!_inputUnavailableWarningLogged)
                {
                    Debug.LogWarning($"[{nameof(PlayerController)}] Waiting for InputManager before subscribing to player input.", this);
                    _inputUnavailableWarningLogged = true;
                }

                return false;
            }

            _socialMoveAction = input.Social.Move;
            _dungeonMoveAction = input.Dungeon.Move;
            _socialInteractAction = input.Social.Interact;
            _dungeonInteractAction = input.Dungeon.Interact;

            SubscribeToMove(_socialMoveAction);
            SubscribeToMove(_dungeonMoveAction);
            SubscribeToInteract(_socialInteractAction);
            SubscribeToInteract(_dungeonInteractAction);
            Debug.Log(
                $"[{nameof(PlayerController)}] Subscribed to Social and Dungeon Interact. " +
                $"Current map: {input.CurrentMap}; Social enabled: {_socialInteractAction.enabled}; " +
                $"Dungeon enabled: {_dungeonInteractAction.enabled}.", this);
            _inputUnavailableWarningLogged = false;
            return true;
        }

        private void SubscribeToMove(InputAction move)
        {
            move.performed += OnMovePerformed;
            move.canceled  += OnMoveCanceled;
            move.started   += OnMovePerformed;
        }

        private void UnsubscribeFromMove(InputAction move)
        {
            move.performed -= OnMovePerformed;
            move.canceled  -= OnMoveCanceled;
            move.started   -= OnMovePerformed;
        }

        private void SubscribeToInteract(InputAction interact)
        {
            interact.started += OnInteractStarted;
            interact.performed += OnInteractPerformed;
        }

        private void UnsubscribeFromInteract(InputAction interact)
        {
            interact.started -= OnInteractStarted;
            interact.performed -= OnInteractPerformed;
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            Debug.Log(
                $"[{nameof(PlayerController)}] Interact pressed: map={context.action.actionMap.name}, " +
                $"control={context.control?.path ?? "unknown"}.", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[{nameof(PlayerController)}] Trigger enter: '{other.name}' (tag: {other.tag}).", this);
            UpdateNearbyInteractables(other, true);
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"[{nameof(PlayerController)}] Trigger exit: '{other.name}' (tag: {other.tag}).", this);
            UpdateNearbyInteractables(other, false);
        }

        private void UpdateNearbyInteractables(Collider other, bool entering)
        {
            foreach (MonoBehaviour component in other.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is not IPlayerInteractable interactable) continue;

                if (entering)
                {
                    _nearbyInteractables.TryGetValue(interactable, out int count);
                    _nearbyInteractables[interactable] = count + 1;
                    Debug.Log($"[{nameof(PlayerController)}] Registered nearby interactable '{component.name}'.", this);
                }
                else if (_nearbyInteractables.TryGetValue(interactable, out int count))
                {
                    if (count <= 1)
                        _nearbyInteractables.Remove(interactable);
                    else
                        _nearbyInteractables[interactable] = count - 1;

                    Debug.Log($"[{nameof(PlayerController)}] Unregistered nearby interactable '{component.name}'.", this);
                }
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            Debug.Log(
                $"[{nameof(PlayerController)}] Interact pressed: map={context.action.actionMap.name}, " +
                $"control={context.control?.path ?? "unknown"}, " +
                $"nearby interactables={_nearbyInteractables.Count}.", this);

            if (_nearbyInteractables.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlayerController)}] Interact pressed with no nearby interactables.", this);
                return;
            }

            IPlayerInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (IPlayerInteractable interactable in _nearbyInteractables.Keys)
            {
                Transform point = interactable.InteractionPoint;
                if (point == null) continue;

                float distance = (point.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;

                nearest = interactable;
                nearestDistance = distance;
            }

            nearest?.Interact();
        }

        private void Update()
        {
            if (_socialMoveAction == null)
                TrySubscribeToInput();

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