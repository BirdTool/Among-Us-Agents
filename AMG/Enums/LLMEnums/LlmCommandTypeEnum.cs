namespace AMG.Enums.LLMEnums
{
    public enum LlmCommandTypeEnum
    {
        Get_Info,
        Add_Info,
        Remove_Info,
        Clear_Info,
        Put_Info,
        Public_Action, // Execute public actions such as vote and speak in chat
        Private_Action // Execute private actions such as send messages to the developer or to other agents
    }
}