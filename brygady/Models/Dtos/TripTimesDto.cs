namespace Brygady.Data
{
public class TripTimeDto
{
    public int TripId { get; set; }
    public int TypeOfDayId { get; set; } // Ensure this matches what the application expects
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
    public int Id { get; set; } // Id z trip time
    public int TripId { get; set; } // Istniejące tripId
    public TimeSpan ArrivalDepartureTime { get; set; } // Czas przyjazdu/odjazdu
    public int LineStopId { get; set; } // Id przystanku
}


}