using Microsoft.AspNetCore.Mvc;

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
                if(input.InitialSettings.PhasesCount == 1 || input.InitialSettings.PhasesCount == 3) {
                    ConfigurationService configurationService = new ConfigurationService();
                    var pc = configurationService.GenerateConfiguration(input); // Эта часть уходин в игнор

                    return Ok(input.Shield.Fuses);                              // Вывожу только список предохранителей
                }
                return BadRequest($"Wrong phases number (You entered {input.InitialSettings.PhasesCount}).");
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