using NetTopologySuite.Geometries;

namespace Brygady.Data
{
    public class Line
    {
        public int id { get; set; }
        public string? name { get; set; } // Możesz dodać inne właściwości, które przechowują dodatkowe dane

        public Point? location { get; set; } // Właściwość dla lokalizacji przystanku

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
