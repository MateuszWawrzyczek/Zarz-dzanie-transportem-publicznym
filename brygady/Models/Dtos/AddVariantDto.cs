namespace Brygady.Data
{
    public class AddVariantDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public List<TripRequest>? Trips { get; set; }
    }
}