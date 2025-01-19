using Microsoft.AspNetCore.Mvc;

namespace FuseBox.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Устанавливает маршрут для контроллера.
    public class ConfigurationController : ControllerBase
    {
        // Для хранения экземпляра службы ConfigurationService
        private readonly ConfigurationService _configurationService;

        // Конструктор контроллера
        public ConfigurationController(ConfigurationService configurationService)
        {
            _configurationService = configurationService;

        }

        // Атрибут, указывающий, что метод будет обрабатывать HTTP-запросы типа POST по маршруту api/configuration/generate.
        [HttpPost(Name = "GetProjectConfig")]

        // Mетод GenerateConfiguration. Он принимает объект ProjectConfiguration, который передается в теле запроса ([FromBody]) и возвращает объект типа IActionResult (результат выполнения действия).
        public IActionResult GenerateConfiguration([FromBody] Project config)
        {
            try
            {
                var result = _configurationService.GenerateConfiguration(config);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
}
