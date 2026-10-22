using System;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.UI.Elements;

// "<  Option  >" selector. Either arrow moves to the next/previous option and wraps around
public class MenuTextSlider : UIElement
{
    private const float ArrowWidth = 40f;

    public IReadOnlyList<string> Options;
    public int Index;
    public Action<int> OnChanged;

    // Optional: read the real current value every frame, so the label stays correct
    // when something else (not this menu) changes it.
    public Func<int> GetIndex;

    public string Current => Options.Count == 0 ? string.Empty : Options[Index];

    public MenuTextSlider(IReadOnlyList<string> options, int defaultIndex = 0, Action<int> onChanged = null, Func<int> getIndex = null)
    {
        Options = options;
        Index = Wrap(defaultIndex);
        OnChanged = onChanged;
        GetIndex = getIndex;
    }

    public override void Draw()
    {
        if (Options == null || Options.Count == 0) return;

        if (GetIndex != null)
            Index = Wrap(GetIndex());

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("<", new GUIStyle(GUI.skin.button) { fontSize = 15 }, GUILayout.Width(ArrowWidth)))
            Move(-1);

        GUILayout.Label(Current, new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter
        }, GUILayout.ExpandWidth(true));

        if (GUILayout.Button(">", new GUIStyle(GUI.skin.button) { fontSize = 15 }, GUILayout.Width(ArrowWidth)))
            Move(1);

        GUILayout.EndHorizontal();
    }

    private void Move(int step)
    {
        Index = Wrap(Index + step);
        OnChanged?.Invoke(Index);
    }

    private int Wrap(int index)
    {
        int count = Options?.Count ?? 0;
        return count == 0 ? 0 : ((index % count) + count) % count;
    }
}
