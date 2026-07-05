using UnityEngine;

namespace Core
{
    /// <summary>
    /// Base class for per-scene initialization.
    /// Place one subclass on a SceneSetter GameObject in each scene.
    ///
    /// Responsibilities:
    ///   1. Tell InputManager which action map this scene starts with.
    ///   2. Perform any other scene-specific setup (camera, music, etc.).
    ///
    /// Example subclass for an overworld scene:
    /// ──────────────────────────────────────────
    ///   public class OverworldSceneSetter : SceneSetter
    ///   {
    ///       protected override INPUTACTION_MAP InitialActionMap => INPUTACTION_MAP.Exploration;
    ///
    ///       protected override void OnSceneReady()
    ///       {
    ///           // spawn player, start BGM, etc.
    ///       }
    ///   }
    /// </summary>
    public abstract class SceneSetter : MonoBehaviour
    {
        // ── Override in subclass ─────────────────────────────────────────────

        /// <summary>Which input map should be active when this scene starts.</summary>
        protected abstract INPUTACTION_MAP InitialActionMap { get; }

        /// <summary>
        /// Called after the input map has been set.
        /// Put scene-specific initialization here instead of in Awake/Start.
        /// </summary>
        protected virtual void OnSceneReady() { }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Start()
        {
            // InputManager persists across scenes, so it should always exist here.
            if (InputManager.Instance == null)
            {
                Debug.LogError($"[SceneSetter] InputManager.Instance is null in scene '{gameObject.scene.name}'. " +
                               "Make sure the GameManager prefab is present in the first scene.");
                return;
            }

            InputManager.Instance.ChangeActionMap(InitialActionMap);
            OnSceneReady();
        }
    }
}
