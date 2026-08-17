using UnityEngine;

namespace AMG.UI.Elements;

public class MenuText : UIElement
{
    public string Text;

    public MenuText(string text)
    {
        Text = text;
    }

    public override void Draw()
    {
        GUILayout.Label(Text, new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        });
    }
}
