using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Brygady.Data;



namespace Brygady.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusStopsController : ControllerBase
    {
        private readonly string _connectionString;

        public BusStopsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet]
        public async Task<IActionResult> GetBusStops()
        {
            try
            {
                var busStops = new List<BusStop>();

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT id, name FROM bus_stops ORDER BY name ASC;";
                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            busStops.Add(new BusStop
                            {
                                id = reader.GetInt32(0),
                                name = reader.GetString(1)
                            });
                        }
                    }
                }

                return Ok(busStops);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBusStop(int id)
        {
            try
            {
                BusStop? busStop = null;

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT id, name FROM bus_stops WHERE id = @Id";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                busStop = new BusStop
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.GetString(1)
                                };
                            }
                        }
                    }
                }

                if (busStop == null)
                {
                    return NotFound($"Przystanek autobusowy o ID {id} nie został znaleziony.");
                }

                return Ok(busStop);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostBusStop([FromBody] string busStopName)
        {
            if (string.IsNullOrWhiteSpace(busStopName))
            {
                return BadRequest("Podaj nazwę przystanku, ona nie może być pusta.");
            }

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "INSERT INTO bus_stops (name) VALUES (@Name)";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", busStopName);

                        // Wykonujemy zapytanie, ale nie musimy zwracać żadnego rezultatu
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Zwracamy status Created, ale bez zawartości
                return Ok(new { message = "Przystanek dodany" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBusStop(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "DELETE FROM bus_stops WHERE id = @Id";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        var result = await command.ExecuteNonQueryAsync();

                        if (result == 0)
                        {
                            return NotFound($"Nie znaleziono przystanku autobusowego z ID: {id}");
                        }
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBusStop(int id, [FromBody] BusStop updatedBusStop)
        {
            if (string.IsNullOrWhiteSpace(updatedBusStop.name))
            {
                return BadRequest("Nowa nazwa przystanku nie może być pusta.");
            }

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "UPDATE bus_stops SET name = @Name WHERE id = @Id";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", updatedBusStop.name);
                        command.Parameters.AddWithValue("@Id", id);

                        var rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound($"Przystanek autobusowy o ID {id} nie został znaleziony.");
                        }
                    }
                }

                return Ok(new { message = "Przystanek zaktualizowany pomyślnie." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

    }
}
