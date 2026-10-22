using System.Collections.Generic;
using System.Text;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmMeetingSession
    {
        private string GetMeetingObservations()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Meeting Observations ===");
            if (!string.IsNullOrEmpty(LlmMeetingContext.Description)) sb.AppendLine(LlmMeetingContext.Description);

            if (MeetingHud.Instance != null)
            {
                byte reporterId = MeetingHud.Instance.reporterId;
                if (reporterId <= 250)
                {
                    var rep = Utils.Players.GetPlayerByPlayerId(reporterId);
                    string repName = rep != null
                        ? $"{rep.name} (Colour: {rep.Data?.ColorName ?? "Unknown"})"
                        : $"Player {reporterId}";
                    sb.AppendLine($"Called by: {repName} (ID: {reporterId})");
                }
                else
                {
                    sb.AppendLine("Called by: Emergency Button / System");
                }

                sb.AppendLine($"Meeting State: {(Utils.IsMeetingVoting ? "Voting Open" : "Discussion Time")}");
            }

            if (GameData.Instance != null)
                sb.AppendLine($"Tasks: {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks} completed");

            if (_brain is not StructuredAgentBrain brain) return sb.ToString();

            sb.AppendLine($"You personally saw a body this round: {brain.sawABody}");
            if (brain.bodiesSeenDead.Count > 0)
            {
                sb.AppendLine("Dead Bodies Seen by You:");
                foreach (var body in brain.bodiesSeenDead)
                {
                    var victim = Utils.Players.GetPlayerByPlayerId(body.PlayerId);
                    string victimName = victim != null
                        ? $"{victim.name} (Colour: {victim.Data?.ColorName ?? "Unknown"})"
                        : $"Player {body.PlayerId}";
                    var room = AgentVision.GetRoomAtPosition(body.Position);
                    sb.AppendLine($"  - {victimName} (ID: {body.PlayerId}), Room: {room}, ~{body.TimeSinceDeath:0.#}s ago");
                }
            }

            if (brain.KnownBodyRoom.HasValue)
                sb.AppendLine($"Known Body Room: {brain.KnownBodyRoom.Value}");

            var crimes = new List<string>();
            foreach (var m in brain.GetMemories())
            {
                if (m.PlayerId == brain.AgentId) continue;
                var pl = Utils.Players.GetPlayerByPlayerId(m.PlayerId);
                string plName = pl != null ? $"{pl.name} (ID: {m.PlayerId})" : $"Player {m.PlayerId}";

                if (m.SawKilling)
                {
                    string victim = m.LastVictimSeenKilled.HasValue
                        ? Utils.Players.GetPlayerByPlayerId(m.LastVictimSeenKilled.Value)?.name ?? $"ID {m.LastVictimSeenKilled.Value}"
                        : "Unknown";
                    crimes.Add($"[CRITICAL] KILL: {plName} killed {victim} in {m.KillRoom} at {m.TimeOfKillWitnessed:0.#}s!");
                }
                if (m.SawVenting)
                    crimes.Add($"[CRITICAL] VENT: {plName} vented in {m.VentRoom}!");
                if (m.SeenFakeTask)
                    crimes.Add($"[SUSPICIOUS] FAKE TASK: {plName} faked a task!");
                if (m.SuspectedOfLongTaskDwell)
                    crimes.Add($"[SUSPICIOUS] LONG DWELL: {plName} lingered at {m.LongDwellTaskType} for {m.LongDwellDuration:0.#}s.");
                if (m.SeenVerifiedTask)
                    crimes.Add($"[CLEAR] VERIFIED TASK: {plName} completed a verified task at {m.LastVerifiedTaskTime:0.#}s.");
            }

            if (crimes.Count > 0)
            {
                sb.AppendLine("Eyewitness Incidents:");
                foreach (var c in crimes) sb.AppendLine($"  - {c}");
            }
            else
            {
                sb.AppendLine("Eyewitness Incidents: None directly witnessed.");
            }

            sb.AppendLine($"Your state before meeting: {brain.currentState}");
            if (brain.currentLocalTask != null)
            {
                string taskName = brain.currentLocalTask.Task?.TaskType.ToString() ?? "Task";
                string roomName = brain.currentLocalTask.Task?.StartAt.ToString()
                    ?? AgentVision.GetRoomAtPosition(brain.currentLocalTask.Position).ToString();
                sb.AppendLine($"Your task before meeting: {taskName} in {roomName}");
            }
            if (brain.isGoingToFixASabotage)
                sb.AppendLine("You were heading to fix a sabotage.");

            return sb.ToString();
        }
    }
}
