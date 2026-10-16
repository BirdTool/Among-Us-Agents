using AMG.Enums;

namespace AMG.Interfaces
{
    public interface ILLMClient
    {
        uint Index { get; }
        string Key { get; }
        LlmProvider Provider { get; }
        ILLMResponse<string, T> Request<T>(ILLMRequest<T> llm);
    }

    public interface ILLMRequest<T>
    {
        string Prompt { get; }
        int MaxTokens { get; }
        bool Stream { get; }   
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