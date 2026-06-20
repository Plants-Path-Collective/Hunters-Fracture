using _00_Core;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("CharacterSheet")]
    [SerializeField] private CharacterSheetSO _characterSheetSO;
    [Space(10)]
    
    [Header("Unit Stats")]
    [Space (5)]
    public float currentHP;
    public float maxHP;

    public float currentSP;
    public float maxSP;
    
    public float speed;
    public float strenght;
    public float magicPower;
    
    public float evasion;
    public float accuracy;

    public float physicalDefense;
    public float magicalDefense;

    public UNIT_STATE unitState;
}