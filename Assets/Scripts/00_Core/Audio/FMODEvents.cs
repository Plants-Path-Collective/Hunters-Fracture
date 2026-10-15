using UnityEngine;
using FMODUnity;
public class FMODEvents : MonoBehaviour
{
    [field: Header("Music")]
    [field: SerializeField] public EventReference CombatMusic { get; private set; }

    public static FMODEvents instance { get; private set;  }

    private void Awake()
    {
        if (instance != null) 
        {
            Debug.LogError("Se encontró mas de un FMODEvents en esta escena");
        }
        instance = this;
    }
}
