using AMG.Enums;

namespace AMG.Models.LLM.Providers
{
    public class DeepSeekClient(string key, uint index, string clientId) : OpenAICompatibleClient(key, index, clientId)
    {
        public override LlmProvider Provider => LlmProvider.DeepSeek;

        // "deepseek-chat" = non-thinking; use "deepseek-reasoner" through ILLMRequest.Model for thinking.
        protected override string DefaultModel => "deepseek-chat";
        protected override string BaseUrl => "https://api.deepseek.com";
    }
}
