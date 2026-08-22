using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private float _ventTimeInside = 0f;
        private Vent _currentVent = null;
        private CooldownTimer _ventActionTimer = new();

        private void UpdateInVent()
        {
            if (myAgent.inVent == false) 
            {
                SetState(AgentState.Calculating);
                return;
            }

            _ventTimeInside += Time.deltaTime;

            if (_currentVent == null)
            {
                _currentVent = VentManager.GetNearestVent(Vector2Position);
                if (_currentVent == null) return;
            }

            if (!_ventActionTimer.IsOver()) return;

            // Scenario 1: Target Room is specified (TravelVentPlan)
            if (TargetRoomForVent.HasValue && TargetRoomForVent.Value != _currentVent.GetRoom())
            {
                // We are traveling. Look for connected vents to move closer or explore
                var connectedVents = VentManager.GetConnectedVents(_currentVent);
                if (connectedVents.Count > 0)
                {
                    // For simplicity, pick a random connected vent to traverse the network
                    // In a more complex scenario, we would pathfind the vent network.
                    Vent nextVent = null;
                    foreach (var v in connectedVents)
                    {
                        if (v.GetRoom() == TargetRoomForVent.Value)
                        {
                            nextVent = v;
                            break;
                        }
                    }

                    if (nextVent == null) nextVent = connectedVents.GetRandomItemSecureOrDefault();

                    if (nextVent != null)
                    {
                        if (_currentVent.Left == nextVent) _currentVent.ClickLeft();
                        else if (_currentVent.Right == nextVent) _currentVent.ClickRight();
                        else if (_currentVent.Center == nextVent) _currentVent.ClickCenter();

                        _currentVent = nextVent;
                        _ventActionTimer.StartDelay(1.5f);
                        return;
                    }
                }
            }

            // Target room reached or no target room (Escape or Spy)
            if (AvoidWitnessesWhenVenting)
            {
                var safeVent = VentManager.GetSafeConnectedVent(_currentVent, myAgent);
                
                if (safeVent != null)
                {
                    if (safeVent != _currentVent)
                    {
                        // Travel to safe vent and wait to exit next frame
                        if (_currentVent.Left == safeVent) _currentVent.ClickLeft();
                        else if (_currentVent.Right == safeVent) _currentVent.ClickRight();
                        else if (_currentVent.Center == safeVent) _currentVent.ClickCenter();

                        _currentVent = safeVent;
                        _ventActionTimer.StartDelay(0.5f);
                        return;
                    }
                    else
                    {
                        // Current vent is safe!
                        if (_ventTimeInside > 2f) // Minimum time inside
                        {
                            ExitCurrentVent();
                        }
                    }
                }
                else
                {
                    // No safe vent found immediately. 
                    if (_ventTimeInside > 15f)
                    {
                        // Trapped too long, exit anyway or pick a random connected to keep searching
                        var connectedVents = VentManager.GetConnectedVents(_currentVent);
                        if (connectedVents.Count > 0)
                        {
                            var randomNext = connectedVents.GetRandomItemSecureOrDefault();
                            if (_currentVent.Left == randomNext) _currentVent.ClickLeft();
                            else if (_currentVent.Right == randomNext) _currentVent.ClickRight();
                            else if (_currentVent.Center == randomNext) _currentVent.ClickCenter();

                            _currentVent = randomNext;
                            _ventActionTimer.StartDelay(2.0f);
                        }
                        else
                        {
                             ExitCurrentVent();
                        }
                    }
                }
            }
            else
            {
                // Don't care about witnesses, just exit
                if (_ventTimeInside > 1f)
                {
                    ExitCurrentVent();
                }
            }
        }

        private void ExitCurrentVent()
        {
            if (_currentVent != null)
            {
                SafeExitVent(_currentVent);
                TargetRoomForVent = null;
                AvoidWitnessesWhenVenting = true;
                _ventTimeInside = 0f;
                _currentVent = null;
            }
        }
    }
}