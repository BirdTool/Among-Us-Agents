using AMG.AI.Control;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Mind.Decisions.Sabotages
{
    public static class SabotageManager
    {
        public static void CheckSabotageStateChange()
        {
            var active = GetActiveManualSabotage();

            if (active != null && Utils.CurrentSabotage != null && active.GetType() != Utils.CurrentSabotage.GetType())
            {
                Utils.CurrentSabotage = null;
                Utils.OnSabotageEnded?.Invoke();
                foreach (var agent in AgentManager.Agents)
                {
                    var brain = agent.Control.GetComponent<AgentBrain>();
                    if (brain != null)
                    {
                        brain.currentSabotageStep = null;
                        brain.isGoingToFixASabotage = false;
                        brain.SetState(AgentState.Calculating);
                    }
                }
            }

            if (active != null && Utils.CurrentSabotage == null)
            {
                Utils.CurrentSabotage = active;
                Utils.OnSabotageStarted?.Invoke(active);
            }
            else if (active == null && Utils.CurrentSabotage != null)
            {
                Utils.CurrentSabotage = null;
                Utils.OnSabotageEnded?.Invoke();
                foreach (var agent in AgentManager.Agents)
                {
                    var brain = agent.Control.GetComponent<AgentBrain>();
                    if (brain != null)
                    {
                        brain.currentSabotageStep = null;
                        brain.isGoingToFixASabotage = false;
                        brain.SetState(AgentState.Calculating);
                    }
                }
            }
        }

        public static ISabotage GetActiveManualSabotage()
        {
            var ship = ShipStatus.Instance;
            if (ship == null) return null;

            if (Utils.Sabotages.SabotagesCount(ship) >= 2)
            {
                // There's a hacker among us
                Utils.Sabotages.RepairSabotages(ship);
                return null;
            }

            var map = (MapNames)Utils.GetCurrentMapID();

            return map switch
            {
                MapNames.Skeld => SkeldSabotageHandler(ship),
                _ => null,
            };
        }

        private static ISabotage SkeldSabotageHandler(ShipStatus ship)
        {
            if (Utils.Sabotages.IsReactorSabotaged(ship)) { LogManager.LogDebug("O Reator foi sabotado!"); return new SkeldReactorSabotage(); }
            if (Utils.Sabotages.IsOxygenSabotaged(ship)) { LogManager.LogDebug("O Oxigênio foi sabotado!"); return new SkeldO2Sabotage(); }
            if (Utils.Sabotages.IsElectricalSabotaged(ship)) { LogManager.LogDebug("As luzes foram sabotadas!"); return new SkeldLightsSabotage(); }
            if (Utils.Sabotages.IsCommsSabotaged(ship)) { LogManager.LogDebug("Os Comms foram sabotados!"); return new SkeldCommsSabotage(); }
            return null;
        }
    }
}