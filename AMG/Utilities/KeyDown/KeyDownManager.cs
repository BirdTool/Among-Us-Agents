using System;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.Utilities.KeyDown
{
    public class KeyDownManager(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private readonly Dictionary<KeyCode, Action> _keyHandlers = [];

        void Awake()
        {
            KeyPressManager.DiscoverAndRegister(this);
        }

        public void RegisterKeyDown(KeyCode key, Action action)
        {
            _keyHandlers[key] = action;
        }

        public void UnregisterKeyDown(KeyCode key)
        {
            _keyHandlers.Remove(key);
        }

        void Update()
        {
            foreach (var (key, action) in _keyHandlers)
            {
                if (Input.GetKeyDown(key))
                {
                    action.Invoke();
                }
            }
        }
    }
}