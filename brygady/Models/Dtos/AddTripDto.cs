namespace Brygady.Data
{    
    public class TripRequest
    {
        public int Id { get; set; }
        public int TypeOfDayId { get; set; }
        public int VariantId { get; set; }
        public int Direction { get; set; }  
        public List<TripTimeDto>? Times { get; set; }

    }
        public class NewTripDto{
        public int Id { get; set; }
        public int TypeOfDayId { get; set; }
    }

}
