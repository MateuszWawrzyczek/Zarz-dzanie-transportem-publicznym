using NetTopologySuite.Geometries;

namespace Brygady.Data
{
    public class Line
    {
        public int id { get; set; }
        public string? name { get; set; } 
        public Point? location { get; set; } 
        public Line(string na, Point loc)
        {
            name = na;
            location = loc;
        }
        public Line(string na)
        {
            name = na;
        }

        public Line()
        {
        }
    }
}
