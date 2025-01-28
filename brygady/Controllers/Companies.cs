using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Brygady.Data;


namespace Brygady.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly string _connectionString;

        public CompaniesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet()]
        public async Task<IActionResult> GetAllCompanies()
        {
            try
            {
                var companies = new List<Company>();

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT id, name, phone, email FROM Company";
                    try
                    {
                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    companies.Add(new Company
                                    {
                                        Id = reader.GetInt32(0),
                                        Name = reader.GetString(1),
                                        Phone = reader.GetString(2),
                                        Email = reader.GetString(3)
                                    });
                                }
                            }
                        }
                    }catch (Exception ex)
                    {
                        Console.WriteLine($"Wystąpił błąd: {ex.Message}");
                        return BadRequest($"Wystąpił błąd: {ex.Message}");
                    }
                }

                return Ok(companies);
            }
            catch (Exception ex)
            {
                return BadRequest($"Wystąpił błąd: {ex.Message}");
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddCompany([FromBody] Company company)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "INSERT INTO Company (name, phone, email) VALUES (@name, @phone, @email)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", company.Name);
                        command.Parameters.AddWithValue("@phone", company.Phone);
                        command.Parameters.AddWithValue("@email", company.Email);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Firma została dodana.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Wystąpił błąd: {ex.Message}");
            }
        }

        [HttpPut("edit/{id}")]
        public async Task<IActionResult> EditCompany(int id, [FromBody] Company company)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "UPDATE Company SET name = @name, phone = @phone, email = @email WHERE id = @id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@name", company.Name);
                        command.Parameters.AddWithValue("@phone", company.Phone);
                        command.Parameters.AddWithValue("@email", company.Email);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Firma została zaktualizowana.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Wystąpił błąd: {ex.Message}");
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "DELETE FROM Company WHERE id = @id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Firma została usunięta.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Wystąpił błąd: {ex.Message}");
            }
        }
    }
}
