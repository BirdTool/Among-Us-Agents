using AMG.Enums.SafeRpcEnums;
using AmongUs.GameOptions;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        public UseVentRpcEnums SafeUseVentNotExecute(Vent vent)
        {
            if (IsDead) return UseVentRpcEnums.ERROR_AgentIsDead;

            var isEngineer = Agent.Data.Role.Role == RoleTypes.Engineer;

            if (!IsImpostor && !isEngineer) return UseVentRpcEnums.ERROR_AgentIsNotImpostorOrEngineer;

            if (vent == null) return UseVentRpcEnums.ERROR_VentDoesNotExist;
            if (UnityEngine.Vector2.Distance(Vector2Position, vent.transform.position) > 3f) return UseVentRpcEnums.ERROR_AgentIsTooFarFromVent;

            if (isEngineer)
            {
                var engineerRole = Agent.Data.Role.Cast<EngineerRole>();

                if (engineerRole.cooldownSecondsRemaining > 0f)
                {
                    return UseVentRpcEnums.ERROR_AgentIsInCooldown;
                }
            }

            return UseVentRpcEnums.SUCCESS;
        }

        public UseVentRpcEnums SafeUseVent(Vent vent)
        {
            var result = SafeUseVentNotExecute(vent);

            if (result != UseVentRpcEnums.SUCCESS)
            {
                return result;
            }

            Agent.MyPhysics.RpcEnterVent(vent.Id);
            Agent.inVent = true;

            if (Agent.Data.Role.Role == RoleTypes.Engineer)
            {
                var engineerRole = Agent.Data.Role.Cast<EngineerRole>();
                engineerRole.inVentTimeRemaining = 25f;
            }

            return result;
        }

        public VentingResultEnum SafeLeaveVentNotExecute()
        {
            if (IsDead) return VentingResultEnum.ERROR_AgentIsDead;

            if (!Agent.inVent) return VentingResultEnum.ERROR_AgentIsNotInVent;

            return VentingResultEnum.SUCCESS;
        }

        public VentingResultEnum SafeLeaveVent(Vent vent)
        {
            var result = SafeLeaveVentNotExecute();

            if (result != VentingResultEnum.SUCCESS)
            {
                return result;
            }

            Agent.MyPhysics.RpcExitVent(vent.Id);
            Agent.inVent = false;

            return result;
        }

        public VentingResultEnum SafeVentGoRight(Vent vent)
        {
            if (vent.Right == null) return VentingResultEnum.ERROR_NoVentInThatDirection;

            var exitResult = SafeLeaveVent(vent);
            if (exitResult != VentingResultEnum.SUCCESS) return exitResult;

            Agent.MyPhysics.RpcEnterVent(vent.Right.Id);

            return VentingResultEnum.SUCCESS;
        }

        public VentingResultEnum SafeVentGoLeft(Vent vent)
        {
            if (vent.Left == null) return VentingResultEnum.ERROR_NoVentInThatDirection;

            var exitResult = SafeLeaveVent(vent);
            if (exitResult != VentingResultEnum.SUCCESS) return exitResult;

            Agent.MyPhysics.RpcEnterVent(vent.Left.Id);

            return VentingResultEnum.SUCCESS;
        }

        public VentingResultEnum SafeVentGoCenter(Vent vent)
        {
            if (vent.Center == null) return VentingResultEnum.ERROR_NoVentInThatDirection;

            var exitResult = SafeLeaveVent(vent);
            if (exitResult != VentingResultEnum.SUCCESS) return exitResult;

            Agent.MyPhysics.RpcEnterVent(vent.Center.Id);

            return VentingResultEnum.SUCCESS;
        }
    }
}