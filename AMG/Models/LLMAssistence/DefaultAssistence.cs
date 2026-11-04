using AMG.Interfaces;
using AMG.Models.LLMAssistence.Crewmate;
using AMG.Models.LLMAssistence.Impostor;
using AmongUs.GameOptions;

namespace AMG.Models.LLMAssistence
{
    public class LLMDefaultAssistence : ILLMAssistence
    {
        public virtual void Update()
        {

        }

        public static ILLMAssistence GetAssistenceByRole(RoleTypes role)
        {
            return role switch
            {
                RoleTypes.Detective => new DetectiveAssistence(),
                RoleTypes.Engineer => new EngineerAssistence(),
                RoleTypes.Scientist => new ScientistAssistence(),
                RoleTypes.Tracker => new TrackerAssistence(),

                RoleTypes.Phantom => new PhantomAssistence(),
                RoleTypes.Shapeshifter => new ShapeshifterAssistence(),
                RoleTypes.Viper => new ViperAssistence(),

                _ => new LLMDefaultAssistence()
            };
        }
    }
}