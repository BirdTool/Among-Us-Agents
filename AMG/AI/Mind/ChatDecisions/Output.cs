using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMG.AI.Navigation;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.ChatDecisions
{
    public class ChatOutput(AgentBrain brain)
    {
        private readonly float _startedAt = Time.time;
        private float SecondsSinceStart => Time.time - _startedAt;
        private readonly List<SendingMessageType> _alreadySentMessages = [];

        public List<PlayerControl> GetPlayersThatWereCaughtKilling()
        {
            var memories = brain.GetMemories().Where(memory => memory.SawKilling);
            if (!memories.Any()) return [];

            List<PlayerControl> playersThatWereCaughtKillingAlive = [];
            foreach (var memory in memories)
            {
                var player = Utils.Players.GetPlayerByPlayerId(memory.PlayerId);
                if (player != null && !player.Data.IsDead)
                {
                    playersThatWereCaughtKillingAlive.Add(player);
                }
            }

            return playersThatWereCaughtKillingAlive;
        }
        
        public SendingMessageType? Evaluate()
        {
            var playersThatWereCaughtKillingAlive = GetPlayersThatWereCaughtKilling();
            
            if (playersThatWereCaughtKillingAlive.Count > 0 && !_alreadySentMessages.Contains(SendingMessageType.SawKillingAffirming)) return SendingMessageType.SawKillingAffirming;
            if (brain.bodiesSeenDead.Count > 0 && !_alreadySentMessages.Contains(SendingMessageType.BodyLocationAffirming)) return SendingMessageType.BodyLocationAffirming;
            if (SecondsSinceStart < 5 && !_alreadySentMessages.Contains(SendingMessageType.BodyLocationQuestion) && !_alreadySentMessages.Contains(SendingMessageType.BodyLocationAffirming) && !_alreadySentMessages.Contains(SendingMessageType.SawKillingAffirming)) return SendingMessageType.BodyLocationQuestion;

            return null;
        }

        public string GetMessage(SendingMessageType messageType)
        {
            switch (messageType)
            {
                case SendingMessageType.SawKillingAffirming:
                    var playersThatWereCaughtKillingAlive = GetPlayersThatWereCaughtKilling();
                    
                    if (playersThatWereCaughtKillingAlive.Count > 0)
                    {
                        List<string> colourNames = [];
                        foreach (var memory in playersThatWereCaughtKillingAlive)
                        {
                            colourNames.Add(Utils.Colours.GetPlayerColor(Utils.Players.GetPlayerByPlayerId(memory.PlayerId)).ToString());
                        }
                        
                        StringBuilder message = new();
                        message.Append("I saw ");
                        for (int i = 0; i < colourNames.Count; i++)
                        {
                            message.Append(colourNames[i]);
                            if (i < colourNames.Count - 2)
                            {
                                message.Append(", ");
                            }
                            else if (i == colourNames.Count - 2)
                            {
                                message.Append(" and ");
                            }
                        }
                        message.Append(" killing");
                        return message.ToString();
                    }
                    break;
                case SendingMessageType.BodyLocationAffirming:
                    var bodies = brain.bodiesSeenDead;
                    if (bodies.Count == 0) return "I don't know where the body is.";

                    List<string> foundLocations = [];

                    foreach (var body in bodies)
                    {
                        var waypoint = body.Position.GetClosestNode(); 
                        if (waypoint == null) continue;

                        var room = waypoint.Room;
                        var bodyWasBy = false;

                        if (room == SystemTypes.Hallway)
                        {
                            bodyWasBy = true;
                            var closestRoom = SystemTypes.Hallway;
                            float minDistance = float.MaxValue;

                            foreach (var wp in WaypointManager.AllWaypoints)
                            {
                                if (wp.Room != SystemTypes.Hallway) 
                                {
                                    float dist = Vector2.Distance(body.Position, wp.Position);
                                    if (dist < minDistance)
                                    {
                                        minDistance = dist;
                                        closestRoom = wp.Room;
                                    }
                                }
                            }
                            
                            room = closestRoom;
                        }

                        string locationText = bodyWasBy ? $"near {room}" : $"in {room}";
                        
                        if (!foundLocations.Contains(locationText))
                        {
                            foundLocations.Add(locationText);
                        }
                    }

                    if (foundLocations.Count > 0)
                    {
                        if (foundLocations.Count == 1)
                        {
                            return $"The body was {foundLocations[0]}";
                        }
                        else
                        {
                            return $"The bodies were {string.Join(" and ", foundLocations)}"; 
                        }
                    }
                    
                    return "I don't remember where the body was.";
                case SendingMessageType.BodyLocationQuestion:
                    return "Where was the body?";
                default:
                    break;
            }

            return null;
        }

        public void MarkAsSent(SendingMessageType type)
        {
            if (!_alreadySentMessages.Contains(type))
            {
                _alreadySentMessages.Add(type);
            }
        }
    }

    public enum SendingMessageType
    {
        VoteQuestion, // who should i vote?
        VoteAffirming, // you should vote...
        WhereQuestion, // where was pink?
        WhereAffirming, // i was in...
        BodyLocationQuestion,  // where was the body?
        BodyLocationAffirming, // the body was in...
        WhoSawKillingQuestion, // who did you saw killing?
        SawKillingAffirming, // i saw cyan killing
        VentingQuestion, // who vented?
        VentingAffirming // rose vented!
    }
}