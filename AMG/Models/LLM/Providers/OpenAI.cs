using System;
using System.Collections.Generic;
using AMG.Enums;

namespace AMG.Models.LLM.Providers
{
    public class OpenAIClient(string key, uint index, string clientId) : OpenAICompatibleClient(key, index, clientId)
    {
        public override LlmProvider Provider => LlmProvider.OpenAI;

        protected override string DefaultModel => "gpt-5.4-mini";
        protected override string BaseUrl => "https://api.openai.com/v1";

        protected override string MaxTokensField => "max_completion_tokens";

        // "reasoning_effort" exists only on reasoning models (gpt-5.x, o-series); it is a 400 on gpt-4.x.
        // gpt-5.1+ default to "none", so without an Effort these models answer without reasoning.
        protected override bool ApplyReasoning(Dictionary<string, object> body, LlmCall call)
        {
            if (!call.Effort.HasValue || !IsReasoningModel(call.Model))
                return false;

            body["reasoning_effort"] = call.Effort.Value.ToString().ToLowerInvariant();
            return true;
        }

        private static bool IsReasoningModel(string model)
            => model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase)
               || (model.Length > 1 && char.ToLowerInvariant(model[0]) == 'o' && char.IsDigit(model[1]));
    }
}
