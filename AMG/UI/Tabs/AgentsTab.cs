using AMG.AI.Services.MessagesService;
using AMG.Data;
using System;
using AMG.UI.Elements;
using AMG.UI.UIActions;
using AMG.Enums.ChatServiceEnums;
using AMG.AI.Services.ChatService;

namespace AMG.UI.Tabs;

public class AgentsTab : TabBase
{
    public AgentsTab() : base("Agents Controller") { }

    protected override void Build()
    {
        new MenuText("Agents controller");
        new MenuSubText("Choose the agents quantity");
        
        new MenuSlider(1f, 15f, 1f, (value) => 
        {
            AgentsController.AgentsCount = (int)value;
        });

        new MenuSubText("Choose the impostor quantity");
        
        new MenuSlider(1f, 3f, 2f, (value) => 
        {
            AgentsController.ImpostorCount = (int)value;
        });

        new MenuButton("Spawn Agents", () => 
        {
            AgentsController.SpawnAgents();
        });

        new MenuSubText("My identity (private messages)");

        new MenuTextSlider(
            Enum.GetNames(typeof(PrivateMessageSenderEnum)),
            (int)GlobalVariables.MyIdentity,
            (index) => GlobalVariables.MyIdentity = (PrivateMessageSenderEnum)index,
            () => (int)GlobalVariables.MyIdentity);

        new MenuSubText("Set Chat Mode");

        new MenuTextSlider(
            Enum.GetNames(typeof(ChatModeEnum)),
            (int)ChatMode.Current,
            (index) => ChatMode.Current = (ChatModeEnum)index,
            () => (int)ChatMode.Current);

        new MenuButton("Chat Debug", () => 
        {
            ChatMode.ChatDebug = !ChatMode.ChatDebug;
        });
    }
}
