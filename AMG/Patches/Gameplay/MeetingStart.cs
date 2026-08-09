using HarmonyLib;
using AMG.Utilities;
using System.Collections.Generic;
using System.Numerics;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingStart
    {
        private static readonly Dictionary<MapNames, List<Vector2>> SpawnPoints = new()
        {
            { 
                MapNames.Skeld, [
                    new Vector2(-0.88981533f, 2.3614898f),
                    new Vector2(0.7460794f, 1.1460873f),
                    new Vector2(-0.81188965f, -0.17605028f),
                    new Vector2(-2.4433825f, 0.9951041f),
                    new Vector2(-2.0680761f, 1.9053919f),
                    new Vector2(0.21532157f, 1.9861684f),
                    new Vector2(0.39162004f, 0.2546291f),
                    new Vector2(-1.9079813f, 0.091001794f),
                    new Vector2(-1.726843f, 2.1716404f),
                    new Vector2(-0.122098714f, 2.1840334f),
                    new Vector2(0.6528221f, 1.5002148f),
                    new Vector2(0.5813107f, 0.6370896f),
                    new Vector2(-0.22113949f, -0.11228117f),
                    new Vector2(-1.2370925f, -0.18261504f),
                    new Vector2(-2.3678174f, 0.44894052f),
                    new Vector2(-2.4547808f, 1.4829628f)
                ]
            }
        };
        
        public static void Prefix()
        {
            if (!Utils.IsFreePlay) return;
            var allBrains = Utils.GetAllBrains();
            if (allBrains.Count == 0) return;
            var vectorList = SpawnPoints[(MapNames)Utils.GetCurrentMapID()];
            
            var currentPositionIndex = 0;
            for (int i = 0; i < allBrains.Count; i++)
            {
                var currentBrain = allBrains[i];
                
                var positionsSize = vectorList.Count;
                if (positionsSize <= currentPositionIndex)
                {
                    currentPositionIndex = 0;
                }
                var currentPosition = vectorList[currentPositionIndex];
                if (!currentBrain.IsDead) currentPositionIndex++;
                currentBrain.AgentControl.transform.position = new UnityEngine.Vector3(currentPosition.X, currentPosition.Y, 0);
            }
        }
    }
}