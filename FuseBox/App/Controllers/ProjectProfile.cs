using FuseBox.App.Models.DTO;
using FuseBox.FuseBox;
using FuseBox.App.Models;
using AutoMapper;
using FuseBox.App.Models.DTO.ConfugurationDTO;
using FuseBox.App.Models.Shild_Comp;


namespace FuseBox.Controllers
{
    public class ProjectProfile : Profile
    {
        public ProjectProfile()
        {
            // ==============================
            //        DTO -> Entity
            // ==============================

            CreateMap<ProjectDTO, Project>()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.FuseBox, opt => opt.MapFrom(src => src.FuseBox))
                .ForMember(dest => dest.FloorGrouping, opt => opt.MapFrom(src => src.FloorGrouping))
                .ForMember(dest => dest.GlobalGrouping, opt => opt.MapFrom(src => src.GlobalGrouping))
                .ForMember(dest => dest.Floors, opt => opt.MapFrom(src => src.Floors));

            CreateMap<FuseBoxUnitDTO, FuseBoxUnit>()
                .ForMember(dest => dest.ComponentGroups, opt => opt.MapFrom(src => src.ComponentGroups))
                .ForMember(dest => dest.CableConnections, opt => opt.MapFrom(src => src.CableConnections));

            CreateMap<FuseBoxComponentGroupDTO, FuseBoxComponentGroup>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components));

            CreateMap<FloorDTO, Floor>()
                .ForMember(dest => dest.Rooms, opt => opt.MapFrom(src => src.Rooms));

            CreateMap<RoomDTO, Room>()
                .ForMember(dest => dest.Consumer, opt => opt.MapFrom(src => src.Consumer));

            CreateMap<ConsumerDTO, Consumer>();
            CreateMap<ConnectionDTO, CableConnection>();
            CreateMap<InitialSettingsDTO, InitialSettings>();
            CreateMap<FloorGroupingDTO, FloorGrouping>();
            CreateMap<GlobalGroupingDTO, GlobalGrouping>();
            CreateMap<PositionDTO, Position>();
            CreateMap<CableDTO, Cable>();
            CreateMap<PortDTO, Port>();

            CreateMap<ComponentDTO, Component>()
                .Include<FuseDTO, Fuse>()
                .Include<RCDDTO, RCD>()
                .Include<RCDFireDTO, RCDFire>()
                .Include<IntroductoryDTO, Introductory>()
                .Include<EmptySlotDTO, EmptySlot>()
                .Include<ContactorDTO, Contactor>();

            CreateMap<FuseDTO, Fuse>();
            CreateMap<RCDDTO, RCD>();
            CreateMap<RCDFireDTO, RCDFire>();
            CreateMap<IntroductoryDTO, Introductory>();
            CreateMap<EmptySlotDTO, EmptySlot>();
            CreateMap<ContactorDTO, Contactor>();

            // ==============================
            //        Entity -> DTO
            // ==============================

            CreateMap<Project, ProjectDTO>()
                .ForMember(dest => dest.FuseBox, opt => opt.MapFrom(src => src.FuseBox))
                .ForMember(dest => dest.FloorGrouping, opt => opt.MapFrom(src => src.FloorGrouping))
                .ForMember(dest => dest.GlobalGrouping, opt => opt.MapFrom(src => src.GlobalGrouping))
                .ForMember(dest => dest.Floors, opt => opt.MapFrom(src => src.Floors));

            CreateMap<FuseBoxUnit, FuseBoxUnitDTO>()
                .ForMember(dest => dest.ComponentGroups, opt => opt.MapFrom(src => src.ComponentGroups))
                .ForMember(dest => dest.CableConnections, opt => opt.MapFrom(src => src.CableConnections));

            CreateMap<FuseBoxComponentGroup, FuseBoxComponentGroupDTO>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components));

            CreateMap<Floor, FloorDTO>()
                .ForMember(dest => dest.Rooms, opt => opt.MapFrom(src => src.Rooms));

            CreateMap<Room, RoomDTO>()
                .ForMember(dest => dest.Consumer, opt => opt.MapFrom(src => src.Consumer));

            CreateMap<Consumer, ConsumerDTO>();
            CreateMap<CableConnection, ConnectionDTO>();
            CreateMap<InitialSettings, InitialSettingsDTO>();
            CreateMap<FloorGrouping, FloorGroupingDTO>();
            CreateMap<GlobalGrouping, GlobalGroupingDTO>();
            CreateMap<Introductory, IntroductoryDTO>();
            CreateMap<Position, PositionDTO>();
            CreateMap<Cable, CableDTO>();
            CreateMap<Port, PortDTO>();

            CreateMap<Component, ComponentDTO>()
                .Include<Fuse, FuseDTO>()
                .Include<RCD, RCDDTO>()
                .Include<RCDFire, RCDFireDTO>()
                .Include<Introductory, IntroductoryDTO>()
                .Include<EmptySlot, EmptySlotDTO>()
                .Include<Contactor, ContactorDTO>();

            CreateMap<Fuse, FuseDTO>();
            CreateMap<RCD, RCDDTO>();
            CreateMap<RCDFire, RCDFireDTO>();
            CreateMap<Introductory, IntroductoryDTO>();
            CreateMap<EmptySlot, EmptySlotDTO>();
            CreateMap<Contactor, ContactorDTO>();
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