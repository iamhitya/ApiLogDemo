using ApiLogDemo.Models;
using ApiLogDemo.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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

        #endregion

        [HttpGet]
        public IActionResult GetAll() => LogAndExecute(() =>
        {
            var peoples = _peopleService.GetAll();
            return Ok(peoples);
        }, nameof(GetAll));

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
