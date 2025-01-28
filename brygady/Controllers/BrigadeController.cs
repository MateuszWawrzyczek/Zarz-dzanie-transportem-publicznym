using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Brygady.Data;
using System.Text.Json;  

namespace Brygady.Controllers
{
    [Route("api/[controller]")]  
    [ApiController]
    public class BrigadeController : ControllerBase
    {
        private readonly string _connectionString;

        public BrigadeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                  ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet("GetBrigades/{typeOfDayId}")]
        public async Task<ActionResult<IEnumerable<BrigadeDto>>> GetBrigades(int typeOfDayId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            try
            {
                var query = @"
                    SELECT
                        b.id AS brigadeId,
                        b.type_of_days_id AS typeOfDayId,
                        b.name,
                        b.working_time,
                        td.shortage_name,  
                        COALESCE(
                            json_agg(
                                json_build_object('brigade_trip_id', bt.id, 'trip_id', bt.trip_id)
                            ) FILTER (WHERE bt.id IS NOT NULL),
                            '[]'
                        ) AS trips
                    FROM brigades b
                    LEFT JOIN brigades_trips bt 
                        ON b.id = bt.brigade_id
                    LEFT JOIN types_of_days td  
                        ON b.type_of_days_id = td.id
                    WHERE b.type_of_days_id = @typeOfDayId
                    GROUP BY 
                        b.id, b.type_of_days_id, b.name, b.working_time, td.shortage_name;  

                ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@typeOfDayId", typeOfDayId);

                using var reader = await command.ExecuteReaderAsync();
                var brigades = new List<BrigadeDto>();

                while (await reader.ReadAsync())
                {
                    var brigadeId = reader.GetInt32(reader.GetOrdinal("brigadeId"));
                    var TypeOfDayId = reader.GetInt32(reader.GetOrdinal("typeOfDayId"));
                    var name = reader.GetString(reader.GetOrdinal("name"));
                    var workingTime = reader.GetInt32(reader.GetOrdinal("working_time"));
                    var shortageName = reader.GetString(reader.GetOrdinal("shortage_name"));
                    var tripsJson = reader.IsDBNull(reader.GetOrdinal("trips")) 
                                    ? "[]" 
                                    : reader.GetString(reader.GetOrdinal("trips"));
                    

                    var brigade = new BrigadeDto
                    {
                        brigadeId = brigadeId,
                        typeOfDayId = TypeOfDayId,
                        name = name,
                        workingTime = workingTime,
                        shortageName=shortageName,  
                        trips = JsonSerializer.Deserialize<List<BrigadeTripsDto>>(tripsJson) ?? new List<BrigadeTripsDto>()
                    };

                    Console.Write(reader.GetString(reader.GetOrdinal("shortage_name")));
                    brigades.Add(brigade);
                }
                


                return Ok(brigades); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                return StatusCode(500, "Wewnętrzny błąd serwera.");
            }
        }

