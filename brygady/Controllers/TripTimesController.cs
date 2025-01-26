using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Brygady.Data;

namespace Brygady.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsTimesController : ControllerBase
    {
        private readonly string _connectionString;

        public TripsTimesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpPost("AddTripTime")]
        public async Task<IActionResult> AddTripTime([FromBody] AddTripTimeRequest request)
        {
            Console.Write(request);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                // Tworzenie zapytania SQL do dodania nowego wpisu w tabeli TripTimes
                        var query = @"
                        INSERT INTO trip_times ( trip_id, arrival_departure_time, line_stop_id)
                        VALUES ( @tripId, @arrivalDepartureTime, @lineStopId)
                        ON CONFLICT (trip_id, line_stop_id)  
                        DO UPDATE SET
                            arrival_departure_time = @arrivalDepartureTime;  -- Aktualizujemy tylko godzinę, jeśli już istnieje

                    ";

                // Wykonywanie zapytania SQL
                using var command = new NpgsqlCommand(query, connection);
                //command.Parameters.AddWithValue("@id", request.Id);
                command.Parameters.AddWithValue("@tripId", request.TripId);
                command.Parameters.AddWithValue("@arrivalDepartureTime", request.ArrivalDepartureTime);
                command.Parameters.AddWithValue("@lineStopId", request.LineStopId);

                await command.ExecuteNonQueryAsync();

                return Ok(new { Message = "Dodano nowy czas przejazdu." });
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                return StatusCode(500, new { Message = "Wystąpił błąd podczas dodawania czasu przejazdu.", Error = ex.Message });
            }
        }
    }
}
