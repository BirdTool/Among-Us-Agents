using AMG.Interfaces;

namespace AMG.Models.Guidances
{
    public class MeetingBodyLocation(SystemTypes room) : IGuidance
    {
        public SystemTypes Room { get; } = room;

        public string Evaluate()
        {
            return "";
        }
    }
}