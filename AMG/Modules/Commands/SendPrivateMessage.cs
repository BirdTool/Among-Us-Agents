using AMG.AI.Services.MessagesService;
using AMG.Data;
using AMG.Utilities;

namespace AMG.Modules.Commands
{
    public class SendPrivateMessageCommand : ChatCommand
    {
        public override string Name => "private";
        public override string[] Aliases => ["p"];

        public static void Send(string content) => MessageServiceClass.WriteMessage(content, GlobalVariables.MyIdentity);

        public override void Execute(string args)
        {
            if (args.Length > 0)
            {
                Send(args);
                if (Utils.IsFreePlay)
                    PlayerControl.LocalPlayer.RpcSendChat("Sent");
                else
                    Utils.AddChatPrivate("Sent");
            }
        }
    }
}
