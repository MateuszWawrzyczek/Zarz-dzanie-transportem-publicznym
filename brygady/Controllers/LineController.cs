using Microsoft.AspNetCore.Mvc;
using Npgsql;


namespace Brygady.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LineController : ControllerBase
    {
        private readonly string _connectionString;

        public LineController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Line>>> GetLines()
        {
            var linesList = new List<Line>();

            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    return StatusCode(500, "Connection string is not configured.");
                }

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT id, number FROM lines ORDER BY number ASC";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                var line = new Line
                                {
                                    Id = reader.GetInt32(0),
                                    Number = reader.GetString(1) 
                                };

                                linesList.Add(line);
                            }
                            catch (Exception ex)
                            {   
                                return StatusCode(500, $"Error processing record: {ex.Message}");
                            }
                        }
                    }
                }

                if (linesList.Count == 0)
                {
                    return NotFound("Nie dodałeś jeszcze żadnej linii.");
                }

                return Ok(linesList);
            }
            catch (NpgsqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
            public async Task<ActionResult<Line>> GetLineNumber(int id)
            {
                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                    
                        var query = "SELECT number from lines where id = @id";


                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", id);

                            var result = await command.ExecuteScalarAsync();
                            if(result != null)
                            {
                                var lineNumber = result.ToString();
                                return Ok(lineNumber);
                            }
                            else{
                                return NotFound("Nie znaleziono numeru linii z id {Id}.");
                            }
                        }

                        
                    }
                }
            
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<ActionResult<Line>> PostLine(string numberOfLine)
        {
            if (string.IsNullOrWhiteSpace(numberOfLine))
            {
                return BadRequest("Podaj numer linii, nie może być pusty.");
            }

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "INSERT INTO lines (number) VALUES (@Number) RETURNING id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Number", numberOfLine);

                        var result = await command.ExecuteScalarAsync();
                        if (result == null)
                        {
                            throw new InvalidOperationException("Nie udało się wstawić danych.");
                        }

                        var newLine = new Line
                        {
                            Id = (int)result,
                            Number = numberOfLine
                        };

                        return CreatedAtAction(nameof(GetLines), new { id = newLine.Id }, newLine);
                    }
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Conflict($"Numer linii '{numberOfLine}' już istnieje. Dwie linie nie mogą mieć tej samej nazwy.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<Line>> PutLine(int id, string newNumber)
        {
            if (string.IsNullOrWhiteSpace(newNumber))
            {
                return BadRequest("Podaj nazwę(numer) linii, ona nie może być pusta.");
            }

        try
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var query = "UPDATE lines SET number = @NewNumber WHERE id = @Id RETURNING id, number";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NewNumber", newNumber);
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var updatedTypeOfDay = new Line
                            {
                                Id = reader.GetInt32(0),
                                Number = reader.GetString(1)
                            };

                            return Ok(updatedTypeOfDay);
                        }
                        else
                        {
                            return NotFound($"Nie znaleziono linii z {id}.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
        }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLine(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "DELETE FROM lines WHERE id = @Id";
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
    
    }

    public class Line
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;

        public Line() { }

        public Line(string number)
        {
            Number = number ?? throw new ArgumentNullException(nameof(number));
        }
    }

}
