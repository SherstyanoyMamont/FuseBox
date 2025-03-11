using System.ComponentModel.DataAnnotations;

namespace FuseBox
{
    public class AllowedValuesAttribute : ValidationAttribute
    {
        private readonly int[] _allowed;

        public AllowedValuesAttribute(params int[] allowed)
        {
            _allowed = allowed;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return false; // Validation framework подтянет сообщение об ошибке
            }

            if (!_allowed.Contains((int)value))
            {
                return false;
            }

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            // Если пользователь указал ErrorMessage => используем его
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                return ErrorMessage;
            }

            // Иначе дефолтное сообщение
            return $"{name} должно быть одним из значений: {string.Join(", ", _allowed)}";
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