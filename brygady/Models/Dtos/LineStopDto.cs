namespace Brygady.Data
{
    public class LineStopDto
    {
        public int Id { get; set; }
        public int LineId { get; set; }
        public string? StopName { get; set; }  // Może być nullable, jeśli tak chcesz
        public int Direction { get; set; }
        public int Order { get; set; }
        public List<TripTimeDto> Trips { get; set; } = new List<TripTimeDto>();
    }
}