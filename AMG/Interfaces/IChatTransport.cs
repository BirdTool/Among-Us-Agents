using AMG.Enums.SafeRpcEnums;

namespace AMG.Interfaces
{
    public interface IChatTransport
    {
        ChatRpcEnums TrySend(string text);
    }
}