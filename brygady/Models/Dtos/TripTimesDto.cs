namespace Brygady.Data
{
    public class TripTimeDto
    {
        public int TripId { get; set; }
        public int TypeOfDayId { get; set; } 
        public TimeSpan ArrivalDepartureTime { get; set; }
    }

    public class DepartureTimesDto
    {
        public TimeSpan departureTime { get; set; }
        public string? direction { get; set; }
        public string? lineNumber { get; set; }
    }

    public class AddTripTimeRequest
    {
        public int Id { get; set; } 
        public int TripId { get; set; } 
        public TimeSpan ArrivalDepartureTime { get; set; } 
        public int LineStopId { get; set; } 
    }


}