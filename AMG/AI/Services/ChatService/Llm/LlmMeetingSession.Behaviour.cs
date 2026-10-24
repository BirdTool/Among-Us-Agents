using System.Text;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService.Llm
{
    // How the agent talks: players are always referred to by colour, and the conversation rules that keep it calm.
    public sealed partial class LlmMeetingSession
    {
        // The game writes colours as "(Rose)": keep only the word.
        internal static string Colour(NetworkedPlayerInfo data)
        {
            string c = data?.ColorName?.Trim().Trim('(', ')').Trim();
            return string.IsNullOrEmpty(c) ? "Unknown" : c;
        }

        internal static string Colour(PlayerControl player) => Colour(player?.Data);

        internal static string ColourOf(byte playerId)
        {
            string c = Colour(Utils.Players.GetPlayerByPlayerId(playerId));
            return c == "Unknown" ? $"player {playerId}" : c;
        }

        private static string GetPlayerNames()
        {
            var sb = new StringBuilder("=== Player names (id, colour, name) ===\n");
            foreach (var p in GameData.Instance.AllPlayers)
                sb.AppendLine($"- {p.PlayerId} {Colour(p)}: {Clean(p.PlayerName)}");
            return sb.ToString();
        }

        private void AppendBehaviour(StringBuilder sb, TierProfile profile)
        {
            if (profile.Lobby) return;

            sb.AppendLine("Players are identified by COLOUR. Always call, ask and accuse players by colour, never by their name. Names are not shown: if someone writes a name you do not know, use get_player_names (when you have it) to find the colour, and do not guess. Commands still use the player id.");

            if (!profile.TrustChat)
            {
                sb.AppendLine("How to behave in the meeting:");
                sb.AppendLine("- Stay calm and keep it short. Speak only when you have something new: a fact you saw, an answer to someone, a claim, or your opinion for the vote. Otherwise use wait. Talk only about the meeting (who died, who is suspicious, votes), never about anything else.");
                sb.AppendLine("- Answer a question addressed to you before asking anything yourself.");
                sb.AppendLine("- Ask at most ONE question per message, and never ask the same thing twice. If nobody answers your question, drop it: do not repeat it and do not pressure anyone for an answer.");
                sb.AppendLine("- Do not ask where the body was if it was already said or if someone already asked. Where a body was found is only known by whoever saw it; a missing answer is not suspicious by itself.");
                sb.AppendLine("- Do not ask who was nearby, who was with someone, or what someone was doing there by default. Ask only when a story contradicts something you saw yourself or what another player said. When two players report the same body or confirm each other, believe them instead of interrogating either one.");
                sb.AppendLine("- A 'poof' is the Phantom's vanish ability (the Phantom is an impostor role). If someone says they saw a poof, do not ask for where or when: weigh it together with everything else against the colour that did it, remembering it can also be a joke or a mistake.");
                sb.AppendLine("- If you personally saw something, say it once, clearly, with the colour and the room, and do not repeat it afterwards.");
                sb.AppendLine();
            }
            else
                sb.AppendLine();
        }
    }
}
