namespace Brygady.Data
{
    public class TripTimes
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public int VariantStopId { get; set; }
        public TimeSpan? ArrivalDepartureTime { get; set; }
        
    }
}