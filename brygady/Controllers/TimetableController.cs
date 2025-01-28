using Brygady.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;


namespace Brygady.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimetableController : ControllerBase
    {
        private readonly string _connectionString;

        public TimetableController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet("GetTimetable/{lineId}")]
        public async Task<ActionResult> GetTimetable(int lineId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var query = @"
                    SELECT 
                        ls.id AS stop_id,
                        ls.line_id,
                        ls.direction,
                        ls.""order"",
                        t.id AS trip_id,
                        t.type_of_day_id,
                        tt.id,
                        tt.arrival_departure_time

                    FROM line_stops ls
                    INNER JOIN trip_times tt ON ls.id = tt.line_stop_id
                    INNER JOIN trips t ON tt.trip_id = t.id
                    WHERE ls.line_id = @LineId AND tt.arrival_departure_time IS NOT NULL
                    ORDER BY ls.""order"", tt.arrival_departure_time;
                ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@LineId", lineId);

                using var reader = await command.ExecuteReaderAsync();

                var timetable = new List<dynamic>();

                while (await reader.ReadAsync())
                {
                    timetable.Add(new
                    {
                        StopId = reader.GetInt32(0),
                        LineId = reader.GetInt32(1),
                        Direction = reader.GetInt32(2),
                        Order = reader.GetInt32(3),
                        TripId = reader.GetInt32(4),
                        TypeOfDayId = reader.GetInt32(5),
                        TripTimeId = reader.GetInt32(6),
                        ArrivalDepartureTime = reader.GetTimeSpan(7).ToString(@"hh\:mm")
                    });
                }

                return Ok(timetable);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("SaveTimetable")]
        public async Task<ActionResult> SaveTimetable([FromBody] List<JsonElement> timetableData)
        {
            if (timetableData == null || !timetableData.Any())
            {
                return BadRequest("Timetable data is missing or empty.");
            }

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var tripIds = timetableData.Select(entry => entry.GetProperty("tripId").GetInt32()).Distinct().ToList();

                var checkTripsQuery = "SELECT id FROM trips WHERE id = ANY(@TripIds)";
                using (var checkCommand = new NpgsqlCommand(checkTripsQuery, connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@TripIds", tripIds.ToArray());
                    var reader = await checkCommand.ExecuteReaderAsync();
                    var existingTripIds = new HashSet<int>();

                    while (await reader.ReadAsync())
                    {
                        existingTripIds.Add(reader.GetInt32(0));
                    }

                    foreach (var entry in timetableData)
                    {
                        int tripId = entry.GetProperty("tripId").GetInt32();
                        if (!existingTripIds.Contains(tripId))
                        {
                            int typeOfDayId = entry.GetProperty("typeOfDayId").GetInt32();

                            var insertTripQuery = @"
                                INSERT INTO trips (id, type_of_day_id)
                                VALUES (@TripId, @TypeOfDayId)";
                            using (var insertCommand = new NpgsqlCommand(insertTripQuery, connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@TripId", tripId);
                                insertCommand.Parameters.AddWithValue("@TypeOfDayId", typeOfDayId);
                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                var insertTimeQuery = @"
                    INSERT INTO trip_times (id, trip_id, line_stop_id, arrival_departure_time)
                    VALUES (@TripTimeId, @TripId, @LineStopId, @ArrivalDepartureTime)
                    ON CONFLICT (id) 
                    DO UPDATE SET trip_id = EXCLUDED.trip_id, 
                                line_stop_id = EXCLUDED.line_stop_id,
                                arrival_departure_time = EXCLUDED.arrival_departure_time";

                var parametersList = new List<NpgsqlParameter[]>();

                foreach (var entry in timetableData)
                {
                    int stopId = entry.GetProperty("stopId").GetInt32();
                    int tripId = entry.GetProperty("tripId").GetInt32();
                    TimeSpan arrivalDepartureTime = TimeSpan.Parse(entry.GetProperty("arrivalDepartureTime").GetString());
                    int tripTimeId = entry.GetProperty("tripTimeId").GetInt32();

                    var parameters = new[]
                    {
                        new NpgsqlParameter("@TripTimeId", tripTimeId),
                        new NpgsqlParameter("@TripId", tripId),
                        new NpgsqlParameter("@LineStopId", stopId),
                        new NpgsqlParameter("@ArrivalDepartureTime", arrivalDepartureTime)
                    };
                    parametersList.Add(parameters);
                }

                foreach (var parameters in parametersList)
                {
                    using (var command = new NpgsqlCommand(insertTimeQuery, connection, transaction))
                    {
                        command.Parameters.AddRange(parameters);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
                return Ok("Timetable saved successfully!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
