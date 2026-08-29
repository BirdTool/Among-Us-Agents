using System.Linq;
using AMG.AI.Tools;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        public SafeKillRpcEnums SafeKillNotExecute(byte targetId)
        {
            if (IsDead) return SafeKillRpcEnums.ERROR_AgentIsDead;
            if (IsCrewmate) return SafeKillRpcEnums.ERROR_AgentIsNotImpostor;
            
            if (Agent.killTimer > 0f || !KillCooldownManager.CanKill(Agent.PlayerId)) return SafeKillRpcEnums.ERROR_CooldownNotReady;

            var target = Utils.Players.GetPlayerByPlayerId(targetId);
            if (target == null) return SafeKillRpcEnums.ERROR_TargetDoesNotExist;
            if (targetId == Agent.PlayerId) return SafeKillRpcEnums.ERROR_TargetIsItSelf;
            if (target.Data.IsDead) return SafeKillRpcEnums.ERROR_TargetIsDead;
            if (target.Data.Role.IsImpostor) return SafeKillRpcEnums.ERROR_TargetIsImpostor;

            if (Utils.IsMeeting || Utils.IsExiling) return SafeKillRpcEnums.ERROR_IsMeeting;

            float[] nativeKillDistances = [1.0f, 1.8f, 2.5f];
            int killDistIndex = 1; 
            
            if (GameOptionsManager.Instance != null && GameOptionsManager.Instance.currentNormalGameOptions != null)
            {
                killDistIndex = GameOptionsManager.Instance.currentNormalGameOptions.KillDistance;
            }
            
            killDistIndex = Mathf.Clamp(killDistIndex, 0, 2); 
            float currentLobbyMaxDistance = nativeKillDistances[killDistIndex];

            float dist = Vector2.Distance(Vector2Position, target.transform.position);
            if (dist > currentLobbyMaxDistance) return SafeKillRpcEnums.ERROR_TargetTooFar;

            bool isProtected = target.protectedByGuardianId != -1 && target.protectedByGuardianId != 255;
            
            if (isProtected) return SafeKillRpcEnums.FAILED_AngelProtected;

            return SafeKillRpcEnums.SUCCESS;
        }

        public SafeKillRpcEnums SafeKill(byte targetId)
        {
            var result = SafeKillNotExecute(targetId);
            
            if (result != SafeKillRpcEnums.SUCCESS && result != SafeKillRpcEnums.FAILED_AngelProtected) 
            {
                if (result == SafeKillRpcEnums.FAILED_AngelProtected) KillCooldownManager.StartCooldownAsHalf(AgentId);
                return result;
            }

            bool didKillSucceed = result == SafeKillRpcEnums.SUCCESS;
            
            Agent.RpcMurderPlayer(Utils.Players.GetPlayerByPlayerId(targetId), didKillSucceed);

            if (didKillSucceed) KillCooldownManager.StartCooldown(AgentId);
            else if (result == SafeKillRpcEnums.FAILED_AngelProtected) KillCooldownManager.StartCooldownAsHalf(AgentId);

            return result;
        }

        public CloseDoorRoomEnums SafeCloseDoorNotExecute(SystemTypes doorRoom)
        {
            if (!IsImpostor) return CloseDoorRoomEnums.ERROR_AgentIsNotImpostor;
            
            var doorsInRoom = ShipStatus.Instance.AllDoors.Where(x => x.Room == doorRoom).ToList();
            if (doorsInRoom.Count == 0) return CloseDoorRoomEnums.ERROR_DoorDoesNotExist;

            if (doorsInRoom.All(x => !x.IsOpen)) return CloseDoorRoomEnums.ERROR_DoorIsAlreadyClosed;

            if (DoorCooldownTracker.GetRoomDoorCooldown(doorRoom) > 0f)
            {
                return CloseDoorRoomEnums.ERROR_DoorOnCooldown;
            }

            if (Utils.CurrentSabotage != null) return CloseDoorRoomEnums.ERROR_SabotageIsRunning;

            return CloseDoorRoomEnums.SUCCESS;
        }
        public CloseDoorRoomEnums SafeCloseDoor(SystemTypes doorRoom)
        {
            var result = SafeCloseDoorNotExecute(doorRoom);
            
            if (result != CloseDoorRoomEnums.SUCCESS) 
            {
                return result;
            }

            try { ShipStatus.Instance.RpcCloseDoorsOfType(doorRoom); } catch { }

            return CloseDoorRoomEnums.SUCCESS;
        }
    }
}