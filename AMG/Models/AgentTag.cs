using AMG.Enums;

namespace AMG.Models
{
    public class AgentTag
    {
        public string Tag;
        public string ColourHex;
        public string Size;
        public IdentifierEnum Identifier;
        public float? ExpiresAt = null;
    }
}
