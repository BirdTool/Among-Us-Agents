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

        public static void AddMessageToCurrentMeeting(byte playerId, string message)
        {
            if (MeetingData.Count == 0) return;
            MeetingData.Last().AddMessage(playerId, message);
        }

        public static MeetingTranscript GetCurrentMeetingTranscript() => MeetingData.Last();
        public static List<MeetingMessage> GetCurrentMeetingMessages() => MeetingData.Count == 0 ? [] : MeetingData.Last().GetMessages();
        public static string GetCurrentMeetingTranscriptString(byte? myId = null) => MeetingData.Count == 0 ? string.Empty : MeetingData.Last().Transcript(myId);

        public static void Reset() => MeetingData.Clear();
    }

    public class MeetingTranscript
    {
        private readonly List<MeetingMessage> Messages = [];
        private readonly float StartedAt = Time.time;

        public void AddMessage(byte playerId, string message) =>
            Messages.Add(new MeetingMessage(playerId, message, Time.time - StartedAt));

        public List<MeetingMessage> GetMessages() => Messages;

        public string Transcript(byte? myId = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Start of Meeting");
            sb.AppendLine($"================");

            foreach (var msg in Messages)
            {
                var player = msg.Player;
                var colourName = player.PlayerColour;

                sb.AppendLine($"[{msg.TimeSinceMeetingStarted:F2}s] (Id: {player.PlayerId}{(myId == player.PlayerId ? " YOU" : "")}) {player.PlayerName} - {colourName}: {msg.Message}");
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