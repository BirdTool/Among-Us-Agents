using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.SafeRpcEnums;
using AMG.Models;
using AMG.Models.Signals;
using AMG.Utilities;
using UnityEngine;

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

            // Killer can be the player a shapeshifted killer looks like, so the room is where the victim died.
            var room = target.transform.position.GetClosestNode().Room;

            if (killSignal.IsDisguised)
            {
                LogManager.LogInfo($"[KillSignal] {BaseName} saw #{murder.PlayerId} {murder.Data.PlayerName} kill #{id} {target.Data.PlayerName} (disguised killer; real killer: {(killSignal.RealKiller != null ? "#" + killSignal.RealKiller.PlayerId + " " + killSignal.RealKiller.Data.PlayerName : "not sent")})");
            }

            // Framed by a disguised killer that looks like this very agent: it knows it did not do it.
            if (murder.PlayerId != AgentId)
            {
                var murderMemory = GetOrCreateMemory(murder.PlayerId);
                murderMemory.IncreaseSuspiciusPercentage(100f);
                murderMemory.RegisterKillWitnessed(id, Time.time, room);
            }

            KnownBodyRoom = room;

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