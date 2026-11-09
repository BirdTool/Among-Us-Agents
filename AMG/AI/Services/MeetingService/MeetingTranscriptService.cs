using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.MeetingService
{
    public static class MeetingTranscriptService
    {
        private static readonly List<MeetingTranscript> MeetingData = [];

        public static void AddNewMeetingData() => MeetingData.Add(new MeetingTranscript());

        public static void AddMessageToCurrentMeeting(byte playerId, string message) =>
            MeetingData.Last().AddMessage(playerId, message);

        public static MeetingTranscript GetCurrentMeetingTranscript() => MeetingData.Last();
        public static string GetCurrentMeetingTranscriptString() => MeetingData.Last().Transcript();

        public static void Reset() => MeetingData.Clear();
    }

    public class MeetingTranscript
    {
        private readonly List<MeetingMessage> Messages = [];
        private readonly float StartedAt = Time.time;

        public void AddMessage(byte playerId, string message) =>
            Messages.Add(new MeetingMessage(playerId, message, Time.time - StartedAt));

        public List<MeetingMessage> GetMessages() => Messages;

        public string Transcript()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Start of Meeting");
            sb.AppendLine($"================");

            foreach (var msg in Messages)
            {
                var player = msg.Player;
                var colourName = player.PlayerColour;

                sb.AppendLine($"[{msg.TimeSinceMeetingStarted:F2}s] (Id: {player.PlayerId}) {player.PlayerName} - {colourName}: {msg.Message}");
            }

            return sb.ToString();
        }
    }

    public class MeetingMessage(byte playerId, string message, float timeSinceMeetingStarted)
    {
        public byte PlayerId { get; set; } = playerId;
        public string Message { get; set; } = message;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public float TimeSinceMeetingStarted { get; set; } = timeSinceMeetingStarted;

        public AMGPlayerData Player => AMGPlayers.GetPlayer(PlayerId);
    }
}