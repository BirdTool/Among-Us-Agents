using System.Collections.Generic;

namespace AMG.AI.Services.MeetingService
{
    public class MeetingMemories
    {
        public class PlayerMeetingMemoriesClass(byte playerId)
        {
            public byte PlayerId { get; } = playerId;
            public float LastMessageTime { get; set; } = 0f;
            public float LastAccusationTime { get; set; } = 0f;
            public float FrequencyOfSaying { get; set; } = 0f; // Messages per meeting
            public float FrequencyOfAccusing { get; set; } = 0f; // Accusations per meeting
            public float AverageTimeToVote { get; set; } = 0f; // Average time taken to vote (starts at VotingTime, end at Exiling)
        }

        public List<PlayerMeetingMemoriesClass> PlayerMeetingMemories = []; // Shared by all the brains
    }
}