using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance;
        
        [SerializeField] private InputActionMap currentInputActionMap;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void ChangeActionMap()
        {
            
        }
    }
}