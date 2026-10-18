using AMG.Enums;

namespace AMG.Models.LLM.Providers
{
    public class OpenAIClient(string key, uint index, string clientId) : OpenAICompatibleClient(key, index, clientId)
    {
        public override LlmProvider Provider => LlmProvider.OpenAI;

        protected override string DefaultModel => "gpt-5.4-mini";
        protected override string BaseUrl => "https://api.openai.com/v1";

        protected override string MaxTokensField => "max_completion_tokens";
    }
}
