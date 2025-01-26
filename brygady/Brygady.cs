using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Brygady.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BusStop> BusStops { get; set; } // Mapowanie na tabelę "bus_stops"
        public DbSet<TypeOfDays> TypeOfDays { get; set; } // Mapowanie na tabelę "type_of_days"

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BusStop>().ToTable("bus_stops"); // Wymuszenie nazwy tabeli "bus_stops"
            modelBuilder.Entity<TypeOfDays>().ToTable("types_of_days"); // Wymuszenie nazwy tabeli "type_of_days"
        }
    }
}
