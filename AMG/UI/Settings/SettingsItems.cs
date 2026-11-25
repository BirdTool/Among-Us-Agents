using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AMG.UI.Settings
{
    // Builds the buttons of the mod tab by cloning the game's own toggle
    public sealed class SettingsItems(OptionsMenuBehaviour menu, Transform parent)
    {
        private static readonly Color32 On = new(0, 150, 0, 255);
        private static readonly Color32 Off = new(77, 77, 77, 255);
        private static readonly Color32 Choice = new(40, 90, 140, 255);

        private readonly SettingsLayout _layout = new();

        // label: On / Off
        public void Toggle(string label, Func<bool> get, Action<bool> set)
        {
            var button = Create(label, _layout.NextCell());
            void Refresh() => Paint(button, get() ? On : Off, $"{label}: {(get() ? "On" : "Off")}", get());

            OnClick(button, () =>
            {
                set(!get());
                Refresh();
            });
            Refresh();
        }

        // label: value. Each click goes to the next value.
        public void Cycle(string label, string[] options, Func<int> get, Action<int> set)
        {
            var button = Create(label, _layout.NextCell());
            void Refresh() => Paint(button, Choice, $"{label}: {options[Mathf.Clamp(get(), 0, options.Length - 1)]}", true);

            OnClick(button, () =>
            {
                set((get() + 1) % options.Length);
                Refresh();
            });
            Refresh();
        }

        // [-]  label: value  [+]  on a row of its own
        public void Stepper(string label, int min, int max, Func<int> get, Action<int> set)
        {
            var centre = _layout.NextRow();

            var middle = Create(label, centre);
            var minus = Create("-", centre + new Vector3(-2.0f, 0f, 0f));
            var plus = Create("+", centre + new Vector3(2.0f, 0f, 0f));
            Narrow(minus);
            Narrow(plus);

            void Refresh() => Paint(middle, Choice, $"{label}: {get()}", true);

            OnClick(middle, () => { });
            OnClick(minus, () =>
            {
                set(Mathf.Max(min, get() - 1));
                Refresh();
            });
            OnClick(plus, () =>
            {
                set(Mathf.Min(max, get() + 1));
                Refresh();
            });

            Paint(minus, Choice, "-", true);
            Paint(plus, Choice, "+", true);
            Refresh();
        }

        // A plain action. enabled == false: it is drawn grey and does nothing.
        public void Button(string label, Action onClick, Func<bool> enabled = null)
        {
            var button = Create(label, _layout.NextCell());
            bool canClick = enabled?.Invoke() ?? true;

            Paint(button, canClick ? On : Off, label, canClick);
            OnClick(button, () =>
            {
                if (canClick) onClick();
            });
        }

        private ToggleButtonBehaviour Create(string label, Vector3 position)
        {
            var button = Object.Instantiate(menu.DisableMouseMovement, parent);
            button.name = label;
            button.transform.localPosition = position;

            foreach (var translator in button.GetComponentsInChildren<TextTranslatorTMP>(true))
                Object.Destroy(translator);

            button.Text.text = label;
            return button;
        }

        private static void Paint(ToggleButtonBehaviour button, Color32 colour, string text, bool active)
        {
            button.Background.color = colour;
            button.Rollover?.ChangeOutColor(colour);
            button.Text.color = new Color(1f, 1f, 1f, active ? 1f : 0.5f);
            button.Text.text = text;
        }

        private static void OnClick(ToggleButtonBehaviour button, Action action)
        {
            var passive = button.GetComponent<PassiveButton>();
            passive.OnClick = new();
            passive.OnClick.AddListener(action);
        }

        // the small - and + buttons are half as wide
        private static void Narrow(ToggleButtonBehaviour button)
        {
            var background = button.transform.Find("Background");
            background?.localScale = new Vector3(0.5f, 1f, 1f);

            var highlight = button.transform.Find("ButtonHighlight");
            highlight?.localScale = new Vector3(0.5f, 0.95f, 1f);
        }
    }
}
