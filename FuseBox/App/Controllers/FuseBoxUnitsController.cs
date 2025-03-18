using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using FuseBox;
using FuseBox.App.DataBase;
using FuseBox.App.Models.DTO;
using FuseBox.FuseBox;
using Microsoft.EntityFrameworkCore;
using FuseBox.App.Models;
using AutoMapper;
using System;
using FuseBox.App.Models.DTO.ConfugurationDTO;


namespace FuseBox.Controllers
{

    public class FuseBoxUnitProfile : Profile
    {
        public FuseBoxUnitProfile()
        {
            // Маппинг из DTO в Entity
            CreateMap<FuseBoxUnitDTO, FuseBoxUnit>()
                .ForMember(dest => dest.Project, opt => opt.Ignore()); // Игнорируем Project, если он не приходит из DTO

            // Маппинг из Entity в DTO
            CreateMap<FuseBoxUnit, FuseBoxUnitDTO>();
        }
    }


    public class ProjectProfile : Profile
    {
        public ProjectProfile()
        {
            //  DTO to Entity

            CreateMap<ProjectDTO, Project>()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.FuseBox, opt => opt.MapFrom(src => src.FuseBox))
                .ForMember(dest => dest.FloorGrouping, opt => opt.MapFrom(src => src.FloorGrouping))
                .ForMember(dest => dest.GlobalGrouping, opt => opt.MapFrom(src => src.GlobalGrouping))
                .ForMember(dest => dest.Floors, opt => opt.MapFrom(src => src.Floors));

