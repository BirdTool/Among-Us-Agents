using AMG.AI.Mind.LLMAgentBrain;
using AMG.Interfaces;
using AMG.Models.LLMAssistence.Crewmate;
using AMG.Models.LLMAssistence.Impostor;
using AmongUs.GameOptions;

namespace AMG.Models.LLMAssistence
{
    public class LLMDefaultAssistence(LLMAgentBrain brain) : ILLMAssistence
    {
        public LLMAgentBrain Brain { get; } = brain;

        public virtual void Update()
        {
            RoleUpdate();
        }

        protected virtual void RoleUpdate() {  }

        public virtual string Guidance(IGuidance guidance)
        {
            return "";
        }

        public static ILLMAssistence GetAssistenceByRole(RoleTypes role, LLMAgentBrain brain)
        {
            return role switch
            {
                RoleTypes.Detective => new DetectiveAssistence(brain),
                RoleTypes.Engineer => new EngineerAssistence(brain),
                RoleTypes.Scientist => new ScientistAssistence(brain),
                RoleTypes.Tracker => new TrackerAssistence(brain),

                RoleTypes.Phantom => new PhantomAssistence(brain),
                RoleTypes.Shapeshifter => new ShapeshifterAssistence(brain),
                RoleTypes.Viper => new ViperAssistence(brain),

                _ => new LLMDefaultAssistence(brain)
            };
        }

        public static ILLMAssistence GetAssistenceByRole(LLMAgentBrain brain)
        {
            return GetAssistenceByRole(brain.CurrentRole, brain);
        }
    }
}