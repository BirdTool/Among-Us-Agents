using System;
using UnityEngine;

namespace AMG.UI.Elements;

public class MenuCheckButton : UIElement
{
    public string Text;
    public bool IsEnabled;
    public Action<bool> OnToggled;

    public MenuCheckButton(string text, bool defaultState = false, Action<bool> onToggled = null)
    {
        Text = text;
        IsEnabled = defaultState;
        OnToggled = onToggled;
    }

    public override void Draw()
    {
        bool newState = GUILayout.Toggle(IsEnabled, Text, new GUIStyle(GUI.skin.toggle) { fontSize = 15 });
        if (newState != IsEnabled)
        {
            IsEnabled = newState;
            OnToggled?.Invoke(IsEnabled);
        }
    }
}