            CreateMap<FuseBoxUnitDTO, FuseBoxUnit>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components))
                .ForMember(dest => dest.CableConnections, opt => opt.MapFrom(src => src.CableConnections));

            CreateMap<FuseBoxComponentGroupDTO, FuseBoxComponentGroup>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components));

            CreateMap<ConnectionDTO, Connection>();

            CreateMap<InitialSettingsDTO, InitialSettings>();
            CreateMap<FloorGroupingDTO, FloorGrouping>();
            CreateMap<GlobalGroupingDTO, GlobalGrouping>();

            CreateMap<FloorDTO, Floor>()
                .ForMember(dest => dest.Rooms, opt => opt.MapFrom(src => src.Rooms));

            CreateMap<RoomDTO, Room>()
                .ForMember(dest => dest.Consumer, opt => opt.MapFrom(src => src.Consumer));

            CreateMap<ConsumerDTO, Consumer>();

            // Entity to DTO

            CreateMap<Project, ProjectDTO>()
                .ForMember(dest => dest.FuseBox, opt => opt.MapFrom(src => src.FuseBox))
                .ForMember(dest => dest.FloorGrouping, opt => opt.MapFrom(src => src.FloorGrouping))
                .ForMember(dest => dest.GlobalGrouping, opt => opt.MapFrom(src => src.GlobalGrouping))
                .ForMember(dest => dest.Floors, opt => opt.MapFrom(src => src.Floors));

            CreateMap<FuseBoxUnit, FuseBoxUnitDTO>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components))
                .ForMember(dest => dest.CableConnections, opt => opt.MapFrom(src => src.CableConnections));

            CreateMap<FuseBoxComponentGroup, FuseBoxComponentGroupDTO>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components));

            CreateMap<Connection, ConnectionDTO>();

            CreateMap<InitialSettings, InitialSettingsDTO>();
            CreateMap<FloorGrouping, FloorGroupingDTO>();
            CreateMap<GlobalGrouping, GlobalGroupingDTO>();

            CreateMap<Floor, FloorDTO>()
                .ForMember(dest => dest.Rooms, opt => opt.MapFrom(src => src.Rooms));

            CreateMap<Room, RoomDTO>()
                .ForMember(dest => dest.Consumer, opt => opt.MapFrom(src => src.Consumer));

            CreateMap<Consumer, ConsumerDTO>();
        }
    }

    [ApiController]
    [Route("")]
    public class FuseBoxUnitsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        public FuseBoxUnitsController(AppDbContext context)
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Project, ProjectDTO>()
                    .ForMember(dest => dest.FuseBox, opt => opt.Ignore());  // Игнорируем ссылку на FuseBox, чтобы избежать цикла

                cfg.AddProfile<ProjectProfile>();
            }).CreateMapper();
            _context = context;
        }

        //works with adequate input data, but somthing wrong with actual calculations or logic
        [HttpPost("calculation")]// POST request for    http://localhost:5133/calculation
        public IActionResult CalculateFuseBox([FromBody] ProjectDTO dto)
        {

            // Проверка входных данных
            if (dto == null) return BadRequest("Проект не передан вообще.");
            if (dto.FuseBox == null) return BadRequest("FuseBox пустой.");
            if (dto.Floors == null || !dto.Floors.Any()) return BadRequest("Нет этажей.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Находим или создаём пользователя
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == "test@example.com");

                if (existingUser == null)
                {
                    existingUser = new User { Email = "test@example.com" };
                    _context.Users.Add(existingUser);
                    _context.SaveChanges();
                }

                // Маппим проект
                var project = _mapper.Map<Project>(dto);  // Используем AutoMapper для преобразования

                // Привязываем пользователя
                project.User = existingUser;

                project.InitialSettings = _mapper.Map<InitialSettings>(dto.InitialSettings);
                project.InitialSettings.Project = project; // Связь руками при необходимости


                ///////////////////////////////////////////////////////////////////////////////////////////////////
                // Работа с БД: FuseBox


                // FuseBoxUnit
                var existingFuseBox = _context.FuseBoxes
                      .FirstOrDefault(fb => fb.Id == dto.FuseBox.Id);

                if (existingFuseBox == null)
                {
                    project.FuseBox.Project = project;
                    _context.FuseBoxes.Add(project.FuseBox);
                }
                else
                {
                    _mapper.Map(dto.FuseBox, existingFuseBox);
                    _context.Attach(existingFuseBox);
                    _context.Entry(existingFuseBox).State = EntityState.Modified;

                    project.FuseBox = existingFuseBox;
                }


                ///////////////////////////////////////////////////////////////////////////////////////////////////
                /// Схранение в базу и отправка во Фронт-энд

                // Сохранение в базу данных используя Сущности
                _context.Projects.Add(project);
                _context.SaveChanges();

                // Маппишь обратно в DTO после сохранения
                var resultDto = _mapper.Map<ProjectDTO>(project);

                // Логируем данные после сохранения
                Console.WriteLine("Project saved successfully.");

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                // Возвращаем результат во Фронт-энд
                var data = JsonConvert.SerializeObject(resultDto, Formatting.Indented);
                return Content(data, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during project processing: " + ex.Message);
                // Логируем полное исключение
                //Console.WriteLine("Full exception: " + JsonConvert.SerializeObject(ex, Formatting.Indented));

                var fullError = GetFullError(ex);
                Console.WriteLine(fullError); // В логи
                return BadRequest($"Deserializ error: {fullError}");
            }
        }



        private string GetFullError(Exception ex)
        {
            var messages = new List<string>();
            while (ex != null)
            {
                messages.Add(ex.Message);
                ex = ex.InnerException;
            }
            return string.Join(" --> ", messages);
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





//// FloorGrouping
//project.FloorGrouping = new FloorGrouping
//{
//    IndividualFloorGrouping = dto.FloorGrouping.IndividualFloorGrouping,
//    SeparateUZOPerFloor = dto.FloorGrouping.SeparateUZOPerFloor,
//    Project = project
//};
//project.GlobalGrouping = new GlobalGrouping
//{
//    Sockets = dto.GlobalGrouping.Sockets,
//    Lighting = dto.GlobalGrouping.Lighting,
//    Conditioners = dto.GlobalGrouping.Conditioners,
//    Project = project
//};

//// Floors
//var existingFloor = _context.Floors
//    .FirstOrDefault(f => f.Id == dto.Floors[0].Id);

//if (existingFloor == null)
//{
//    // Добавляем новый этаж
//    project.Floors = dto.Floors.Select(floorDto =>
//    {
//        var floor = new Floor
//        {
//            Name = floorDto.Name,
//            Project = project,
//            Rooms = new List<Room>()
//        };

//        floor.Rooms = floorDto.Rooms.Select(roomDto =>
//        {
//            var room = new Room
//            {
//                Name = roomDto.Name,
//                Floor = floor,
//                Consumer = new List<Consumer>()
//            };

//            room.Consumer = roomDto.Consumer.Select(consumerDto => new Consumer
//            {
//                Name = consumerDto.Name,
//                Amper = consumerDto.Amper,
//                FuseBoxUnit = project.FuseBox // <---- или твоя логика
//            }).ToList();

//            return room;
//        }).ToList();

//        return floor;
//    }).ToList();
//}
//else
//{
//    // Обновляем существующий этаж
//    _context.Entry(existingFloor).CurrentValues.SetValues(dto.Floors[0]);
//}



//// Проверка входных данных
//var validationResults = ValidationHelper.Validate(project);

//if (validationResults.Count == 0)
//{
//    Console.WriteLine("Validation was successful!");
//    //Console.WriteLine($"Id: {user.Id}, Name: {user.Name}, Age: {user.Age}");
//}
//else
//{
//    Console.WriteLine("Validation error:");
//    foreach (var validationResult in validationResults)
//    {
//        Console.WriteLine($" - {validationResult.ErrorMessage}");
//    }
//}

//var configurationService = new ConfigurationService(project);  // Создаю объект сервиса
//configurationService.GenerateConfiguration();                    // Модифицирую конфигурацию входного объекта





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