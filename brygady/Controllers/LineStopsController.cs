using Brygady.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Brygady.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LineStopsController : ControllerBase
    {
        private readonly string _connectionString;

        public LineStopsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet("{lineId}")]
        public async Task<ActionResult<IEnumerable<LineStop>>> GetLineStopsByLineId(int lineId)
        {
            var lineStopsList = new List<LineStop>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        SELECT 
                            ls.id, 
                            ls.line_id, 
                            ls.stop_id, 
                            bs.name AS stop_name, 
                            ls.direction, 
                            ls.""order""
                        FROM 
                            line_stops ls
                        INNER JOIN 
                            bus_stops bs
                        ON 
                            ls.stop_id = bs.id
                        WHERE 
                            ls.line_id = @lineId;";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@lineId", lineId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lineStopsList.Add(new LineStop
                                {
                                    Id = reader.GetInt32(0),
                                    LineId = reader.GetInt32(1),
                                    StopId = reader.GetInt32(2),
                                    StopName = reader.GetString(3),
                                    Direction = reader.GetInt32(4),
                                    Order = reader.GetInt32(5)
                                });
                            }
                        }
                    }
                }

                return Ok(lineStopsList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<ActionResult> AddOrUpdateLineStops([FromBody] List<AddLineStopDto> lineStops)
        {
            Console.Write(lineStops);
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Nieprawidłowy ModelState:", ModelState);
                return BadRequest(ModelState);
            }

            if (lineStops == null || !lineStops.Any())
            {
                Console.WriteLine("Pusta lista lineStops:", lineStops);
                return BadRequest("The lineStops list cannot be empty.");
            }

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            foreach (var lineStop in lineStops)
                            {
                                Console.WriteLine(lineStop.id.HasValue);
                                Console.WriteLine(lineStop.id);

                                if (lineStop.id.HasValue)
                                {
                                    
                                    var updateQuery = @"
                                        UPDATE line_stops
                                        SET stop_id = @stopId, 
                                            direction = @direction, 
                                            ""order"" = @order
                                        WHERE id = @id;
                                    ";

                                    using (var updateCommand = new NpgsqlCommand(updateQuery, connection, transaction))
                                    {
                                        updateCommand.Parameters.AddWithValue("@id", lineStop.id);
                                        updateCommand.Parameters.AddWithValue("@stopId", lineStop.StopId);
                                        updateCommand.Parameters.AddWithValue("@direction", lineStop.Direction);
                                        updateCommand.Parameters.AddWithValue("@order", lineStop.Order);

                                        await updateCommand.ExecuteNonQueryAsync();
                                    }
                                }
                                else 
                                {
                                    var insertQuery = @"
                                        INSERT INTO line_stops (line_id, stop_id, direction, ""order"")
                                        VALUES (@lineId, @stopId, @direction, @order);
                                    ";

                                    using (var insertCommand = new NpgsqlCommand(insertQuery, connection, transaction))
                                    {
                                        insertCommand.Parameters.AddWithValue("@lineId", lineStop.LineId);
                                        insertCommand.Parameters.AddWithValue("@stopId", lineStop.StopId);
                                        insertCommand.Parameters.AddWithValue("@direction", lineStop.Direction);
                                        insertCommand.Parameters.AddWithValue("@order", lineStop.Order);

                                        await insertCommand.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            await transaction.CommitAsync();
                            return Ok(new { message = "Line stops added or updated successfully" });
                        }
                        catch (Exception err)
                        {
                            await transaction.RollbackAsync();
                            Console.WriteLine($"Error during transaction: {err.Message}");
                            return StatusCode(500, $"Internal server error during transaction: {err.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddOrUpdateLineStops: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpDelete("RemoveLineStops")]
        public async Task<ActionResult> RemoveLineStops([FromBody] List<int> indexes)
        {
            if (indexes == null || indexes.Count == 0)
            {
                return Ok(new { message = "Brak przystanków do usunięcia. Operacja została pominięta." });
            }
            
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"
                        DELETE FROM line_stops
                        WHERE Id = ANY(@Ids::int[]);";  

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Ids", indexes.ToArray());

                        var result = await command.ExecuteNonQueryAsync();

                        if (result == 0)
                        {
                            return NotFound("Brak przystanków do usunięcia dla podanych ID.");
                        }
                    }
                }

                return Ok(new { message = "Przystanki usunięte poprawnie" });
            }
            catch (Exception err)
            {
                return StatusCode(500, $"Błąd serwera podczas operacji: {err.Message}");
            }
        }


    }   

    public class AddLineStopDto
    {
        public int? id { get; set; }
        public int LineId { get; set; }
        public int StopId { get; set; }
        public int Direction { get; set; }
        public int Order { get; set; }
    }

    public class LineStop
    {
        public int Id { get; set; }
        public int LineId { get; set; }
        public int StopId { get; set; }
        public string? StopName { get; set; } 
        public int Direction { get; set; }
        public int Order { get; set; }
    }
}