        [HttpPost("UpdateBrigades")]
        public async Task<IActionResult> UpdateBrigades([FromBody] List<BrigadeDto> brigades)
        {
            if (brigades == null || brigades.Count == 0)
            {
                Console.WriteLine("Error: The brigades list is empty.");
                return BadRequest("The brigades list cannot be empty.");
            }


                Console.WriteLine($"Received {brigades.Count} brigades:");
            foreach (var brigade in brigades)
            {
                Console.WriteLine($"Brigade: name={brigade.name}, typeOfDayId={brigade.typeOfDayId}, workingTime={brigade.workingTime}");
                if (brigade.trips != null && brigade.trips.Count > 0)
                {
                    foreach (var trip in brigade.trips)
                    {
                        Console.WriteLine($"  Trip: trip_id={trip.trip_id}");
                    }
                }
                else
                {
                    Console.WriteLine("  No trips for this brigade.");
                }
            }
            var firstBrigade = brigades.First();
            int typeOfDayId = firstBrigade.typeOfDayId;

            Console.WriteLine($"Starting UpdateBrigades for typeOfDayId: {typeOfDayId}");
            
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    Console.WriteLine("Opening database connection...");
                    await connection.OpenAsync();

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        Console.WriteLine("Transaction started.");
                        try
                        {
                            var deleteBrigadesQuery = "DELETE FROM brigades WHERE type_of_days_id = @typeOfDayId";
                            using (var deleteBrigadesCommand = new NpgsqlCommand(deleteBrigadesQuery, connection, transaction))
                            {
                                deleteBrigadesCommand.Parameters.AddWithValue("@typeOfDayId", typeOfDayId);
                                Console.WriteLine($"Executing delete query: {deleteBrigadesQuery} with typeOfDayId: {typeOfDayId}");
                                await deleteBrigadesCommand.ExecuteNonQueryAsync();
                            }

                            var insertBrigadeQuery = @"
                                INSERT INTO brigades (type_of_days_id, name, working_time)
                                VALUES (@typeOfDayId, @name, @workingTime)
                                RETURNING id";

                            foreach (var brigade in brigades)
                            {
                                Console.WriteLine($"Inserting brigade: {brigade.name}, typeOfDayId: {brigade.typeOfDayId}, workingTime: {brigade.workingTime}");

                                using (var insertBrigadeCommand = new NpgsqlCommand(insertBrigadeQuery, connection, transaction))
                                {
                                    insertBrigadeCommand.Parameters.AddWithValue("@typeOfDayId", brigade.typeOfDayId);
                                    insertBrigadeCommand.Parameters.AddWithValue("@name", (object?)brigade.name ?? DBNull.Value);
                                    insertBrigadeCommand.Parameters.AddWithValue("@workingTime", brigade.workingTime);

                                    var result = await insertBrigadeCommand.ExecuteScalarAsync();
                                    if (result == null || !(result is int))
                                    {
                                        throw new InvalidOperationException("Nie udało się uzyskać ID nowej brygady.");
                                    }
                                    var newBrigadeId = (int)result;


                                    var insertTripQuery = @"
                                        INSERT INTO brigades_trips (brigade_id, trip_id)
                                        VALUES (@brigadeId, @tripId)";

                                    foreach (var trip in brigade.trips ?? new List<BrigadeTripsDto>())
                                    {
                                        
                                        using (var insertTripCommand = new NpgsqlCommand(insertTripQuery, connection, transaction))
                                        {
                                            insertTripCommand.Parameters.AddWithValue("@brigadeId", newBrigadeId);
                                            insertTripCommand.Parameters.AddWithValue("@tripId", trip.trip_id);
                                            await insertTripCommand.ExecuteNonQueryAsync();
                                        }
                                    }
                                }
                            }

                            await transaction.CommitAsync(); 
                            return Ok(new { message = "Brygady zaaktualizowane poprawnie." });
                        }
                        catch (PostgresException ex) when (ex.SqlState == "42601")
                        {
                            return Conflict(new { message = ex.Message });
                        }
                        catch (PostgresException ex) when (ex.SqlState == "P0001")
                        {
                            return Conflict(new { message = ex.Message });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Błąd podczas tranzakcji: {ex.Message}");

                            await transaction.RollbackAsync();

                            return StatusCode(500, new { message = "Wewnętrzny błąd serwera.", details = ex.Message });
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

       [HttpGet("GetBrigadesAccordingToTime")]
        public async Task<ActionResult<IEnumerable<BrigadeDto>>> GetBrigadesAccordingToTime([FromQuery] int time)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            try
            {
                var query = @"
                    SELECT
                        b.id AS brigadeId,
                        b.type_of_days_id AS typeOfDayId,
                        b.name,
                        b.working_time,
                        COALESCE(
                            json_agg(
                                json_build_object('brigade_trip_id', bt.id, 'trip_id', bt.trip_id)
                            ) FILTER (WHERE bt.id IS NOT NULL),
                            '[]'
                        ) AS trips
                    FROM brigades b
                    LEFT JOIN brigades_trips bt 
                        ON b.id = bt.brigade_id
                    WHERE b.working_time < @workingTime
                    GROUP BY 
                        b.id, b.type_of_days_id, b.name, b.working_time;
                ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@workingTime", time);

                using var reader = await command.ExecuteReaderAsync();
                var brigades = new List<BrigadeDto>();

                while (await reader.ReadAsync())
                {
                    var brigadeId = reader.GetInt32(reader.GetOrdinal("brigadeId"));
                    var TypeOfDayId = reader.GetInt32(reader.GetOrdinal("typeOfDayId"));
                    var name = reader.GetString(reader.GetOrdinal("name"));
                    var workingTime = reader.GetInt32(reader.GetOrdinal("working_time"));
                    var tripsJson = reader.IsDBNull(reader.GetOrdinal("trips")) 
                                    ? "[]" 
                                    : reader.GetString(reader.GetOrdinal("trips"));


                    var brigade = new BrigadeDto
                    {
                        brigadeId = brigadeId,
                        typeOfDayId = TypeOfDayId,
                        name = name,
                        workingTime = workingTime,
                        trips = JsonSerializer.Deserialize<List<BrigadeTripsDto>>(tripsJson) ?? new List<BrigadeTripsDto>()
                    };


                    brigades.Add(brigade);
                }
                


                return Ok(brigades); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                return StatusCode(500, "Wewnętrzny błąd serwera.");
            }
        }
    }
}
