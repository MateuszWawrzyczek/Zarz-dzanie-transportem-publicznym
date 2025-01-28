using System.ComponentModel.DataAnnotations;


namespace Brygady.Data
{
    public class AddLineStopDto
    {
        [Required]
        public int LineId { get; set; }
        [Required]
        public int StopId { get; set; }
        [Required]
        public int Direction { get; set; }
        [Required]
        public int Order { get; set; }
    }
}