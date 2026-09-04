using AmongUs.GameOptions;

namespace AMG.Models.InternalRpcModels
{
    public class PlayerRoles(PlayerControl player, RoleTypes role)
    {
        public PlayerControl Player { get; } = player;
        public RoleTypes Role { get; } = role;
    }
}