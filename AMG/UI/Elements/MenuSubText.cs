using UnityEngine;

namespace AMG.UI.Elements;

public class MenuSubText : UIElement
{
    public string Text;

    public MenuSubText(string text)
    {
        Text = text;
    }

    public override void Draw()
    {
        GUILayout.Label(Text, new GUIStyle(GUI.skin.label)
        {
            fontSize = 14
        });
    }
}
