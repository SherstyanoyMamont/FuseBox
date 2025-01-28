using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace FuseBox.Controllers
{
    [ApiController]
    [Route("")]
    public class ConfigurationController : ControllerBase
    {

        //works with adequate input data, but somthing wrong with actual calculations or logic
        [HttpPost("calculation")]// POST request for    http://localhost:5133/calculation
        public IActionResult CalculateFuseBox([FromBody] Project input)
        {
            try
            {
                ConfigurationService configurationService = new();            // Создаю объект сервиса
                var pc = configurationService.GenerateConfiguration(input);   // Модифицирую конфигурацию входного объекта

                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto                  // Для поддержки полиморфизма
                };

                var data = JsonConvert.SerializeObject(pc, Formatting.Indented);

                return Ok(data);                 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //just to test get request      http://localhost:5133/test
        [HttpGet("test")]
        public IActionResult Test()
        {
            string name = System.Environment.UserName;

            return Ok($"Hello, {name};)");
        }
    }
}









//[ApiController]
//[Route("[controller]")]
//public class WeatherForecastController : ControllerBase
//{
//    private static readonly string[] Summaries = new[]
//    {
//        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//    };

//    private readonly ILogger<WeatherForecastController> _logger;

//    public WeatherForecastController(ILogger<WeatherForecastController> logger)
//    {
//        _logger = logger;
//    }

//    [HttpGet(Name = "GetWeatherForecast")]
//    public IEnumerable<WeatherForecast> Get()
//    {
//        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
//        {
//            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            TemperatureC = Random.Shared.Next(-20, 55),
//            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
//        })
//        .ToArray();
//    }
//}