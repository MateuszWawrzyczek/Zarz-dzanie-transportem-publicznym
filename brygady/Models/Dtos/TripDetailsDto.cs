namespace Brygady.Data
{
    public class TripDetailsDto
    {
        public int TripId { get; set; }
        public string? LineNumber{ get; set; }
        public string? StartStop { get; set; }
        public string? EndStop { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
