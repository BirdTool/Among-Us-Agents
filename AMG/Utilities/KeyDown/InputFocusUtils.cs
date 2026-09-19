namespace AMG.Utilities.KeyDown
{
    public static class InputFocusUtils
    {
        public static bool IsTypingInInputField()
        {
            return HudManager.InstanceExists
                && HudManager.Instance.Chat
                && HudManager.Instance.Chat.IsOpenOrOpening;
        }
    }
}