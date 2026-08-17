using UnityEngine;
using System;

namespace AMG.UI.Elements;

public class MenuSlider : UIElement
{
    public float Min;
    public float Max;
    public float Value;
    public Action<float> OnValueChanged;

    public MenuSlider(float min, float max, float defaultValue = 0f, Action<float> onValueChanged = null)
    {
        Min = min;
        Max = max;
        Value = Mathf.Clamp(defaultValue, min, max);
        OnValueChanged = onValueChanged;
    }

    public override void Draw()
    {
        GUILayout.BeginHorizontal();
        float newValue = GUILayout.HorizontalSlider(Value, Min, Max);
        if (Mathf.Abs(newValue - Value) > 0.001f)
        {
            Value = newValue;
            OnValueChanged?.Invoke(Value);
        }
        GUILayout.Label(Value.ToString("0.00"), GUILayout.Width(50));
        GUILayout.EndHorizontal();
    }
}
