using ApiLogDemo.Models;
using ApiLogDemo.Results;
using ApiLogDemo.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ApiLogDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PeopleController : ControllerBase
    {
        private readonly PeopleService _peopleService;
        private readonly ApiLogService _logService;

        public PeopleController(PeopleService peopleService,
            ApiLogService logService)
        {
            _peopleService = peopleService;
            _logService = logService;
        }

        #region Private Methods

        private IActionResult LogAndExecute(Func<IActionResult> action, string methodName)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = action();
            stopwatch.Stop();

            double recordCount = 0;
            if (result is ObjectResult obj && obj.Value is not null)
            {
                // fast path for collections that expose Count
                if (obj.Value is System.Collections.ICollection coll)
                {
                    recordCount = coll.Count;
                }
                // fallback for IEnumerable (exclude string which is IEnumerable<char>)
                else if (obj.Value is System.Collections.IEnumerable en && obj.Value is not string)
                {
                    recordCount = en.Cast<object>().Count();
                }
            }

            _logService.AddLog(new ApiLog
            {
                Method = methodName,
                Endpoint = HttpContext.Request.Path,
                Timestamp = DateTime.UtcNow,
                RecordCount = recordCount,
                ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
            });

            return result;
        }

        private async Task LogAndExecuteAsync(Func<Task<double>> action, string methodName)
        {
            var stopwatch = Stopwatch.StartNew();
            double recordCount = 0;

            try
            {
                recordCount = await action();
            }
            catch (Exception ex)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync("An error occurred while processing your request.");
                return;
            }
            finally
            {
                stopwatch.Stop();

                _logService.AddLog(new ApiLog
                {
                    Method = methodName,
                    Endpoint = HttpContext.Request.Path,
                    Timestamp = DateTime.UtcNow,
                    RecordCount = recordCount,
                    ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                });
            }
        }

        #endregion

        [HttpGet]
        public IActionResult GetAll() => LogAndExecute(() =>
        {
            var peoples = _peopleService.GetAll();
            return Ok(peoples);
        }, nameof(GetAll));

        /// <summary>
        /// The response starts sending immediately — no waiting for all records to load.
        /// Each serialized record is written and flushed right away.
        /// The browser or client receives chunks as they are written.
        /// </summary>
        /// <returns></returns>
        [HttpGet("stream")]
        public async Task GetAllStreamAsync()
        {
            // Important: Do not write headers after the body starts
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            Response.Headers["Transfer-Encoding"] = "chunked";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.Headers["Content-Type"] = "application/json; charset=utf-8";

            try
            {
                await using var writer = new Utf8JsonWriter(Response.BodyWriter, new JsonWriterOptions
                {
                    Indented = false
                });

                writer.WriteStartArray();
                await writer.FlushAsync();
                await Response.Body.FlushAsync(); // ensure headers are committed early

                await foreach (var person in _peopleService.GetAllStreamAsync())
                {
                    JsonSerializer.Serialize(writer, person);
                    writer.Flush(); // flush JSON buffer
                    await Response.Body.FlushAsync(); // push chunk to network
                }

                writer.WriteEndArray();
                await writer.FlushAsync();
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                // Log to console or file; do not return any HTTP response after writing starts
                Console.WriteLine("Streaming error: " + ex);
            }
        }

        [HttpGet("download")]
        public IActionResult Download() => LogAndExecute(() =>
        {
            var peoples = _peopleService.GetAll();
            var json = JsonSerializer.Serialize(peoples);
            var bytes = Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", "people.json");
        }, nameof(Download));

        [HttpGet("download-stream")]
        public IActionResult DownloadStream() => LogAndExecute(() =>
        {
            // Don't load all data into a serialized string
            var peoples = _peopleService.GetAllStreamAsync();

            // Return streamed response
            return new FileCallbackResult("application/json", async (outputStream, _) =>
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultBufferSize = 16 * 1024 // 16 KB buffer
                };

                // Stream directly to response
                await JsonSerializer.SerializeAsync(outputStream, peoples, options);
                await outputStream.FlushAsync();
            })
            {
                FileDownloadName = "people.json"
            };
        }, nameof(DownloadStream));

        [HttpGet("{id}")]
        public IActionResult GetById(int id) => LogAndExecute(() =>
        {
            var person = _peopleService.GetById(id);
            if (person == null)
                return NotFound(new { message = "Person not found." });
            return Ok(person);
        }, nameof(GetById));

        [HttpPost]
        public IActionResult Add([FromBody] Person person) => LogAndExecute(() =>
        {
            _peopleService.Add(person);
            return Ok(new { message = "Person added successfully." });
        }, nameof(Add));

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Person person) => LogAndExecute(() =>
        {
            var updated = _peopleService.Update(id, person);
            if (!updated)
                return NotFound(new { message = "Person not found." });
            return Ok(new { message = "Person updated successfully." });
        }, nameof(Update));

        [HttpDelete("{id}")]
        public IActionResult Delete(int id) => LogAndExecute(() =>
        {
            var deleted = _peopleService.Delete(id);
            if (!deleted)
                return NotFound(new { message = "Person not found." });
            return Ok(new { message = "Person deleted successfully." });
        }, nameof(Delete));

        [HttpPost("seed/{count}")]
        public IActionResult Seed(int count) => LogAndExecute(() =>
        {
            if (count <= 0)
                return BadRequest(new { message = "Count must be greater than zero." });

            var firstNames = new[] { "Alex", "Sam", "Taylor", "Jordan", "Casey", "Morgan", "Riley", "Jamie", "Dakota", "Avery", "Amy", "Arton", "Vicky", "Jordan", "Peter" };
            var lastNames = new[] { "Smith", "Johnson", "Brown", "Williams", "Jones", "Garcia", "Miller", "Davis", "Wilson", "Anderson", "Braga", "Solvin", "Lee", "Joe" };
            var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };

            var rng = Random.Shared;
            var toAdd = new List<Person>(count);

            for (int i = 0; i < count; i++)
            {
                var name = $"{firstNames[rng.Next(firstNames.Length)]} {lastNames[rng.Next(lastNames.Length)]}";
                var age = rng.Next(18, 81);
                var city = cities[rng.Next(cities.Length)];

                toAdd.Add(new Person
                {
                    Name = name,
                    Age = age,
                    City = city,
                });
            }

            _peopleService.AddRange(toAdd);

            return Ok(new { message = $"{toAdd.Count} people added.", added = toAdd.Count });
        }, nameof(Seed));
    }
}
