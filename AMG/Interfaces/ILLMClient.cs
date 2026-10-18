using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AMG.Enums;
using AMG.Models.LLM;

namespace AMG.Interfaces
{
    public interface ILLMClient
    {
        string ClientId { get; }
        uint Index { get; }
        string Key { get; }
        LlmProvider Provider { get; }

        ILLMResponse<string, T> Request<T>(ILLMRequest<T> llm);

        Task<ILLMResponse<string, T>> RequestAsync<T>(ILLMRequest<T> llm, CancellationToken cancellationToken = default);
    }

    public interface ILLMRequest<T>
    {
        string Prompt { get; }
        int MaxTokens { get; }
        bool Stream { get; }

        string SystemPrompt { get; }

        IReadOnlyList<LlmMessage> Messages { get; }

        string Model { get; }

        float? Temperature { get; }

        bool JsonMode { get; }

        LlmConsumer Consumer { get; }
    }

    public interface ILLMResult<T>
    {
        bool Success { get; }
        T Result { get; }
        string Error { get; }
    }

    public interface ILLMResponse<TError, TResponse>
    {
        float Time { get; }
        int TokenCost { get; }
        TResponse Response { get; }
        TError Error { get; }

        bool HasError { get; }
        bool HasResponse { get; }
    }
}
