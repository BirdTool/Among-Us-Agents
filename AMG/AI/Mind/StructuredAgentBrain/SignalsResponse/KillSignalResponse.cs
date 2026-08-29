using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.SafeRpcEnums;
using AMG.Models;
using AMG.Models.Signals;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        private void OnKillSignalReceived(KillSignal killSignal)
        {
            if (IsDead) return;

            sawABody = true;
            var murder = killSignal.Killer;
            var target = killSignal.Target;
            var id = target.PlayerId;

            var murderMemory = GetOrCreateMemory(murder.PlayerId);
            murderMemory.IncreaseSuspiciusPercentage(100f);
            murderMemory.SawKilling = true;

            var reactionTimer = new CooldownTimer();
            reactionTimer.StartDelay(ReactionTime + RandomizerExtensions.GetSecureRandomFloat(0, 1.2f));

            updateAction = new AgentUpdateAction(() =>
            {
                if (reactionTimer.IsRunning && !reactionTimer.Consume())
                {
                    return false;
                }

                var actionResult = SafeReportBody(id);

                if (actionResult == ReportDeadBodyRpcEnums.SUCCESS)
                {
                    return true;
                }

                if (actionResult == ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist)
                {
                    return false;
                }

                if (CurrentPath == null)
                {
                    var end = Pathfinder.GetClosestNode(target.transform.position);
                    var path = Pathfinder.FindPath(WaypointPosition, end, out float dist);
                    CommandGoToPath(path);
                }

                return false;
            })
            {
                ExecuteOnMeeting = false,
                DeleteOnMeeting = true,
                IsOnlyPredefinedAction = false,
            };
        }
    }
}