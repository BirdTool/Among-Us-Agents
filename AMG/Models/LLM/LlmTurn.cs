using System.Collections.Generic;
using System.Text.Json;

namespace AMG.Models.LLM
{
    // What the model must answer with (JSON mode): its reasoning and the commands to run, in order.
    public sealed class LlmTurn
    {
        public string Reasoning { get; set; }
        public List<LlmTurnCommand> Commands { get; set; } = [];
    }

    public sealed class LlmTurnCommand
    {
        public string Name { get; set; }

        // Kept as JsonElement so a model that writes 3 instead of "3", or "hi" instead of ["hi"], is not a ParseError.
        public JsonElement Args { get; set; }

        public string[] GetArgs()
        {
            switch (Args.ValueKind)
            {
                case JsonValueKind.Array:
                    var list = new List<string>();
                    foreach (var item in Args.EnumerateArray())
                        list.Add(AsText(item));
                    return [.. list];

                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    return [];

                default:
                    return [AsText(Args)];
            }
        }

        private static string AsText(JsonElement e)
            => e.ValueKind == JsonValueKind.String ? e.GetString() : e.ValueKind == JsonValueKind.Null ? string.Empty : e.GetRawText();
    }

    public enum LlmTurnStatus
    {
        Ok,        // Turn is filled: run its commands, then ContinueChain() or EndChain()
        Failed,    // the request failed; the chain is already ended
        Discarded  // Invalidate()/Cancel() happened while the model was thinking; the chain is already ended
    }

    public sealed class LlmTurnResult
    {
        public LlmTurnStatus Status { get; init; }
        public LlmTurn Turn { get; init; }
        public string RawText { get; init; } = string.Empty;
        public string ErrorCode { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
        public int StepsLeft { get; init; }

        public bool Ok => Status == LlmTurnStatus.Ok;
    }
}
