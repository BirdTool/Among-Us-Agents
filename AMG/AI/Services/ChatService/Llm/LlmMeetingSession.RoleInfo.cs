using System;
using System.Collections.Generic;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmMeetingSession
    {
        private static readonly Dictionary<string, string> RoleDescriptions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["crewmate"] = "(crewmate role) Basically, the normal role. No special powers. The objective is to do all the tasks or identify and vote out all impostors before they kill too many crewmates.",
            ["impostor"] = "(impostor role) Your role is Impostor. You win by killing enough crewmates so the number of impostors equals the number of crewmates, or by a critical sabotage (O2 / Reactor) if not fixed in time. You can fake tasks, use sabotages, and lie. Don't reveal your role unless you have a concrete plan.",
            ["noisemaker"] = "(crewmate role) when killed (unless comms are sabotaged) it alerts everyone: everybody learns a noisemaker died and the direction of the body.",
            ["tracker"] = "(crewmate role) can track any player; on the minimap it sees where that player is right now, can see mini-teleports (kills) and whether the player used a vent.",
            ["scientist"] = "(crewmate role) doing tasks recharges the battery of its tablet. The tablet shows who is dead, who is alive and who left the game.",
            ["detective"] = "(crewmate role) can learn the exact place a player was when someone was killed. Example: if brown killed orange in storage, investigating brown shows brown was in storage. It fails when many players were in the body's room.",
            ["judge"] = "(crewmate role) after completing a share of its tasks (lobby setting, 50% by default) it can judge someone *during a meeting*. If the judged player is an impostor, the impostor leaves the meeting even if it was not the most voted. If the judged player is innocent, the judge leaves instead, again without a vote. Nobody can tell whom the judge picked unless the judge says it (\"i'll judge maroon\"), and what the judge says is not necessarily true.",
            ["engineer"] = "(crewmate role) can use vents. There is a cooldown between vent uses and a maximum time inside a vent. That is why players ask 'blue cd?' (`cd` = cooldown): a player who says they are Engineer should know that cooldown, so asking for it is a way to test the claim. It is **not proof** either way: an impostor can pretend, and the cooldown is visible in the lobby settings before the game but most players never look, which is why the question works as a technique. Typical chat: `im eng`, `am eng`, `who is eng`, `<player> cd?` (the test), and the claimed value as an answer (`cd is 5 sec`). **Careful:** `eng` right after a room word (`upper eng`, `lower eng`) is the *room* (Upper/Lower Engine), not the role.",
            ["guardian angel"] = "(dead crewmate role) a dead crewmate becomes an angel and can protect any player (cooldown). If the impostor tries to kill the protected player, a shield appears and the kill fails. Only the impostor and the angels know; the other players and the protected player do not. When an angel saves someone, the start of the next meeting says an angel saved someone this round (it does not say who).",
            ["influencer"] = "(dead crewmate role) (newest role): can send up to three abstract images. An image may be a hint, a warning of danger, or point at the impostor, depending on the receiver's interpretation. Most often the image indicates the impostor's colour (a purple bowling ball = purple).",
            ["shapeshifter"] = "(impostor role) slang `SS` Can turn into any player alive this round (a player who died in an earlier round cannot be copied; one who died this round can). There is a maximum duration after which it reverts (the host can make it unlimited).",
            ["phantom"] = "(impostor role) can turn invisible, for a limited time. Slang for this: `farted`, `poofed` because it releases particles when it goes invisible.",
            ["viper"] = "(impostor role) when it kills, the body dissolves after a host-defined time (for example 10 seconds). Nobody is told.",
        };

        private string GetRoleDescription(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                string myRole = _brain.Agent.Data?.Role?.Role.ToString() ?? "Crewmate";
                if (RoleDescriptions.TryGetValue(myRole, out string desc))
                    return $"Your Role ({myRole}): {desc}";
                return $"Your Role is {myRole}.";
            }

            string query = text.Trim();

            // exact match
            if (RoleDescriptions.TryGetValue(query, out string exactMatch))
                return exactMatch;

            // partial / substring match
            foreach (var kv in RoleDescriptions)
            {
                if (kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    query.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            return $"Unknown role '{query}'. Known roles: {string.Join(", ", RoleDescriptions.Keys)}.";
        }
    }
}
