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
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using FuseBox.App.Models.Shild_Comp;


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

    [ApiController]
    [Microsoft.AspNetCore.Components.Route("")]
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
        public async Task<IActionResult> CalculateFuseBox([FromBody] ProjectDTO dto)
        {

            // Проверка входных данных
            if (dto == null) return BadRequest("Проект не передан вообще.");
            if (dto.FuseBox == null) return BadRequest("FuseBox пустой.");  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (dto.Floors == null || !dto.Floors.Any()) return BadRequest("Нет этажей.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // 1. Получаем или создаем пользователя
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == "test@example.com");
                if (existingUser == null)
                {
                    existingUser = new User { Email = "test@example.com" };
                    _context.Users.Add(existingUser);
                    _context.SaveChanges();
                }

                // DTO
                /////////////////////////////////////////////////////////////////////////////////////////////////////////
                // DTO → Entity
                var project = _mapper.Map<Project>(dto);
                project.User = existingUser;

                // Страховка, если AutoMapper обошел конструктор по умолчанию:
                project.FuseBox ??= new FuseBoxUnit();
                project.FloorGrouping ??= new FloorGrouping();
                project.GlobalGrouping ??= new GlobalGrouping();
                project.InitialSettings ??= new InitialSettings();
                project.Floors ??= new List<Floor>();

                // 3. Устанавливаем связи вручную
                if (project.InitialSettings != null)
                    project.InitialSettings.Project = project;

                if (project.FuseBox != null)
                    project.FuseBox.Project = project;

   


                // 5. Генерация конфигурации (после сохранения всего выше)


                try
                {
                    Console.WriteLine("⚙️ Генерация конфигурации начинается...");

                    var configService = new ConfigurationService(project);
                    configService.GenerateConfiguration();

                    Console.WriteLine("✅ Генерация конфигурации завершена.");
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Console.WriteLine("❌ Ошибка диапазона индекса:");
                    Console.WriteLine(ex);
                    return BadRequest("Ошибка индекса в GenerateConfiguration: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Общая ошибка в GenerateConfiguration:");
                    Console.WriteLine(ex);
                    return BadRequest("Ошибка генерации: " + ex.Message);
                }

                Console.WriteLine($"🚨 Floors count: {project.Floors?.Count ?? 0}");
                foreach (var floor in project.Floors ?? new())
                {
                    Console.WriteLine($"🏠 Floor: {floor.Name}, Rooms: {floor.Rooms?.Count ?? 0}");
                    foreach (var room in floor.Rooms ?? new())
                    {
                        Console.WriteLine($"🛏 Room: {room.Name}, Consumers: {room.Consumer?.Count ?? 0}");
                    }
                }

                Console.WriteLine("📦 Проверка ComponentGroups перед сохранением компонентов:");
                foreach (var group in project.FuseBox.ComponentGroups)
                {
                    var entry = _context.Entry(group);
                    Console.WriteLine($"🔍 Group Id: {group.Id}, EF State: {entry.State}");

                    foreach (var component in group.Components ?? new())
                    {
                        Console.WriteLine($"   🧩 Component: {component.Name}, GroupId: {group.Id}, FK = {component.FuseBoxComponentGroupId}");
                    }
                }
                Console.WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                // 4. Добавляем и сохраняем Project (вместе с InitialSettings и FuseBox)

                _context.Projects.Add(project);


                var entries = _context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .ToList();

                foreach (var entry in entries)
                {
                    Console.WriteLine($"!!!!!!!!Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                }

                Console.WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");


                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///
                // 1. Project и User (если User не привязан через внешний ключ к Project — иначе поменяй порядок)


                project.User = existingUser;  // если нужен
                _context.Projects.Add(project);

                // Теперь — только вручную сохраняем кусочки:

                _context.SaveChanges();

                /// 
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////   



                //// 8. Сохраняем ComponentGroups

                ////_context.ComponentGroups.AddRange(project.FuseBox.ComponentGroups);
                ////_context.SaveChanges();
                //// Логирование количества сгенерированных соединений
                //Console.WriteLine($"📊 Connections generated: {project.FuseBox?.CableConnections?.Count ?? 0}");

                //var tempCables = new List<Cable>();
                //var ports = new List<Port>();


                //// 6. Назначаем каждому компоненту ссылку на его группу (теперь с реальным Id)

                //var componentsToSave = new List<Component>();

                //foreach (var group in project.FuseBox.ComponentGroups)
                //{
                //    foreach (var component in group.Components ?? new List<Component>())
                //    {
                //        component.FuseBoxComponentGroupId = group.Id;
                //        component.FuseBoxComponentGroup = null; // чтоб не дублировалось через навигацию
                //        componentsToSave.Add(component);
                //    }
                //}


                //// 7. Сохраняем компоненты

                //foreach (var comp in componentsToSave)
                //{
                //    Console.WriteLine($"🧩 Component: {comp.Name}, GroupId = {comp.FuseBoxComponentGroupId}");
                //}

                //_context.Component.AddRange(componentsToSave); // Или Components, если твоя таблица называется так
                //_context.SaveChanges();

                //// 9. Обработка Connections и временное отсоединение кабелей
                //if (project.FuseBox?.CableConnections != null)
                //{
                //    foreach (var connection in project.FuseBox.CableConnections)
                //    {
                //        connection.FuseBoxUnit = project.FuseBox;

                //        if (connection.Cable != null)
                //        {
                //            tempCables.Add(connection.Cable);
                //            connection.Cable.Connection = null;

                //            var tempCable = connection.Cable;
                //            connection.Cable = null;

                //            var cableEntry = _context.Entry(tempCable);
                //            if (cableEntry.State != EntityState.Detached)
                //                cableEntry.State = EntityState.Detached;
                //        }
                //    }

                //    _context.Connections.AddRange(project.FuseBox.CableConnections);
                //}

                //// 10. Сбор портов
                //if (project.FuseBox?.ComponentGroups != null)
                //{
                //    foreach (var group in project.FuseBox.ComponentGroups)
                //    {
                //        foreach (var comp in group.Components ?? Enumerable.Empty<Component>())
                //        {
                //            ports.AddRange(comp.Ports ?? Enumerable.Empty<Port>());
                //        }
                //    }
                //}

                //if (ports.Any())
                //    _context.Ports.AddRange(ports);

                //// 11. Сохраняем Connections и Ports
                //Console.WriteLine("🧪 Состояние перед сохранением:");
                //foreach (var entry in _context.ChangeTracker.Entries())
                //{
                //    Console.WriteLine($"🔍 Entity: {entry.Entity?.GetType().Name ?? "NULL"}, State: {entry.State}");
                //}

                //_context.SaveChanges(); // Connections получают Id

                //// 12. Возврат кабелей с актуальными внешними ключами
                //var cablesToSave = new List<Cable>();
                //foreach (var conn in project.FuseBox.CableConnections)
                //{
                //    var matchingCable = tempCables.FirstOrDefault();
                //    if (matchingCable != null)
                //    {
                //        matchingCable.ConnectionCableId = conn.Id;
                //        matchingCable.Connection = conn;
                //        conn.Cable = matchingCable;

                //        cablesToSave.Add(matchingCable);
                //        tempCables.Remove(matchingCable);
                //    }
                //}

                //// 13. Сохраняем кабели
                //if (cablesToSave.Any())
                //{
                //    foreach (var c in cablesToSave)
                //    {
                //        if (c == null || c.ConnectionCableId == 0)
                //        {
                //            Console.WriteLine("❌ Cable с отсутствующим FK найден и пропущен.");
                //            continue;
                //        }
                //        Console.WriteLine($"📦 Сохраняем Cable: FK = {c.ConnectionCableId}");
                //    }

                //    _context.Cables.AddRange(cablesToSave.Where(c => c != null && c.ConnectionCableId > 0));
                //    _context.SaveChanges();
                //}
                //else
                //{
                //    Console.WriteLine("⚠️ Нет кабелей для сохранения.");
                //}

                // 14. Возврат результата
                var resultDto = _mapper.Map<ProjectDTO>(project);
                var data = JsonConvert.SerializeObject(resultDto, Formatting.Indented);

                Console.WriteLine("✅ Project and all components saved successfully!");
                return Content(data, "application/json");
            }
            catch (Exception ex)
            {
                var error = GetFullError(ex);
                Console.WriteLine("❌ Ошибка при сохранении:");
                Console.WriteLine(error);
                return BadRequest($"Deserializ error: {error}");
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