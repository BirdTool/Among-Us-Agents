using System.Collections.Generic;
using AMG.Enums;
using AMG.Enums.LLMEnums;

namespace AMG.Models.LLM.Providers
{
    public class DeepSeekClient(string key, uint index, string clientId) : OpenAICompatibleClient(key, index, clientId)
    {
        public override LlmProvider Provider => LlmProvider.DeepSeek;

        // "deepseek-chat" / "deepseek-reasoner" were retired on 2026-07-24. "deepseek-flash" is the current Flash model
        // (thinking + non-thinking in one); use "deepseek-v4-pro" through ILLMRequest.Model for the flagship.
        protected override string DefaultModel => "deepseek-flash";
        protected override string BaseUrl => "https://api.deepseek.com";

        // Thinking is a toggle plus reasoning_effort (low / high / max). Low -> low, Medium -> high, High -> max.
        // (On deepseek-v4-pro the API maps low to high.) Without an Effort the API default applies (thinking on, high).
        protected override bool ApplyReasoning(Dictionary<string, object> body, LlmCall call)
        {
            if (!call.Effort.HasValue)
                return false;

            body["thinking"] = new Dictionary<string, object> { ["type"] = "enabled" };
            body["reasoning_effort"] = call.Effort.Value switch
            {
                LlmEffortEnum.Low => "low",
                LlmEffortEnum.Medium => "high",
                _ => "max"
            };

            return true;
        }
    }
}
