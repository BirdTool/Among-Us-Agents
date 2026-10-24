using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    public static class LlmMeetingContext
    {
        private static PlayerControl _reporter;
        private static NetworkedPlayerInfo _target;
        private static float _capturedAt = -100f;
        private static float _meetingStartedAt = -100f;
        private static bool _captured;

        // What was captured belongs to THIS meeting only if it happened right before the meeting started.
        // Anything older is left over from a previous meeting and must not be used.
        private static bool Fresh =>
            _captured && _capturedAt >= _meetingStartedAt - 8f && _capturedAt <= _meetingStartedAt + 2f;

        // Text for the prompt; empty when nothing is known.
        public static string Description => Build();

        // true only when it is known that the meeting was called with the emergency button (no body reported)
        public static bool IsEmergency => Fresh && _target == null;

        public static void MeetingStarted() => _meetingStartedAt = Time.time;

        public static void Clear()
        {
            _captured = false;
            _reporter = null;
            _target = null;
        }

        public static void Capture(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            LogManager.LogDebug($"[Meeting] ReportDeadBody seen: reporter={LlmMeetingSession.Colour(reporter)}, "
                + (target == null ? "target=none (emergency button)" : $"target={LlmMeetingSession.Colour(target)}"));

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
            // The meeting itself knows who called it: use that, so the description never disagrees
            // with the "Called by" line of the observations.
            var hud = MeetingHud.Instance;
            PlayerControl caller = hud != null && hud.reporterId <= 250
                ? Utils.Players.GetPlayerByPlayerId(hud.reporterId)
                : null;

            if (!Fresh)
            {
                if (caller?.Data == null) return string.Empty;
                return $"A meeting was called by {LlmMeetingSession.Colour(caller)} (ID {caller.PlayerId}). You do NOT know whether it was the emergency button or a body report: do not claim either one.";
            }

            var reporter = caller ?? _reporter;
            string who = reporter?.Data != null
                ? $"{LlmMeetingSession.Colour(reporter)} (ID {reporter.PlayerId})"
                : "an unknown player";

            return _target == null
                ? $"Emergency meeting: {who} pressed the emergency button. NO body was reported."
                : $"Body report: {who} reported the body of {LlmMeetingSession.Colour(_target)} (ID {_target.PlayerId}). Where the body was is only known by whoever saw it.";
        }
    }
}
