using System;
using AMG.AI.Services.MessagesService;
using AMG.Modules.AutoPlay;
using UnityEngine;

namespace AMG.UI;

public class ModUpdater(IntPtr ptr) : MonoBehaviour(ptr)
{
    private void Update()
    {
        MessageServiceClass.Pump();
        PersistentAgent.Tick();
        AutoReturnToLobby.Tick();
    }
}
