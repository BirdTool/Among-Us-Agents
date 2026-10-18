using AMG.Enums;

namespace AMG.Models.LLM
{
    public sealed record LlmMessage(LlmRole Role, string Content);
}
