using UnityEngine;
using UnityEngine.Localization;

namespace PlantsPathCo.DialogueSystem
{
    [CreateAssetMenu(fileName = "NewSpeaker", menuName = "Dialogue System/Speaker")]
    public class SpeakerSO : ScriptableObject
    {
        [Tooltip("Nombre del personaje a mostrar en el panel de diálogo. Localizado, " +
                 "por si el nombre cambia entre idiomas.")]
        public LocalizedString speakerName;

        [Tooltip("Retrato/ilustración del personaje a mostrar junto al nombre.")]
        public Sprite portrait;
    }
}
