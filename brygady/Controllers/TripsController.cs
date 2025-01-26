using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Brygady.Data;

namespace Brygady.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly string _connectionString;

        public TripsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet("GetMaxTripId")]
        public async Task<ActionResult<int>> GetMaxTripId()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var query = "SELECT MAX(id) FROM trips;";

                using var command = new NpgsqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                if (result == DBNull.Value || result == null)
                {
                    return Ok(0);
                }

                return Ok(Convert.ToInt32(result));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetTrips")]
        public async Task<ActionResult<IEnumerable<TripDetailsDto>>> GetTrips()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var query = @"
                    SELECT DISTINCT
                        t.id AS trip_id,
                        l.number AS line_number,
                        start_stop.name AS start_stop,
                        end_stop.name AS end_stop,
                        first_time.arrival_departure_time AS start_time,
                        last_time.arrival_departure_time AS end_time
                    FROM 
                        trips t
                    JOIN
                        trip_times first_time ON t.id = first_time.trip_id
                    JOIN
                        line_stops first_line_stop ON first_time.line_stop_id = first_line_stop.id
                    JOIN
                        bus_stops start_stop ON first_line_stop.stop_id = start_stop.id
                    JOIN
                        trip_times last_time ON t.id = last_time.trip_id
                    JOIN
                        line_stops last_line_stop ON last_time.line_stop_id = last_line_stop.id
                    JOIN
                        bus_stops end_stop ON last_line_stop.stop_id = end_stop.id
                    JOIN
                        lines l ON first_line_stop.line_id = l.id
                    WHERE
                        first_time.arrival_departure_time = (
                            SELECT MIN(arrival_departure_time)
                            FROM trip_times
                            WHERE trip_id = t.id
                        )
                        AND last_time.arrival_departure_time = (
                            SELECT MAX(arrival_departure_time)
                            FROM trip_times
                            WHERE trip_id = t.id
                        );

                    ";

                using var command = new NpgsqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                //Console.Write(reader);
                var trips = new List<TripDetailsDto>();

                while (await reader.ReadAsync())
                {
                    var trip = new TripDetailsDto
                    {
                        TripId = reader.GetInt32(reader.GetOrdinal("trip_id")),
                        LineNumber = reader.GetString(reader.GetOrdinal("line_number")),
                        StartStop = reader.GetString(reader.GetOrdinal("start_stop")),
                        EndStop = reader.GetString(reader.GetOrdinal("end_stop")),
                        StartTime = reader.GetTimeSpan(reader.GetOrdinal("start_time")),
                        EndTime = reader.GetTimeSpan(reader.GetOrdinal("end_time"))
                    };
                    trips.Add(trip);
                }
                Console.WriteLine(trips);
                return Ok(trips);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                return StatusCode(500, $"Wewnętrnzy błąd serwera: {ex.Message}");
            }
        }

        [HttpGet("GetTrips/{typeOfDayId}")]
        public async Task<ActionResult<IEnumerable<TripDetailsDto>>> GetTripsByTypeOfDay(int typeOfDayId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var query = @"
                    SELECT DISTINCT
                        t.id AS trip_id,
                        l.number AS line_number,
                        start_stop.name AS start_stop,
                        end_stop.name AS end_stop,
                        first_time.arrival_departure_time AS start_time,
                        last_time.arrival_departure_time AS end_time
                    FROM 
                        trips t
                    JOIN
                        trip_times first_time ON t.id = first_time.trip_id
                    JOIN
                        line_stops first_line_stop ON first_time.line_stop_id = first_line_stop.id
                    JOIN
                        bus_stops start_stop ON first_line_stop.stop_id = start_stop.id
                    JOIN
                        trip_times last_time ON t.id = last_time.trip_id
                    JOIN
                        line_stops last_line_stop ON last_time.line_stop_id = last_line_stop.id
                    JOIN
                        bus_stops end_stop ON last_line_stop.stop_id = end_stop.id
                    JOIN
                        lines l ON first_line_stop.line_id = l.id
                    WHERE
                        first_time.arrival_departure_time = (
                            SELECT MIN(arrival_departure_time)
                            FROM trip_times
                            WHERE trip_id = t.id
                        )
                        AND last_time.arrival_departure_time = (
                            SELECT MAX(arrival_departure_time)
                            FROM trip_times
                            WHERE trip_id = t.id
                        )
                        AND t.type_of_day_id = @typeOfDayId; 
                ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@typeOfDayId", typeOfDayId);

                using var reader = await command.ExecuteReaderAsync();
                var trips = new List<TripDetailsDto>();

                while (await reader.ReadAsync())
                {
                    var trip = new TripDetailsDto
                    {
                        TripId = reader.GetInt32(reader.GetOrdinal("trip_id")),
                        LineNumber = reader.GetString(reader.GetOrdinal("line_number")),
                        StartStop = reader.GetString(reader.GetOrdinal("start_stop")),
                        EndStop = reader.GetString(reader.GetOrdinal("end_stop")),
                        StartTime = reader.GetTimeSpan(reader.GetOrdinal("start_time")),
                        EndTime = reader.GetTimeSpan(reader.GetOrdinal("end_time"))
                    };
                    trips.Add(trip);
                }

                if (trips.Count == 0)
                {
                    return NotFound($"Nie znaleziono kursów dla typu dnia o ID: {typeOfDayId}.");
                }

                return Ok(trips);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

        [HttpGet("GetTripsLongerThan/{time}")]
        public async Task<ActionResult<IEnumerable<TripDetailsDto>>> GetTripsLongerThan(int time)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
               

                var query = @"
                    SELECT DISTINCT
                        t.id AS trip_id,
                        l.number AS line_number,
                        start_stop.name AS start_stop,
                        end_stop.name AS end_stop,
                        first_time.arrival_departure_time AS start_time,
                        last_time.arrival_departure_time AS end_time
                    FROM 
                        trips t
                    JOIN trip_times first_time ON t.id = first_time.trip_id
                    JOIN line_stops first_line_stop ON first_time.line_stop_id = first_line_stop.id
                    JOIN bus_stops start_stop ON first_line_stop.stop_id = start_stop.id
                    JOIN trip_times last_time ON t.id = last_time.trip_id
                    JOIN line_stops last_line_stop ON last_time.line_stop_id = last_line_stop.id
                    JOIN bus_stops end_stop ON last_line_stop.stop_id = end_stop.id
                    JOIN lines l ON first_line_stop.line_id = l.id
                    WHERE
                        first_time.arrival_departure_time = (
                            SELECT MIN(arrival_departure_time)
                            FROM trip_times
                            WHERE trip_id = t.id
                        )
                        AND last_time.arrival_departure_time = (
                            SELECT MAX(arrival_departure_time)
                            FROM trip_times
                            WHERE trip_id = t.id
                        )
                        -- TUTAJ kluczowy warunek:
                        AND (last_time.arrival_departure_time - first_time.arrival_departure_time)
                            > (@time || ' minutes')::interval
                ";

                // Dodajemy parametr @time
                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@time", time);

                using var reader = await command.ExecuteReaderAsync();
                var trips = new List<TripDetailsDto>();

                while (await reader.ReadAsync())
                {
                    var trip = new TripDetailsDto
                    {
                        TripId = reader.GetInt32(reader.GetOrdinal("trip_id")),
                        LineNumber = reader.GetString(reader.GetOrdinal("line_number")),
                        StartStop = reader.GetString(reader.GetOrdinal("start_stop")),
                        EndStop = reader.GetString(reader.GetOrdinal("end_stop")),
                        StartTime = reader.GetTimeSpan(reader.GetOrdinal("start_time")),
                        EndTime = reader.GetTimeSpan(reader.GetOrdinal("end_time"))
                    };
                    trips.Add(trip);
                }

                if (trips.Count == 0)
                {
                    return NotFound($"Nie znaleziono kursów dłuższych niż {time} minut.");
                }

                return Ok(trips);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }

        [HttpPost("PostTrip")]
        public async Task<ActionResult> PostTrip([FromBody] NewTripDto newTrip)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var query = @"
                    INSERT INTO trips (id, type_of_day_id) 
                    VALUES (@Id, @TypeOfDayId);
                ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", newTrip.Id); 
                command.Parameters.AddWithValue("@TypeOfDayId", newTrip.TypeOfDayId); 

                await command.ExecuteNonQueryAsync();

                return CreatedAtAction(nameof(PostTrip), new { tripId = newTrip.Id }, newTrip.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }


        [HttpGet("GetDepartures")]
        public async Task<ActionResult<IEnumerable<DepartureTimesDto>>> GetDepartures(int stopId, int time, int typeOfDayId)
        {
            var currentTime = DateTime.Now; 
            var endTime = currentTime.AddMinutes(time); 
            
            var currentTimeSpan = currentTime.TimeOfDay;
            var endTimeSpan = endTime.TimeOfDay;

            TimeSpan midnight = new TimeSpan(0, 0, 0);
            TimeSpan lastMoment = new TimeSpan(23, 59, 59);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                string query;

                
                if (endTime.Date > currentTime.Date) 
                {
                    query = @"
                    SELECT DISTINCT
                        t.id AS trip_id,
                        start_stop.name AS start_stop,
                        end_stop.name AS end_stop,
                        first_time.arrival_departure_time AS start_time, 
                        last_time.arrival_departure_time AS end_time,
                        line.number AS line_number
                    FROM 
                        trips t
                    JOIN
                        trip_times first_time ON t.id = first_time.trip_id
                    JOIN
                        line_stops first_line_stop ON first_time.line_stop_id = first_line_stop.id
                    JOIN
                        bus_stops start_stop ON first_line_stop.stop_id = start_stop.id
                    JOIN
                        trip_times last_time ON t.id = last_time.trip_id
                    JOIN
                        line_stops last_line_stop ON last_time.line_stop_id = last_line_stop.id
                    JOIN
                        bus_stops end_stop ON last_line_stop.stop_id = end_stop.id
                    JOIN
                        lines line ON first_line_stop.line_id = line.id
                    WHERE 
                        first_line_stop.stop_id = @StopId
                        AND (
                            (first_time.arrival_departure_time >= @CurrentTime AND first_time.arrival_departure_time <= @LastMoment) OR
                            (first_time.arrival_departure_time >= @Midnight AND first_time.arrival_departure_time <= @EndTime)
                        )
                        AND t.type_of_day_id = @TypeOfDayId
                        AND start_stop.id != end_stop.id  
                        AND last_time.arrival_departure_time = (
                            SELECT MAX(tt.arrival_departure_time)
                            FROM trip_times tt
                            WHERE tt.trip_id = t.id
                        )
                    ORDER BY 
                        first_time.arrival_departure_time;

                                    ";

                                        
                                    }
                                    else 
                                    {
                                        query = @"
                                            SELECT DISTINCT
                        t.id AS trip_id,
                        start_stop.name AS start_stop,
                        end_stop.name AS end_stop,
                        first_time.arrival_departure_time AS start_time, 
                        last_time.arrival_departure_time AS end_time,
                        line.number AS line_number
                    FROM 
                        trips t
                    JOIN
                        trip_times first_time ON t.id = first_time.trip_id
                    JOIN
                        line_stops first_line_stop ON first_time.line_stop_id = first_line_stop.id
                    JOIN
                        bus_stops start_stop ON first_line_stop.stop_id = start_stop.id
                    JOIN
                        trip_times last_time ON t.id = last_time.trip_id
                    JOIN
                        line_stops last_line_stop ON last_time.line_stop_id = last_line_stop.id
                    JOIN
                        bus_stops end_stop ON last_line_stop.stop_id = end_stop.id
                    JOIN
                        lines line ON first_line_stop.line_id = line.id
                    WHERE 
                        first_line_stop.stop_id = @StopId
                        AND first_time.arrival_departure_time >= @CurrentTime
                        AND first_time.arrival_departure_time <= @EndTime
                        AND t.type_of_day_id = @TypeOfDayId
                        AND start_stop.id != end_stop.id  
                        AND last_time.arrival_departure_time = (
                            SELECT MAX(tt.arrival_departure_time)
                            FROM trip_times tt
                            WHERE tt.trip_id = t.id
                        )
                    ORDER BY 
                        first_time.arrival_departure_time;
                    ";

                }

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@StopId", stopId);
                command.Parameters.AddWithValue("@CurrentTime", currentTimeSpan);
                command.Parameters.AddWithValue("@EndTime", endTimeSpan);
                command.Parameters.AddWithValue("@Midnight", midnight);
                command.Parameters.AddWithValue("@LastMoment", lastMoment);
                command.Parameters.AddWithValue("@TypeOfDayId", typeOfDayId);

                using var reader = await command.ExecuteReaderAsync();
                
                var departures = new List<DepartureTimesDto>();

                while (await reader.ReadAsync())
                {
                    var departure = new DepartureTimesDto
                    {
                        departureTime = reader.GetTimeSpan(reader.GetOrdinal("start_time")),    
                        direction = reader.GetString(reader.GetOrdinal("end_stop")),
                        lineNumber = reader.GetString(reader.GetOrdinal("line_number"))
                    };
                    departures.Add(departure);
                }

                var departuresTimes = departures
                    .GroupBy(d => new { d.departureTime, d.direction })  
                    .Select(g => g.First()) 
                    .ToList();

                return Ok(departuresTimes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                return StatusCode(500, $"Wewnętrzny błąd serwera: {ex.Message}");
            }
        }
    }
}
