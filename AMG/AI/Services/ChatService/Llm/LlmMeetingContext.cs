namespace AMG.AI.Services.ChatService.Llm
{
    public static class LlmMeetingContext
    {
        public static string Description { get; private set; } = "";

        // true when the meeting was called with the emergency button (no body reported)
        public static bool IsEmergency { get; private set; }

        public static void Capture(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            string who = reporter?.Data != null
                ? $"{LlmMeetingSession.Colour(reporter)} (ID {reporter.PlayerId})"
                : "an unknown player";

            IsEmergency = target == null;

            Description = target == null
                ? $"Emergency meeting: {who} pressed the emergency button. NO body was reported."
                : $"Body report: {who} reported the body of {LlmMeetingSession.Colour(target)} (ID {target.PlayerId}). Where the body was is only known by whoever saw it.";
        }
    }
}
