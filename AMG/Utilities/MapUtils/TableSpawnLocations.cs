using System.Collections.Generic;
using AMG.AI.Mind.StructuredAgentBrain;
using UnityEngine;

namespace AMG.Utilities.MapUtils
{
    public static class TableSpawnLocations
    {
        public static readonly Dictionary<MapNames, List<Vector2>> SpawnPoints = new()
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

        public static List<Vector2> CurrentMapSpawnPoints => SpawnPoints.ContainsKey((MapNames)Utils.GetCurrentMapID()) ? SpawnPoints[(MapNames)Utils.GetCurrentMapID()] : [];
    
        public static void MakeAllAgentsSpawnAtTable(bool resetPath = false){
            var allBrains = Utils.GetAllAgentController();
            if (allBrains.Count == 0) return;
            var vectorList = CurrentMapSpawnPoints;
            
            var currentPositionIndex = 0;
            for (int i = 0; i < allBrains.Count; i++)
            {
                var currentBrain = allBrains[i];
                if (resetPath)
                {
                    currentBrain.ResetPath();
                    if (currentBrain is StructuredAgentBrain structuredBrain) structuredBrain.ResetDestinations();
                }
                
                var positionsSize = vectorList.Count;
                if (positionsSize <= currentPositionIndex)
                {
                    currentPositionIndex = 0;
                }
                var currentPosition = vectorList[currentPositionIndex];
                if (!currentBrain.IsDead) currentPositionIndex++;
                currentBrain.Agent.transform.position = new UnityEngine.Vector3(currentPosition.x, currentPosition.y, 0);
            }
        }
    }
}