using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AMG.AI.Services.ChatService.Llm
{
    // What the agent does and does not say when its role is Impostor.
    public sealed partial class LlmMeetingSession
    {
        // Another player must have said this about a partner before the agent may stop protecting him.
        private const int WitnessesToAbandonPartner = 2;

        private static readonly Regex KillAccusationRegex = new(@"\b(?:kill(?:ed|ing|s|er)?|murder(?:ed)?|stab(?:bed)?)\b", Flags);
        private static readonly Regex DefenceRegex = new(@"\b(?:not|isn['’]?t|no proof|no evidence|cleared|innocent|safe|clean|doubt|weak)\b", Flags);

        private bool AmImpostor => _brain.Agent?.Data?.Role != null && _brain.Agent.Data.Role.IsImpostor;

        // The other impostors of this match (empty when the agent is not an impostor).
        private List<byte> FellowImpostorIds()
        {
            var ids = new List<byte>();
            if (!AmImpostor) return ids;

            foreach (var p in GameData.Instance.AllPlayers)
                if (p.PlayerId != _brain.AgentId && p.Role != null && p.Role.IsImpostor)
                    ids.Add(p.PlayerId);

            return ids;
        }

        private static bool IsOut(byte id)
        {
            foreach (var p in GameData.Instance.AllPlayers)
                if (p.PlayerId == id) return p.IsDead || p.Disconnected;
            return true;
        }

        // A partner is "busted" when other players (not the agent, not the partner) already say they saw him kill:
        // at that point defending him only exposes the agent too.
        private bool IsBusted(byte partnerId)
        {
            string colour = Regex.Escape(ColourOf(partnerId));
            var witnesses = new HashSet<byte>();

            foreach (var e in LlmMeetingTranscript.All)
            {
                if (e.SenderId == _brain.AgentId || e.SenderId == partnerId || string.IsNullOrEmpty(e.Text)) continue;
                if (!Regex.IsMatch(e.Text, $@"\b{colour}\b", Flags) || !KillAccusationRegex.IsMatch(e.Text)) continue;
                witnesses.Add(e.SenderId);
            }

            return witnesses.Count >= WitnessesToAbandonPartner;
        }

        // The impostor never accuses a partner (unless he is busted): the crew would catch both of them.
        private string FindAccusedPartner(string text)
        {
            if (!AmImpostor || DefenceRegex.IsMatch(text)) return null;

            foreach (var id in FellowImpostorIds())
            {
                if (IsBusted(id)) continue;

                string colour = Regex.Escape(ColourOf(id));
                string words = $"{DeadAccuseWords}|saw|seen|kill\\w*";
                string pattern = $@"\b{colour}\b[^.!?]{{0,30}}\b(?:{words})\b|\b(?:{words})\b[^.!?]{{0,30}}\b{colour}\b";
                if (Regex.IsMatch(text, pattern, Flags))
                    return $"error: {ColourOf(id)} is your fellow impostor. Never accuse or doubt a partner: the crew would catch you both. Put the suspicion on a crewmate, or say nothing.";
            }

            return null;
        }

        private void AppendImpostorPlaybook(StringBuilder sb, TierProfile profile)
        {
            if (profile.Lobby || !AmImpostor) return;

            var partners = FellowImpostorIds();

            sb.AppendLine("=== You are an IMPOSTOR: play it like one ===");
            sb.AppendLine(partners.Count == 0
                ? "You are the only impostor."
                : "Your fellow impostors (your partners, confirmed): "
                  + string.Join(", ", partners.Select(id => ColourOf(id) + (IsOut(id) ? " (out of the game)" : string.Empty))) + ".");
            sb.AppendLine("- You know who the impostors are; the crewmates do not. Never reveal your role or your partners, and never say \"we\" about the impostors.");
            sb.AppendLine("- Act like an innocent crewmate: react to the body or the button as an innocent would, and when someone asks where you were, give a simple, plausible alibi (a room, a task) and keep it consistent with what you already said.");
            sb.AppendLine("- NEVER accuse, suspect, blame or vote for a fellow impostor, not even to look fair. If someone accuses a partner without real proof, you may calmly say the accusation looks weak or ask for proof, but do not defend him too hard: that exposes you both.");
            sb.AppendLine($"- The only exception: at least {WitnessesToAbandonPartner} other players already say they saw that partner kill. Then he is lost: stop defending him and, if the group is clearly voting him out, vote with the group to keep your cover.");
            sb.AppendLine("- Put the suspicion on CREWMATES: pick someone already under suspicion or with a weak story, and give a believable reason from the chat or your notes. Never claim you saw a kill or a vent that your notes do not show.");
            sb.AppendLine("- When voting: join the group on a crewmate who is already the main suspect; with no consensus, skip rather than vote for a partner.");
            sb.AppendLine("- Never share your real knowledge of your own kills or your partners' kills and vents; do not help the crew find the impostors.");
            sb.AppendLine();
        }
    }
}
