

namespace Brygady.Data
{
    public class LineStop{
        public int Id { get; set; }
        public int LineId {get; set;}
        public int StopId {get; set;}
        public string? StopName { get; set; }
        public int Direction {get; set;}
        public int Order{get; set;}
    }
}