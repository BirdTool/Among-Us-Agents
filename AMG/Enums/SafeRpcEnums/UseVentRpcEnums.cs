namespace AMG.Enums.SafeRpcEnums
{
    public enum UseVentRpcEnums
    {
        ERROR_AgentIsDead,
        ERROR_AgentIsNotImpostorOrEngineer,
        ERROR_VentDoesNotExist,
        ERROR_AgentIsTooFarFromVent,
        ERROR_AgentIsInCooldown, // only engineer
        SUCCESS,
        SUCCESS_Exit
    }
}