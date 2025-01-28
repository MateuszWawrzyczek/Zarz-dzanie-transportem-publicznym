
namespace Brygady.Data
{
    public class TypeOfDays
    {
        public int id {get; set;}
        public string? name {get; set;}
        public TypeOfDays(string na)
        {
            name=na;
        }
        public TypeOfDays(){}
    }
}