using System;
using System.Collections.Generic;
using AMG.UI.Tabs;
using UnityEngine;

namespace AMG.UI;

public class MenuUI : MonoBehaviour
{
    public MenuUI(IntPtr ptr) : base(ptr) { }

    private static int windowWidth = 700;
    private static int windowHeight = 550;
    private Rect _windowRect;

    public static bool IsGUIActive = false;
    private List<TabBase> _tabs = new();
    private int _selectedTab = 0;

    private void Start()
    {
        // Add tabs
        _tabs.Add(new AgentsTab());

        // Center window on screen initially (will be re-centered later if 0)
        _windowRect = new Rect(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
        
        AMGPlugin.Log.LogInfo("MenuUI Start() executed and tabs initialized.");
    }

    private bool _hasLoggedUpdate = false;

    private void Update()
    {
        if (!_hasLoggedUpdate)
        {
            AMGPlugin.Log.LogInfo("MenuUI Update() is running!");
            _hasLoggedUpdate = true;
        }

        // Check for the menu keybind toggle
        KeyCode toggleKey = KeyCode.Delete;
        
        if (AMGPlugin.MenuKeybind != null && !string.IsNullOrEmpty(AMGPlugin.MenuKeybind.Value))
        {
            if (Enum.TryParse<KeyCode>(AMGPlugin.MenuKeybind.Value, true, out var parsedKey))
            {
                toggleKey = parsedKey;
            }
        }

        if (Input.GetKeyDown(toggleKey))
        {
            IsGUIActive = !IsGUIActive;
            AMGPlugin.Log.LogInfo($"Menu toggled. IsGUIActive: {IsGUIActive}");
            
            if (IsGUIActive)
            {
                if (AMGPlugin.MenuOpenOnMouse != null && AMGPlugin.MenuOpenOnMouse.Value)
                {
                    Vector2 mousePosition = Input.mousePosition;
                    _windowRect.position = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
                }
                else if (_windowRect.x <= 0 || _windowRect.y <= 0)
                {
                    // Fallback to center if it was initialized too early with Screen.width = 0
                    _windowRect = new Rect(
                        Screen.width / 2f - windowWidth / 2f,
                        Screen.height / 2f - windowHeight / 2f,
                        windowWidth,
                        windowHeight
                    );
                }
            }
        }
    }

    private void OnGUI()
    {
        if (!IsGUIActive) return;

        GUI.skin.toggle.fontSize = GUI.skin.button.fontSize = GUI.skin.label.fontSize = 15;

        // Ensure we draw over everything
        GUI.depth = 0;

        _windowRect = GUI.Window(
            1337, // arbitrary ID
            _windowRect, 
            (GUI.WindowFunction)WindowFunction, 
            "AmongUsAIAgent - Mod Menu"
        );
    }

    public void WindowFunction(int windowID)
    {
        GUILayout.BeginHorizontal();

        // Left tab selector (15% width)
        GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.15f));
        for (var i = 0; i < _tabs.Count; i++)
        {
            Color standardColor = GUI.backgroundColor;

            if (_selectedTab == i)
            {
                // Highlight the selected tab
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            }

            if (GUILayout.Button(_tabs[i].Name, GUILayout.Height(35)))
            {
                _selectedTab = i;
            }

            GUI.backgroundColor = standardColor;
        }
        GUILayout.EndVertical();

        // Separator logic
        GUILayout.Space(10f);

        // Right tab content and controls (85% width)
        GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.85f - 20f));

        if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
        {
            // Draw title for current tab
            GUILayout.Label(_tabs[_selectedTab].Name, new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            });
            GUILayout.Space(10f);

            // Draw content of the current tab
            _tabs[_selectedTab].Draw();
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        // Make the window draggable
        GUI.DragWindow();
    }
}
