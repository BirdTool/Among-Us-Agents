using System;
using UnityEngine;

namespace AMG.UI.Elements;

public class MenuButton : UIElement
{
    public string Text;
    public Action OnClick;

    public MenuButton(string text, Action onClick)
    {
        Text = text;
        OnClick = onClick;
    }

    public override void Draw()
    {
        if (GUILayout.Button(Text, new GUIStyle(GUI.skin.button) { fontSize = 15 }))
        {
            OnClick?.Invoke();
        }
    }
}
