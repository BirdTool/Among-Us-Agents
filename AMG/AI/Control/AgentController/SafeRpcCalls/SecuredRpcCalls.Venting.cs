using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        protected float? _timeSinceStartedVenting = null;
        protected const float VENTING_ANIMATION_DURATION = 0.6f;

        public UseVentRpcEnums SafeUseVentNotExecute(Vent vent)
        {
            LogManager.LogDebug($"[VENT-DEBUG] name={vent.name} id={vent.Id} pos={vent.transform.position}");
            if (IsDead) return UseVentRpcEnums.ERROR_AgentIsDead;

            var isEngineer = Agent.Data.Role.Role == RoleTypes.Engineer;

            if (!IsImpostor && !isEngineer) return UseVentRpcEnums.ERROR_AgentIsNotImpostorOrEngineer;

            if (vent == null) return UseVentRpcEnums.ERROR_VentDoesNotExist;
            if (UnityEngine.Vector2.Distance(Vector2Position, vent.transform.position) > 4.5f) return UseVentRpcEnums.ERROR_AgentIsTooFarFromVent;

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
            if (result != UseVentRpcEnums.SUCCESS) return result;

            Agent.transform.position = vent.transform.position;
            
            if (Agent.MyPhysics.body != null)
            {
                Agent.MyPhysics.body.position = vent.transform.position;
                Agent.MyPhysics.body.velocity = Vector2.zero; 
                DesiredVelocity = Vector2.zero;
            }
            Physics2D.SyncTransforms();
            
            Agent.NetTransform.Halt();

            Agent.MyPhysics.RpcEnterVent(vent.Id);

            Agent.inVent = true;
            _timeSinceStartedVenting = UnityEngine.Time.time;

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
            Agent.Collider?.enabled = true;

            return result;
        }

        public VentingResultEnum SafeVentGoRight(Vent vent)
        {
            if (vent.Right == null) return VentingResultEnum.ERROR_NoVentInThatDirection;

            Agent.transform.position = vent.Right.transform.position;
            Agent.MyPhysics.body?.position = vent.Right.transform.position;

            Agent.NetTransform.RpcSnapTo(vent.Right.transform.position);

            return VentingResultEnum.SUCCESS;
        }

        public VentingResultEnum SafeVentGoLeft(Vent vent)
        {
            if (vent.Left == null) return VentingResultEnum.ERROR_NoVentInThatDirection;

            Agent.transform.position = vent.Left.transform.position;
            Agent.MyPhysics.body?.position = vent.Left.transform.position;

            Agent.NetTransform.RpcSnapTo(vent.Left.transform.position);

            return VentingResultEnum.SUCCESS;
        }

        public VentingResultEnum SafeVentGoCenter(Vent vent)
        {
            if (vent.Center == null) return VentingResultEnum.ERROR_NoVentInThatDirection;

            Agent.transform.position = vent.Center.transform.position;
            Agent.MyPhysics.body?.position = vent.Center.transform.position;

            Agent.NetTransform.RpcSnapTo(vent.Center.transform.position);

            return VentingResultEnum.SUCCESS;
        }
    }
}