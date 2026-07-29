namespace AMG.Enums.SafeRpcEnums
{
    public enum ChatRpcEnums
    {
        ERROR_IsNotInMeeting, // ignore if the agent is dead
        ERROR_InCooldown,
        ERROR_ContentIsEmpty,
        ERROR_ContentIsBiggerThan100,
        SUCCESS
    }
}