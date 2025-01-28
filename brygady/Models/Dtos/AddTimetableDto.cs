using System.ComponentModel.DataAnnotations;

namespace Brygady.Data
{
    public class AddTimetableDto
    {
        public int Id { get; set; }
        public int LineId { get; set; }
        //public string StopName { get; set; }
        public int Direction { get; set; }
        //public int Order { get; set; }
        public List<TripTimeDto>? Trips { get; set; } = new List<TripTimeDto>();
    }


}
