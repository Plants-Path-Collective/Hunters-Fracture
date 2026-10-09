using Core;
using Core.CombatSystem;
using Core.CombatSystem.Units;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Combat Camera Selection")]
    [SerializeField] private CombatController combat;
    [SerializeField] private CinemachineCamera leftTurnCamera;
    [SerializeField] private CinemachineCamera midTurnCamera;
    [SerializeField] private CinemachineCamera rightTurnCamera;
    [SerializeField] private CinemachineCamera noAllyTurnCamera;

    public static List<CinemachineCamera> cameras = new();
    public static CinemachineCamera ActiveCamera;

    private void OnEnable()
    {
        if (combat == null)
        {
            Debug.LogError($"[{nameof(CameraManager)}] Assign a CombatController.");
            return;
        }

        combat.OnTurnStart += HandleTurnStarted;
        combat.OnTurnSkipped += HandleTurnSkipped;
        combat.OnCombatEnd += HandleCombatEnded;
    }

    private void OnDisable()
    {
        if (combat == null) return;

        combat.OnTurnStart -= HandleTurnStarted;
        combat.OnTurnSkipped -= HandleTurnSkipped;
        combat.OnCombatEnd -= HandleCombatEnded;
    }

    private void HandleTurnStarted(Unit actor) => SwitchForActor(actor);

    private void HandleTurnSkipped(Unit actor) => SwitchForActor(actor);

    private void HandleCombatEnded(COMBAT_OUTCOME outcome)
    {
        SwitchCamera(noAllyTurnCamera);
    }

    private void SwitchForActor(Unit actor)
    {
        if (actor == null)
        {
            Debug.LogError($"[{nameof(CameraManager)}] Cannot select a camera for a null actor.");
            return;
        }

        if (actor.Team != UNIT_TEAM.Ally)
        {
            SwitchCamera(noAllyTurnCamera);
            return;
        }

        if (actor.Slot is not COMBAT_SLOT slot)
        {
            Debug.LogError($"[{nameof(CameraManager)}] Ally '{actor.Name}' has no combat slot.");
            return;
        }

        CinemachineCamera turnCamera = slot switch
        {
            COMBAT_SLOT.Left => leftTurnCamera,
            COMBAT_SLOT.Mid => midTurnCamera,
            COMBAT_SLOT.Right => rightTurnCamera,
            _ => null
        };

        if (turnCamera == null)
        {
            Debug.LogError($"[{nameof(CameraManager)}] No camera is assigned for ally slot {slot}.");
            return;
        }

        SwitchCamera(turnCamera);
    }

    public static bool IsActiveCamera(CinemachineCamera cam)
    {
        return cam == ActiveCamera;
    }

    public static void SwitchCamera(CinemachineCamera newCam)
    {
        if (newCam == null)
        {
            Debug.LogError($"[{nameof(CameraManager)}] Cannot switch to a null camera.");
            return;
        }

        newCam.Priority = 10;
        ActiveCamera = newCam;

        foreach (CinemachineCamera cam in cameras)
        {
            if (cam != null && cam != newCam)
                cam.Priority = 0;
        }
    }   

    public static void Register(CinemachineCamera cam)
    {
        if (cam != null && !cameras.Contains(cam))
            cameras.Add(cam);
    }

    public static void Unregister(CinemachineCamera cam)
    {
        cameras.Remove(cam);
        if (ActiveCamera == cam)
            ActiveCamera = null;
    }
}
