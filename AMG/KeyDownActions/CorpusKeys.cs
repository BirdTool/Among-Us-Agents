using AMG.AI.Services.ChatService.Corpus;
using AMG.Utilities.KeyDown;
using UnityEngine;

namespace AMG.KeyDownActions
{
    [KeyPressManager.Register(KeyCode.F9)]
    public static class CorpusToggleKey
    {
        public static void Execute() => ChatCorpus.Toggle();
    }
}
