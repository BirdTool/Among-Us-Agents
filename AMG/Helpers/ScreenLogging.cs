using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Il2CppInterop.Runtime;

// IT DOES NOT WORK
// IT DOES NOT WORK
// IT DOES NOT WORK
// IT DOES NOT WORK
// IT DOES NOT WORK
// IT DOES NOT WORK
// IT DOES NOT WORK

namespace AMG.Helpers
{
    public static class ScreenLogging
    {
        private static ScreenLoggerBehavior _instance;

        public static void Log(string message)
        {
            if (_instance == null)
            {
                var go = new GameObject("AMG_ScreenLoggerUI");
                GameObject.DontDestroyOnLoad(go);
                _instance = go.AddComponent<ScreenLoggerBehavior>();
            }

            _instance.AddLog(message);
        }
    }

    public class ScreenLoggerBehavior : MonoBehaviour
    {
        public ScreenLoggerBehavior(IntPtr ptr) : base(ptr) { }

        private class LogItem
        {
            public GameObject Go;
            public TextMeshPro TextComp;
            public float Timer;
        }

        private readonly List<LogItem> _logs = new();

        private const int MaxLogs = 5;
        private const float LogDuration = 15f;
        private const float FadeDuration = 1.0f;
        private const float SpacingY = 0.35f;

        public void AddLog(string msg)
        {
            var hud = HudManager.Instance;
            if (hud == null) return;

            var go = new GameObject("LogEntry");
            go.transform.SetParent(hud.transform, false);
            go.layer = hud.gameObject.layer;

            var tmpro = go.AddComponent<TextMeshPro>();

            var templateText = hud.GetComponentInChildren<TextMeshPro>();

            if (templateText != null)
            {
                tmpro.font = templateText.font;
                tmpro.fontSharedMaterial = templateText.fontSharedMaterial;
            }
            else
            {
                var fonts = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TMP_FontAsset>());
                if (fonts != null && fonts.Length > 0)
                {
                    tmpro.font = fonts[0].Cast<TMP_FontAsset>();
                }
            }

            tmpro.text = msg;
            tmpro.richText = true;
            tmpro.fontSize = 1.5f;
            tmpro.alignment = TextAlignmentOptions.BottomLeft;
            tmpro.color = Color.white;
            tmpro.sortingOrder = 10000;

            go.transform.localScale = Vector3.zero;

            _logs.Add(new LogItem { Go = go, TextComp = tmpro, Timer = LogDuration });

            if (_logs.Count > MaxLogs)
            {
                Destroy(_logs[0].Go);
                _logs.RemoveAt(0);
            }
        }

        void Update()
        {
            var hud = HudManager.Instance;
            if (hud == null) return;

            Camera uiCam = null;
            foreach (var cam in Camera.allCameras)
            {
                if (cam.name == "UI Camera")
                {
                    uiCam = cam;
                    break;
                }
            }
            if (uiCam == null) return;

            float camHeight = uiCam.orthographicSize;
            float camWidth = camHeight * uiCam.aspect;

            Vector3 basePos = new Vector3(-camWidth + 0.2f, -camHeight + 0.2f, -1f);

            for (int i = _logs.Count - 1; i >= 0; i--)
            {
                var log = _logs[i];

                if (log.Go == null || log.TextComp == null)
                {
                    _logs.RemoveAt(i);
                    continue;
                }

                log.Timer -= Time.deltaTime;

                if (log.Timer <= 0)
                {
                    Destroy(log.Go);
                    _logs.RemoveAt(i);
                    continue;
                }

                if (log.Go.transform.parent != hud.transform)
                {
                    log.Go.transform.SetParent(hud.transform, false);
                    log.Go.layer = hud.gameObject.layer;
                }

                int stackIndex = _logs.Count - 1 - i;
                log.Go.transform.localPosition = basePos + new Vector3(0, stackIndex * SpacingY, 0);
                log.Go.transform.localRotation = Quaternion.identity;

                float lifeLived = LogDuration - log.Timer;
                if (lifeLived < 0.2f)
                {
                    log.Go.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, lifeLived / 0.2f);
                }
                else
                {
                    log.Go.transform.localScale = Vector3.one;
                }

                if (log.Timer < FadeDuration)
                {
                    float alpha = log.Timer / FadeDuration;
                    Color32 color = log.TextComp.color;
                    log.TextComp.color = new Color32(color.r, color.g, color.b, (byte)(255 * alpha));
                }
            }
        }
    }
}