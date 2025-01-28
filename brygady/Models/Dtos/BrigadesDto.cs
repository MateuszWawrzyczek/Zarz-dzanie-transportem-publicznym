namespace Brygady.Data
{
    public class BrigadeDto
    {
        public int? brigadeId {get; set;}
        public int typeOfDayId {get; set;}
        public string? name {get; set;}
        public int workingTime {get; set;}
        public string? shortageName { get; set; }
        public List<BrigadeTripsDto>? trips {get; set;}
    }
    public class BrigadeTripsDto
    {
        public int brigade_trip_id {get; set;}
        public int trip_id {get; set;}   
    }
    public class BrigadeUpdateDto{
        public int TypeOfDayId { get; set; }
        public List<BrigadeDto>? Brigades { get; set; } = new();
    }

}
