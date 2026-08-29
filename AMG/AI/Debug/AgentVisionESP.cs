using System;
using System.Collections.Generic;
using System.IO;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Enums.GameEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Debug
{
    public class AgentVisionESP(IntPtr ptr) : MonoBehaviour(ptr)
    {
        public static bool IsActive = false;

        public static readonly int[] LayersToTest = [(int)LayersEnum.Shadow, (int)LayersEnum.IlluminatedBlocking];

        private static readonly List<LineRenderer> _linePool = [];
        private static int _lineIndex = 0;
        private static readonly Vector2[] EightDirections =
        [
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            (Vector2.up + Vector2.right).normalized,
            (Vector2.up + Vector2.left).normalized,
            (Vector2.down + Vector2.right).normalized,
            (Vector2.down + Vector2.left).normalized,
        ];

        private static void DrawAllDirectionsDebug(Vector2 origin, float maxDistance, float agentRadius)
        {
            foreach (var dir in EightDirections)
            {
                DrawStraightPathDebug(origin, dir, maxDistance, agentRadius);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                IsActive = !IsActive;
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                DumpUnityLayersToFile();
            }
        }

        private void LateUpdate()
        {
            _lineIndex = 0;

            if (IsActive && Camera.main != null && PlayerControl.LocalPlayer != null)
            {
                var localPlayer = PlayerControl.LocalPlayer;
                // float agentRadius = GetAgentRadius(localPlayer);

                // DrawAllDirectionsDebug(localPlayer.GetTruePosition(), 10f, agentRadius);
                DrawVisionESP(localPlayer);
            }

            for (int i = _lineIndex; i < _linePool.Count; i++)
            {
                if (_linePool[i].gameObject.activeSelf)
                    _linePool[i].gameObject.SetActive(false);
            }
        }

        private static float GetAgentRadius(PlayerControl player)
        {
            var col = player.GetComponent<CircleCollider2D>();
            if (col == null) return 0.35f;

            return col.radius * player.transform.lossyScale.x;
        }

        private static int GetCustomLayerMask()
        {
            int mask = 0;
            foreach (int layer in LayersToTest)
            {
                mask |= (1 << layer);
            }
            return mask;
        }

        private static void DrawStraightPathDebug(Vector2 origin, Vector2 direction, float maxDistance, float agentRadius)
        {
            try
            {
                Vector2 rayOrigin = new(origin.x, origin.y + 0.1f);
                int movementMask = (1 << (int)LayersEnum.Ship)
                                  | (1 << (int)LayersEnum.Objects)
                                  | (1 << (int)LayersEnum.ShortObjects);

                var hit = CircleCastIgnoringTriggers(rayOrigin, agentRadius, direction, maxDistance, movementMask);
                float drawDist = hit?.distance ?? maxDistance;

                Vector2 end = new(rayOrigin.x + direction.x * drawDist, rayOrigin.y + direction.y * drawDist);
                DrawWorldLine(rayOrigin, end, Color.green, 0.01f);
            }
            catch (Exception e)
            {
                LogManager.LogError($"[DrawStraightPathDebug] : An Error Occured! : {e}");
            }
        }

        private static bool IsWithinScreenBounds(Vector2 viewerPos, Vector2 targetPos)
        {
            if (Camera.main == null) return true;

            float halfHeight = Camera.main.orthographicSize;
            float halfWidth = halfHeight * Camera.main.aspect;

            Vector2 delta = targetPos - viewerPos;
            return Mathf.Abs(delta.x) <= halfWidth && Mathf.Abs(delta.y) <= halfHeight;
        }

        private static RaycastHit2D? CircleCastIgnoringTriggers(Vector2 origin, float radius, Vector2 direction, float maxDistance, int mask)
        {
            var hits = Physics2D.CircleCastAll(origin, radius, direction, maxDistance, mask);

            RaycastHit2D? closest = null;
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (closest == null || h.distance < closest.Value.distance)
                    closest = h;
            }
            return closest;
        }

        private static void DrawVisionESP(PlayerControl localPlayer)
        {
            Vector2 myPos = localPlayer.GetTruePosition();
            Vector2 myEyes = new(myPos.x, myPos.y + 0.5f);

            int targetMask = GetCustomLayerMask();
            float visionRadius = GetLocalVisionRadius(localPlayer);

            foreach (var target in PlayerControl.AllPlayerControls)
            {
                if (target == localPlayer || target.Data.IsDead) continue;
                if (IsBlockedByClosedDoor(localPlayer, target)) continue;

                Vector2 targetPos = target.GetTruePosition();
                Vector2 targetEyes = new(targetPos.x, targetPos.y + 0.5f);
                Vector2 boxCenter = new(targetPos.x, targetPos.y + 0.35f);

                float dist = Vector2.Distance(myEyes, targetEyes);
                if (dist > visionRadius) continue;

                if (!IsWithinScreenBounds(myPos, targetPos)) continue;

                Vector2 dir = (targetEyes - myEyes).normalized;
                var hit = RaycastIgnoringTriggers(myEyes, dir, dist, targetMask);

                if (hit != null) continue;

                Color c = GetSuspicionColor(null, target);
                DrawWorldLine(myEyes, targetEyes, c, 0.01f);
                DrawWorldBox(boxCenter, 0.3f, 0.45f, c, 0.01f);
            }
        }
        private static bool IsBlockedByClosedDoor(PlayerControl viewer, PlayerControl target)
        {
            var viewerRoom = Utils.GetPlayerRoom(viewer);
            var targetRoom = Utils.GetPlayerRoom(target);

            if (viewerRoom == targetRoom) return false;

            return Utils.IsRoomClosed(viewerRoom) || Utils.IsRoomClosed(targetRoom);
        }

        private static RaycastHit2D? RaycastIgnoringTriggers(Vector2 origin, Vector2 direction, float maxDistance, int mask)
        {
            var hits = Physics2D.RaycastAll(origin, direction, maxDistance, mask);

            RaycastHit2D? closest = null;
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (closest == null || h.distance < closest.Value.distance)
                    closest = h;
            }
            return closest;
        }

        private static float GetLocalVisionRadius(PlayerControl player)
        {
            if (player.lightSource != null)
                return player.lightSource.viewDistance;

            return 5f;
        }

        private static Color GetSuspicionColor(StructuredAgentBrain brain, PlayerControl target)
        {
            return Color.red;
        }

        private static void DrawWorldLine(Vector2 worldA, Vector2 worldB, Color color, float thickness)
        {
            var line = GetAvailableLine();
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(worldA.x, worldA.y, -10f));
            line.SetPosition(1, new Vector3(worldB.x, worldB.y, -10f));
            line.startColor = color;
            line.endColor = color;
            line.startWidth = thickness;
            line.endWidth = thickness;
        }

        private static void DrawWorldBox(Vector2 center, float halfWidth, float halfHeight, Color color, float thickness)
        {
            var line = GetAvailableLine();
            line.positionCount = 5;
            float z = -10f;

            Vector3 tl = new(center.x - halfWidth, center.y + halfHeight, z);
            Vector3 tr = new(center.x + halfWidth, center.y + halfHeight, z);
            Vector3 br = new(center.x + halfWidth, center.y - halfHeight, z);
            Vector3 bl = new(center.x - halfWidth, center.y - halfHeight, z);

            line.SetPosition(0, tl);
            line.SetPosition(1, tr);
            line.SetPosition(2, br);
            line.SetPosition(3, bl);
            line.SetPosition(4, tl);

            line.startColor = color;
            line.endColor = color;
            line.startWidth = thickness;
            line.endWidth = thickness;
        }

        private static LineRenderer GetAvailableLine()
        {
            if (_lineIndex >= _linePool.Count)
            {
                var go = new GameObject($"ESP_Line_{_lineIndex}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                var lr = go.AddComponent<LineRenderer>();
                lr.material = DestroyableSingleton<HatManager>.Instance.PlayerMaterial;
                _linePool.Add(lr);
            }

            var currentLine = _linePool[_lineIndex++];
            currentLine.gameObject.SetActive(true);
            return currentLine;
        }

        private static void DumpUnityLayersToFile()
        {
            try
            {
                string path = Path.Combine(Environment.CurrentDirectory, "AmongUs_Layers.txt");
                using StreamWriter writer = new(path);

                writer.WriteLine("=== MAPA DE LAYERS DO AMONG US ===");
                for (int i = 0; i < 32; i++)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        writer.WriteLine($"Layer {i}: {layerName}");
                    }
                }
                LogManager.LogDebug($"[ESP] Camadas exportadas com sucesso em: {path}");
            }
            catch (Exception e)
            {
                LogManager.LogError($"[ESP] Falha ao extrair layers: {e}");
            }
        }
    }
}