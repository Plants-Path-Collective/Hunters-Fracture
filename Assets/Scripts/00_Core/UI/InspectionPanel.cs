using Core.CombatSystem.Units;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectionPanel : MonoBehaviour
{
    // References
    public UnitDefinitionSO unitInspected;

    [Header("--- Identity ---")]
    public TextMeshProUGUI nameText;
    public Image portraitImage;
    public TextMeshProUGUI tag1Text;
    public TextMeshProUGUI tag2Text;
    public TextMeshProUGUI tag3Text; 
    
    [Header("--- Combat Info ---")]
    public TextMeshProUGUI combatSlotText;
    public TextMeshProUGUI unitTypeText;

    [Header("--- Stats ---")]
    public TextMeshProUGUI HPText;
    public TextMeshProUGUI SPText;
    public TextMeshProUGUI SpeedText;
    public TextMeshProUGUI StrengthText;
    public TextMeshProUGUI MagicPowerText;
    public TextMeshProUGUI PDefText;
    public TextMeshProUGUI MDefText;
}
