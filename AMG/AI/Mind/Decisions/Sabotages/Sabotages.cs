using AMG.AI.Navigation;
using AMG.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind.Decisions.Sabotages
{
    public abstract class ManualSabotageStep : SabotageSteps
    {
        private List<Waypoint> _locations = [];
        public override List<Waypoint> Locations => _locations;

        public ManualSabotageStep(params Vector2[] rawPositions)
        {
            foreach (var pos in rawPositions)
            {
                var closest = GetGuaranteedClosestNode(pos);
                if (closest != null) _locations.Add(closest);
            }
        }

        private Waypoint GetGuaranteedClosestNode(Vector2 targetPos)
        {
            Waypoint closest = null;
            float minDistance = float.MaxValue;
            foreach (var wp in WaypointManager.AllWaypoints)
            {
                float dist = Vector2.Distance(targetPos, wp.Position);
                if (dist < minDistance) { minDistance = dist; closest = wp; }
            }
            return closest;
        }
    }


    public class ReactorSabotageStep : ManualSabotageStep
    {
        private byte _consoleId; // 0 = Up, 1 = Down
        public ReactorSabotageStep(byte consoleId, Vector2 pos) : base(pos) { _consoleId = consoleId; }

        public override void CompleteStep(AgentBrain brain)
        {
            if (ShipStatus.Instance != null)
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, _consoleId);
        }
    }

    public class O2SabotageStep : ManualSabotageStep
    {
        private byte _consoleId;
        public O2SabotageStep(byte consoleId, Vector2 pos) : base(pos) { _consoleId = consoleId; }

        public override void CompleteStep(AgentBrain brain)
        {
            if (ShipStatus.Instance != null)
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, _consoleId);
        }
    }

    public class LightsSabotageStep : ManualSabotageStep
    {
        public LightsSabotageStep(Vector2 pos) : base(pos) { }

        public override void CompleteStep(AgentBrain brain)
        {
            if (ShipStatus.Instance == null) return;

            var elecSys = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
            for (byte i = 0; i < 5; i++)
            {
                var switchMask = 1 << (i & 0x1F);

                if ((elecSys.ActualSwitches & switchMask) != (elecSys.ExpectedSwitches & switchMask))
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, i);

                    break;
                }
            }
        }
    }

    public class CommsSabotageStep : ManualSabotageStep
    {
        public CommsSabotageStep(Vector2 pos) : base(pos) { }

        public override void CompleteStep(AgentBrain brain)
        {
            if (ShipStatus.Instance != null)
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 16);
        }
    }

    public class SkeldReactorSabotage : ISabotage
    {
        public bool StepComplete => true;
        public List<SabotageSteps> GetSteps() =>
        [
            new ReactorSabotageStep(0, new Vector2(-14.3f, -5.2f)),
            new ReactorSabotageStep(1, new Vector2(-14.3f, -7.4f))
        ];
    }

    public class SkeldO2Sabotage : ISabotage
    {
        public bool StepComplete => false;
        public List<SabotageSteps> GetSteps() =>
        [
            new O2SabotageStep(0, new Vector2(6.2f, -7.1f)),
            new O2SabotageStep(1, new Vector2(6.5f, -2.5f))
        ];
    }

    public class SkeldLightsSabotage : ISabotage
    {
        public bool StepComplete => false;
        public List<SabotageSteps> GetSteps() => [new LightsSabotageStep(new Vector2(-7.2f, -8.3f))];
    }

    public class SkeldCommsSabotage : ISabotage
    {
        public bool StepComplete => false;
        public List<SabotageSteps> GetSteps() => [new CommsSabotageStep(new Vector2(4.3f, -15.5f))];
    }
}