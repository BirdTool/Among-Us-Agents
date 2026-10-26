namespace AMG.Enums.ChatServiceEnums
{
    public enum ChatContextEnum
    {
        Lobby,            // before the match starts
        Roaming,          // during a round, outside meetings
        MeetingStart,     // first seconds of a meeting (immediate reactions)
        MeetingFollowUp,  // meeting in progress, after the first reactions
        Voting            // end of the meeting, vote phase
    }
}