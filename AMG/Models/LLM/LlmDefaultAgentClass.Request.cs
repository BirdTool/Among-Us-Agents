using System;
using System.Threading;
using System.Threading.Tasks;
using AMG.AI.LLM;
using AMG.AI.Services.ChatService.Llm;
using AMG.Enums;
using AMG.Enums.LLMEnums;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.Models.LLM
{
    public partial class LlmDefaultAgentClass
    {
        public virtual LlmEffortEnum? Effort { get; set; }

        public virtual string AgentName => GetType().Name;

        private LlmReasoning _reasoning;
        private CancellationTokenSource _cts;
        private Task<ILLMResponse<string, LlmTurn>> _pending;
        private LlmCommandContext _chainCtx;
        private int _epoch, _epochAtSend;
        private bool _parseRetried;
        private string _notes = string.Empty; // text added before the first answer (wake note...): goes right after the prompt

        // A request is in flight.
        public bool IsBusy => _pending != null;

        // Between BeginChain and EndChain (a chain is the series of steps of one wake-up).
        public bool InChain => _reasoning != null;

        public int StepsLeft => _reasoning == null ? 0 : _reasoning.MaxSteps > 0 ? Math.Max(0, _reasoning.MaxSteps - _reasoning.Steps) : int.MaxValue;

        // Starts a new chain and sends its first request. false = not started (busy, or BuildPrompt failed).
        // maxSteps / deadlineSeconds <= 0 mean no limit.
        public bool BeginChain(LlmCommandContext ctx, int maxSteps, float deadlineSeconds, string wakeNote = null)
        {
            if (IsBusy)
                return false;

            EndChain(); // a previous chain left open: close it

            _chainCtx = ctx;
            _parseRetried = false;
            _reasoning = LlmApiRouter.BeginReasoning(LlmConsumer.ChatService, $"llm-{AgentName}", maxSteps);
            _cts = deadlineSeconds > 0 ? new CancellationTokenSource(TimeSpan.FromSeconds(deadlineSeconds)) : new CancellationTokenSource();

            Messages.Clear();
            _notes = string.Empty;

            if (!string.IsNullOrWhiteSpace(wakeNote))
                AppendUser(wakeNote);

            return SendStep();
        }

        public bool ContinueChain()
        {
            if (IsBusy || !InChain)
                return false;

            if (StepsLeft <= 0)
            {
                EndChain();
                return false;
            }

            return SendStep();
        }

        public bool TryHarvest(out LlmTurnResult result)
        {
            result = null;

            var task = _pending;

            if (task == null || !task.IsCompleted)
                return false;

            _pending = null;

            if (_epochAtSend != _epoch)
            {
                EndChain();
                result = new LlmTurnResult { Status = LlmTurnStatus.Discarded };
                return true;
            }

            ILLMResponse<string, LlmTurn> response = task.IsFaulted || task.IsCanceled ? null : task.Result;

            if (response == null || response.HasError)
            {
                var detail = response as LlmResponse<LlmTurn>;
                string code = task.IsFaulted ? "Exception" : task.IsCanceled ? "Cancelled" : detail?.ErrorCode ?? "Unknown";
                string why = task.IsFaulted ? task.Exception?.GetBaseException().Message : task.IsCanceled ? "cancelled" : response.Error;

                LogManager.LogWarning($"[LLM {AgentName}] step failed ({code}): {why}");

                // The model answered with something that is not our JSON: ask once more (the router may pick another key).
                if (!_parseRetried && code == "ParseError" && _cts != null && !_cts.IsCancellationRequested)
                {
                    _parseRetried = true;

                    if (SendStep())
                        return false;
                }

                EndChain(false, code);
                result = new LlmTurnResult { Status = LlmTurnStatus.Failed, ErrorCode = code, Error = why ?? string.Empty };
                return true;
            }

            var ok = response as LlmResponse<LlmTurn>;
            string raw = ok?.RawText ?? string.Empty;
            var turn = response.Response ?? new LlmTurn();
            turn.Commands ??= [];

            Messages.Add(new LlmMessage(LlmRole.Assistant, raw));
            result = new LlmTurnResult { Status = LlmTurnStatus.Ok, Turn = turn, RawText = raw, StepsLeft = StepsLeft };
            return true;
        }

        // The answer being awaited (if any) was decided with stale information: it will come back as Discarded.
        public void Invalidate() => _epoch++;

        // Stops the chain. A request in flight is cancelled and the chain closes when it comes back through TryHarvest (Discarded).
        public void Cancel() => EndChain();

        public void EndChain(bool success = true, string errorCode = null)
        {
            if (_pending != null)
            {
                _epoch++;
                _cts?.Cancel();
                return;
            }

            _reasoning?.Complete(success, success ? null : errorCode);
            _reasoning = null;
            _chainCtx = null;

            _cts?.Dispose();
            _cts = null;
        }

        public void AppendUser(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (Messages.Count == 0)
                _notes = _notes.Length == 0 ? text : _notes + "\n" + text;
            else if (Messages[^1].Role == LlmRole.User)
                Messages[^1] = Messages[^1] with { Content = Messages[^1].Content + "\n" + text };
            else
                Messages.Add(new LlmMessage(LlmRole.User, text));
        }

        private bool SendStep()
        {
            string prompt;

            try
            {
                prompt = BuildPrompt(_chainCtx);
            }
            catch (Exception e)
            {
                LogManager.LogWarning($"[LLM {AgentName}] BuildPrompt failed: {e.Message}");
                EndChain(false, "PromptError");
                return false;
            }

            _epochAtSend = _epoch;

            string head = _notes.Length == 0 ? prompt : prompt + "\n\n" + _notes;

            var request = new LlmRequest<LlmTurn>
            {
                Messages = [new LlmMessage(LlmRole.User, head), .. Messages],
                MaxTokens = MaxOutputTokens,
                JsonMode = true,
                Effort = Effort
            };

            var reasoning = _reasoning;
            var ct = _cts.Token;

            // The router limits requests per key, so agents asking at the same time are safe.
            _pending = Task.Run(() => reasoning.RequestAsync(request, ct), ct);
            return true;
        }
    }
}
