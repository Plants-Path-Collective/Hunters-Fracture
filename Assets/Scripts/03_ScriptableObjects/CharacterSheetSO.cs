using JetBrains.Annotations;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterSheet", menuName =  "ScriptableObjects/CharacterSheet", order = 1)]
public class CharacterSheetSO : ScriptableObject
{
    [Header("Unit Stats")]
    [Space (5)]
    
    [Tooltip(("Cantidad de puntos de vida de la Unit"))]
    public float HP;
    
    [Tooltip(("Cantidad de puntos de mana/energía de la Unit"))]
    public float SP;
    
    [Tooltip(("Cantidad de puntos de Velocidad de la Unit"))]
    public float speed;
    
    [Tooltip(("Cantidad de puntos de Fuerza de la Unit, " +
              "se utiliza en el calculo de Ataques Físicos"))]
    public float strenght;
    
    [Tooltip(("Cantidad de puntos de Poder Mágico de la Unit, " +
              "se utiliza en el calculo de Ataques Mágicos"))]
    public float magicPower;
    
    [Tooltip(("Cantidad de puntos de Evasión de la Unit"))]
    public float evasion;
    
    [Tooltip(("Cantidad de puntos de Precisión de la Unit, " +
              "100 por defecto"))]
    public float accuracy = 100;

    [Tooltip(("Cantidad de puntos de Defensa ante Ataques Físicos"))]
    public float physicalDefense;
    
    [Tooltip(("Cantidad de puntos de Defensa ante Ataques Mágicos"))]
    public float magicalDefense;
    [Space(15)] 

    [Header("Character Information")] 
    [Space(5)]
    [CanBeNull]
    public Sprite characterPortrait;
    
    [Tooltip(("Nombre del Hunter, se utiliza para la Wiki, el display de la Party, " +
              "el panel de info durante el Combate, y para la ejecución de conversaciones"))]
    public string characterName;
    
    [TextArea(4, 10)]
    [Tooltip(("Descripción del personaje, se utiliza para el apartado de Hunters en la Wiki del Beeper"))]
    public string characterPhysicalDescription;
    [Space (15)]

    [Header("Model & Animations for Overworld")] 
    [Space(5)]
    [CanBeNull]
    public AnimationClip ow_idleAnimation;
    public AnimationClip ow_walkAnimation;
    public AnimationClip ow_interactAnimation;
    [Space(15)] 

    [Header("Model & Animations for Overworld")] 
    [Space(5)] 
    [CanBeNull]
    public AnimationClip cb_idleAnimation;
    public AnimationClip cb_runAnimation;
    public AnimationClip cb_basicAttackAnimation;
    public AnimationClip cb_skill1Animation;
    public AnimationClip cb_skill2Animation;
    public AnimationClip cb_ultimateAnimation;
}