using UnityEngine;
using UnityEngine.Events;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// Shows the model that matches the unit's current form: the base model normally, and the form's
    /// model while that form is active. Put it on the unit prefab next to Unit, and keep every model
    /// under [Visuals] so the damage and death animations affect whichever one is visible.
    /// </summary>
    [RequireComponent(typeof(Unit))]
    public class UnitFormVisuals : MonoBehaviour
    {
        [Header("--- Models ---")]
        [Tooltip("Shown while the unit is in no alternate form.")]
        [SerializeField] private GameObject baseModel;
        [Tooltip("Model of each alternate form. A form without an entry here keeps the base model.")]
        [SerializeField] private FormModel[] formModels;

        [Header("--- Feedback ---")]
        [Tooltip("Invoked right after the models swap (both ways): particles, sound, etc.")]
        [SerializeField] private UnityEvent onModelSwapped;

        private Unit unit;

        private void Awake()
        {
            unit = GetComponent<Unit>();
            unit.OnFormChanged += HandleFormChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (unit != null)
                unit.OnFormChanged -= HandleFormChanged;
        }

        private void HandleFormChanged(Unit changed)
        {
            Refresh();
            onModelSwapped?.Invoke();
        }

        /// <summary>Turns on the model of the active form (or the base model) and turns the rest off.</summary>
        private void Refresh()
        {
            bool formModelShown = false;

            if (formModels != null)
            {
                foreach (FormModel entry in formModels)
                {
                    if (entry.model == null) continue;

                    bool show = entry.form != null && entry.form == unit.ActiveForm;
                    entry.model.SetActive(show);
                    formModelShown |= show;
                }
            }

            if (baseModel != null)
                baseModel.SetActive(!formModelShown);
        }

        /// <summary>Swaps the first form model by hand to check scale, pivot and animator in the editor.</summary>
        [ContextMenu("Debug/Toggle First Form Model")]
        private void DebugToggle()
        {
            if (formModels == null || formModels.Length == 0 || formModels[0].model == null) return;

            bool showForm = !formModels[0].model.activeSelf;
            formModels[0].model.SetActive(showForm);
            if (baseModel != null)
                baseModel.SetActive(!showForm);
        }
    }
}