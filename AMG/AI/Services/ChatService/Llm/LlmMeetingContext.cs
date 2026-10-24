using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    public static class LlmMeetingContext
    {
        private static PlayerControl _reporter;
        private static NetworkedPlayerInfo _target;
        private static float _capturedAt = -100f;
        private static bool _captured;

        // Text for the prompt; empty until a meeting was captured.
        public static string Description => Build();

        // true when the meeting was called with the emergency button (no body reported)
        public static bool IsEmergency => _captured && _target == null;

        public static void Capture(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            // ReportDeadBody can run more than once for the same meeting: a later call without a body
            // must not turn a body report into an emergency meeting.
            if (target == null && _target != null && Time.time - _capturedAt < 3f) return;

            _reporter = reporter;
            _target = target;
            _capturedAt = Time.time;
            _captured = true;
        }

        private static string Build()
        {
            if (!_captured) return string.Empty;

            // The meeting itself knows who called it: use that, so the description never disagrees
            // with the "Called by" line of the observations.
            var reporter = _reporter;
            var hud = MeetingHud.Instance;
            if (hud != null && hud.reporterId <= 250)
                reporter = Utils.Players.GetPlayerByPlayerId(hud.reporterId) ?? reporter;

            string who = reporter?.Data != null
                ? $"{LlmMeetingSession.Colour(reporter)} (ID {reporter.PlayerId})"
                : "an unknown player";

            return _target == null
                ? $"Emergency meeting: {who} pressed the emergency button. NO body was reported."
                : $"Body report: {who} reported the body of {LlmMeetingSession.Colour(_target)} (ID {_target.PlayerId}). Where the body was is only known by whoever saw it.";
        }
    }
}
