using System;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.Utilities.KeyDown
{
    public class KeyDownManager(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private readonly struct KeyBinding(Action action, bool onlyExecuteInGame)
        {
            public readonly Action Action = action;
            public readonly bool OnlyExecuteInGame = onlyExecuteInGame;
        }

        private readonly Dictionary<KeyCode, KeyBinding> _keyHandlers = [];

        void Awake()
        {
            KeyPressManager.DiscoverAndRegister(this);
        }

        public void RegisterKeyDown(KeyCode key, Action action, bool onlyExecuteInGame = true)
        {
            _keyHandlers[key] = new KeyBinding(action, onlyExecuteInGame);
        }

        public void UnregisterKeyDown(KeyCode key)
        {
            _keyHandlers.Remove(key);
        }

        void Update()
        {
            if (InputFocusUtils.IsTypingInInputField()) return;

            bool inGame = PlayerControl.LocalPlayer != null;

            foreach (var (key, binding) in _keyHandlers)
            {
                if (binding.OnlyExecuteInGame && !inGame) continue;

                if (Input.GetKeyDown(key))
                {
                    binding.Action.Invoke();
                }
            }
        }
    }
}