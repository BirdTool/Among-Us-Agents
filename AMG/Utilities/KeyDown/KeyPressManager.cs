using System;
using System.Reflection;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Utilities.KeyDown
{
    public static class KeyPressManager
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class RegisterAttribute(KeyCode key) : Attribute
        {
            public KeyCode Key { get; } = key;

            public bool OnlyExecuteInGame { get; set; } = true;
        }

        public static void DiscoverAndRegister(KeyDownManager manager)
        {
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<RegisterAttribute>();
                if (attr == null) continue;

                var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    LogManager.LogWarning($"[KeyPressManager] {type.Name} tem [Register] mas não define um método estático Execute().");
                    continue;
                }

                var action = (Action)Delegate.CreateDelegate(typeof(Action), method);
                manager.RegisterKeyDown(attr.Key, action, attr.OnlyExecuteInGame);

                LogManager.LogDebug($"[KeyPressManager] {type.Name} registrado na tecla {attr.Key} (OnlyExecuteInGame={attr.OnlyExecuteInGame}).");
            }
        }
    }
}