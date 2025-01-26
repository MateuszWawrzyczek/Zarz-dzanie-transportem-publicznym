using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace Brygady.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TypeOfDaysController : ControllerBase
    {
        private readonly string _connectionString;

        public TypeOfDaysController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TypeOfDays>>> GetTypeOfDays()
        {
            var typeOfDaysList = new List<TypeOfDays>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT id, name FROM types_of_days"; 

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            typeOfDaysList.Add(new TypeOfDays
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }

                if (typeOfDaysList.Count == 0)
                {
                    return NotFound("Nie dodałeś jeszcze żadnego typu dnia.");
                }

                return Ok(typeOfDaysList); // Zwracamy dane w formacie JSON
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<TypeOfDays>>> GetTypeOfDaysById(int id)
        {
            var typeOfDaysList = new List<TypeOfDays>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT id, name FROM types_of_days WHERE id = @id"; // Filtrowanie po id

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id); // Dodanie parametru do zapytania

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                typeOfDaysList.Add(new TypeOfDays
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1)
                                });
                            }
                        }
                    }
                }

                if (typeOfDaysList.Count == 0)
                {
                    return NotFound($"Nie znaleziono typu dnia o ID: {id}.");
                }

                return Ok(typeOfDaysList); // Zwracamy dane w formacie JSON
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("{name_of_type_of_days}")]
        public async Task<ActionResult<TypeOfDays>> PostTypeOfDay(string name_of_type_of_days)
        {
            int newId;

            if (string.IsNullOrWhiteSpace(name_of_type_of_days))
            {
                return BadRequest("Podaj nazwę typu dnia, ona nie może być pusta.");
            }

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "INSERT INTO types_of_days (name) VALUES (@Name) RETURNING id"; 

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name_of_type_of_days); 

                        var result = await command.ExecuteScalarAsync(); 
                        if (result == null)
                        {
                            throw new InvalidOperationException("Failed to insert data.");
                        }

                        newId = (int)result;
                    }
                }

                var newTypeOfDay = new TypeOfDays
                {
                    Id = newId,
                    Name = name_of_type_of_days
                };

                
                return CreatedAtAction(nameof(GetTypeOfDays), new { id = newId }, newTypeOfDay);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}"); 
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TypeOfDays>> PutTypeOfDay(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                return BadRequest("Podaj nazwę typu dnia, ona nie może być pusta.");
            }

        try
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var query = "UPDATE types_of_days SET name = @NewName WHERE id = @Id RETURNING id, name";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NewName", newName);
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var updatedTypeOfDay = new TypeOfDays
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            };

                            return Ok(updatedTypeOfDay);
                        }
                        else
                        {
                            return NotFound($"No type of day found with id {id}.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTypeOfDay(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "DELETE FROM types_of_days WHERE id = @Id";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        var result = await command.ExecuteNonQueryAsync();

                        if (result == 0)
                        {
                            return NotFound($"Nie znaleziono typu dnia z ID: {id}");
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

        [HttpGet("{id}/shortage-name")]
        public async Task<ActionResult<string>> GetShortageName(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT shortage_name FROM types_of_days WHERE id = @Id";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        var result = await command.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            return NotFound($"Nie znaleziono typu dnia z ID: {id} lub brak wartości shortage_name.");
                        }

                        var shortageName = result.ToString();
                        return Ok(shortageName); // Zwrócenie shortage_name w odpowiedzi
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

    }

    public class TypeOfDays
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public TypeOfDays() { }

        public TypeOfDays(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
