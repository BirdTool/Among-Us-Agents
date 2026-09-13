using System;
using System.Collections.Generic;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Models.Plans;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain.Plans
{
    public class AgentPlanManager(StructuredAgentBrain brain)
    {
        private readonly StructuredAgentBrain _brain = brain;
        private bool _setCalculatingAtEnd = false;
        private Func<bool> _functionAfterFalseCheck = null;
        private bool _alreadylogged = false;

        public void SetCalculatingAtEnd(bool value)
        {
            _setCalculatingAtEnd = value;
        }

        public readonly List<IPlan> QueuePlans = [];

        public void AddPlan(IPlan plan)
        {
            QueuePlans.Add(plan);
        }

        public void ClearPlans()
        {
            QueuePlans.Clear();
        }

        public void Execute()
        {
            if (QueuePlans.Count == 0) return;

            _brain.updateAction = new Models.AgentUpdateAction(() =>
            {
                try
                {
                    if (_functionAfterFalseCheck != null)
                    {
                        LogManager.LogDebug("[planmanager-debug] function after false check is not null");
                        if (_functionAfterFalseCheck())
                        {
                            LogManager.LogDebug("[planmanager-debug] function after false check returned true");
                            QueuePlans.Clear();
                            _brain.SetState(AgentState.Calculating);
                            return true;
                        }
                        else
                        {
                            LogManager.LogDebug("[planmanager-debug] function after false check returned false");
                            return false;
                        }
                    }

                    if (QueuePlans.Count == 0)
                    {
                        LogManager.LogDebug("[planmanager-debug] queue is empty");
                        if (_setCalculatingAtEnd) _brain.SetState(AgentState.Calculating);
                        return true;
                    }

                    var plan = QueuePlans[0];
                    
                    if (!_alreadylogged)
                    {
                        LogManager.LogInfo($"[planmanager-debug] executing plan: {plan.Name}");
                        _alreadylogged = true;
                    }
                    
                    if (plan is CheckPlan checkPlan)
                    {
                        _alreadylogged = false;
                        LogManager.LogDebug("[planmanager-debug] executing checkplan plan");
                        if (checkPlan.CheckIfPassed())
                        {
                            LogManager.LogDebug("[planmanager-debug] checkplan plan passed");
                            QueuePlans.RemoveAt(0);
                        }
                        else
                        {
                            LogManager.LogDebug("[planmanager-debug] checkplan plan failed");
                            if (checkPlan._actionAfterCheckIsFalse != null)
                            {
                                LogManager.LogDebug("[planmanager-debug] checkplan plan has action after false check");
                                _functionAfterFalseCheck = checkPlan._actionAfterCheckIsFalse;
                                QueuePlans.RemoveAt(0);
                                return false;
                            }
                            QueuePlans.Clear();
                            _brain.SetState(AgentState.Calculating);
                            return true;
                        }
                        return false;
                    }
                    plan.Execute(_brain);

                    if (plan.IsDone)
                    {
                        _alreadylogged = false;
                        LogManager.LogDebug("[planmanager-debug] plan is done");
                        QueuePlans.RemoveAt(0);
                    }

                    return false;
                }
                catch (Exception e)
                {
                    LogManager.LogError($"An Error occurred in PlanManager: {e}");
                    QueuePlans.Clear();
                    _brain.SetState(AgentState.Calculating);
                    return true;
                }

            });
        }
    }
}
