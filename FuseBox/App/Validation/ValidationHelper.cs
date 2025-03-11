using System.ComponentModel.DataAnnotations;

namespace FuseBox
{
    public static class ValidationHelper
    {
        public static IList<ValidationResult> Validate(object obj)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(obj, null, null);

            // Валидируем текущий объект
            Validator.TryValidateObject(obj, context, results, true);

            // Теперь ищем свойства классов, чтобы провалидировать вложенные объекты
            var properties = obj.GetType().GetProperties()
                .Where(prop => prop.CanRead && prop.GetIndexParameters().Length == 0)
                .ToList();

            foreach (var property in properties)
            {
                var value = property.GetValue(obj);

                if (value == null)
                    continue;

                // Если это коллекция - валидируем каждый элемент
                if (value is IEnumerable<object> enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        results.AddRange(Validate(item));
                    }
                }
                // Если это класс (но не string), валидируем объект
                else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    results.AddRange(Validate(value));
                }
            }

            return results;
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