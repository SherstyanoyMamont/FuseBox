using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FuseBox;
using FuseBox.App.DataBase;
using FuseBox.App.Factorys;
using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.DTO;
using FuseBox.App.Models.Shild_Comp;
using FuseBox.App.Services.Providers;
using FuseBox.FuseBox;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FuseBox.Controllers
{
    public class FuseBoxUnitProfile : Profile
    {
        public FuseBoxUnitProfile()
        {
            CreateMap<FuseBoxUnitDTO, FuseBoxUnit>()
                .ForMember(
                    destination => destination.Project,
                    options => options.Ignore());

            CreateMap<FuseBoxUnit, FuseBoxUnitDTO>();
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
            _mapper = new MapperConfiguration(configuration =>
            {
                configuration.CreateMap<Project, ProjectDTO>()
                    .ForMember(
                        destination => destination.FuseBox,
                        options => options.Ignore());

                configuration.AddProfile<ProjectProfile>();
            }).CreateMapper();

            _context = context;
        }

        [HttpPost("calculation")]
        public async Task<IActionResult> CalculateFuseBox(
            [FromBody] ProjectDTO dto)
        {
            if (dto == null)
                return BadRequest("Проект не передан вообще.");

            if (dto.FuseBox == null)
                return BadRequest("FuseBox пустой.");

            if (dto.Floors == null || !dto.Floors.Any())
                return BadRequest("Нет этажей.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(
                        user => user.Email == "test@example.com");

                if (existingUser == null)
                {
                    existingUser = new User
                    {
                        Email = "test@example.com"
                    };

                    _context.Users.Add(existingUser);
                }

                var project = _mapper.Map<Project>(dto);
                project.User = existingUser;

                try
                {
                    PrepareConsumerLoads(project);
                }
                catch (ArgumentException exception)
                {
                    return BadRequest(new
                    {
                        message = exception.Message
                    });
                }

                try
                {
                    Console.WriteLine(
                        "Генерация конфигурации начинается...");

                    IProjectSettings settingsProvider =
                        new ProjectSettingsProvider(project);

                    IComponentFactory componentFactory =
                        new ComponentFactory();

                    IConnectionFactory cableConnectionFactory =
                        new ConnectionFactory();

                    IFuseBoxManager fuseBoxManager =
                        new FuseBoxCableConnectionManager(
                            project.FuseBox);

                    IProjectGrouping projectGrouping =
                        new ProjectGroupingProvider(project);

                    IConsumerProvider consumerProvider =
                        new ConsumerProvider(project);

                    IDistributionService distributionService =
                        new DistributionService(
                            settingsProvider,
                            projectGrouping,
                            consumerProvider,
                            componentFactory);

                    var singlePhaseRules =
                        new List<IShieldConfigurationRule>
                        {
                            new IntroductoryModuleRule()
                        };

                    var threePhaseRules =
                        new List<IShieldConfigurationRule>();

                    var configurationService =
                        new ConfigurationService(
                            settingsProvider,
                            componentFactory,
                            fuseBoxManager,
                            cableConnectionFactory,
                            projectGrouping,
                            consumerProvider,
                            singlePhaseRules,
                            threePhaseRules,
                            distributionService);

                    configurationService.GenerateConfiguration();

                    Console.WriteLine(
                        "Генерация конфигурации завершена.");
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    Console.WriteLine(exception);

                    return BadRequest(new
                    {
                        message =
                            "Ошибка индекса в GenerateConfiguration: " +
                            exception.Message
                    });
                }
                catch (Exception exception)
                {
                    Console.WriteLine(
                        "Ошибка генерации конфигурации:");
                    Console.WriteLine(exception);

                    var rootException = exception.GetBaseException();

                    if (rootException is ArgumentException)
                    {
                        return BadRequest(new
                        {
                            message = rootException.Message
                        });
                    }

                    return Problem(
                        title: "Ошибка генерации схемы.",
                        detail:
                            "Не удалось сформировать схему. " +
                            "Подробности записаны в журнал сервера.",
                        statusCode: 500);
                }

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // Возвращаем ID непосредственно сохранённого проекта.
                var result = new
                {
                    Id = project.Id
                };

                Console.WriteLine(
                    $"Проект №{project.Id} и его компоненты сохранены.");

                return Content(
                    JsonConvert.SerializeObject(
                        result,
                        Formatting.Indented),
                    "application/json");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Ошибка сохранения проекта:");
                Console.WriteLine(exception);

                return Problem(
                    title: "Ошибка сохранения проекта.",
                    detail:
                        "Не удалось сохранить проект. " +
                        "Подробности записаны в журнал сервера.",
                    statusCode: 500);
            }
        }

        [HttpGet("project/{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.FuseBox.ComponentGroups)
                    .ThenInclude(group => group.Components)
                .Include(p => p.FuseBox.CableConnections)
                    .ThenInclude(connection => connection.Cable)
                .Include(p => p.FuseBox.CableConnections)
                    .ThenInclude(connection => connection.CabelWay)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project?.FuseBox == null)
                return NotFound();

            var fuseBox = project.FuseBox;

            // SerialNumber — идентификатор устройства на схеме.
            // Id — отдельный идентификатор записи базы данных.
            var devices = fuseBox.ComponentGroups
                .SelectMany(group => group.Components)
                .Where(component => component is not EmptySlot)
                .ToList();

            if (devices.Any(component =>
                component.SerialNumber <= 0))
            {
                return Problem(
                    title: "Некорректные идентификаторы схемы.",
                    detail:
                        "Обнаружено устройство с неположительным " +
                        "SerialNumber.",
                    statusCode: 500);
            }

            if (devices
                .GroupBy(component => component.SerialNumber)
                .Any(group => group.Count() > 1))
            {
                return Problem(
                    title: "Некорректные идентификаторы схемы.",
                    detail:
                        "Обнаружены устройства с одинаковым " +
                        "SerialNumber.",
                    statusCode: 500);
            }

            var schemaIds = devices
                .Select(component => component.SerialNumber)
                .ToHashSet();

            bool hasInvalidConnection =
                fuseBox.CableConnections.Any(connection =>
                    connection.CabelWay == null ||
                    !schemaIds.Contains(
                        connection.CabelWay.IndexStart) ||
                    !schemaIds.Contains(
                        connection.CabelWay.IndexFinish));

            if (hasInvalidConnection)
            {
                return Problem(
                    title: "Некорректные соединения схемы.",
                    detail:
                        "Соединение ссылается на отсутствующее " +
                        "устройство или не содержит концов соединения.",
                    statusCode: 500);
            }

            // Подгружаем потребителей каждого автомата.
            foreach (var fuse in devices.OfType<Fuse>())
            {
                await _context.Entry(fuse)
                    .Collection(component => component.Electricals)
                    .LoadAsync();
            }

            // Устройства идут по SchemaId.
            // Заполнители свободного места всегда находятся в конце.
            foreach (var group in fuseBox.ComponentGroups)
            {
                group.Components = group.Components
                    .OrderBy(component =>
                        component is EmptySlot ? 1 : 0)
                    .ThenBy(component => component.SerialNumber)
                    .ToList();
            }

            var incomingConnections = fuseBox.CableConnections
                .ToLookup(connection =>
                    connection.CabelWay.IndexFinish);

            var rcdIds = devices
                .OfType<RCD>()
                .Select(rcd => rcd.SerialNumber)
                .ToHashSet();

            // Определяем фазу RCD или автомата
            // по входящему фазному соединению.
            string? GetPhaseColour(Component component)
            {
                if (component is not RCD &&
                    component is not Fuse)
                {
                    return null;
                }

                var colours =
                    incomingConnections[component.SerialNumber]
                        .Select(connection =>
                            connection.Cable?.Сolour)
                        .Where(colour =>
                            colour == "Red" ||
                            colour == "Orange" ||
                            colour == "Grey")
                        .Distinct()
                        .ToList();

                return colours.Count == 1
                    ? colours[0]
                    : null;
            }

            // Родительский RCD определяется по соединениям,
            // а не по соседству устройств в массиве.
            int? GetParentRcdId(Component component)
            {
                if (component is not Fuse)
                    return null;

                var parentIds =
                    incomingConnections[component.SerialNumber]
                        .Select(connection =>
                            connection.CabelWay.IndexStart)
                        .Where(sourceId => rcdIds.Contains(sourceId))
                        .Distinct()
                        .ToList();

                return parentIds.Count == 1
                    ? parentIds[0]
                    : null;
            }

            var result = new
            {
                ComponentGroups = fuseBox.ComponentGroups
                    .OrderBy(group => group.Components
                        .Where(component =>
                            component is not EmptySlot)
                        .Select(component => component.SerialNumber)
                        .DefaultIfEmpty(int.MaxValue)
                        .Min())
                    .Select(group => new
                    {
                        Components = group.Components
                            .Select(component => new
                            {
                                SchemaId = component is EmptySlot
                                    ? (int?)null
                                    : component.SerialNumber,

                                DatabaseId = component.Id,

                                Name = component.Name?
                                    .Replace(" ", ""),

                                Slots = component.Slots,

                                Amper = component.Amper,

                                Capacity = component is RCD rcd
                                    ? (int?)rcd.Capacity
                                    : null,

                                PhaseColour =
                                    GetPhaseColour(component),

                                ParentRcdSchemaId =
                                    GetParentRcdId(component),

                                Electricals = component is Fuse fuse
                                    ? fuse.Electricals
                                    : null
                            })
                            .ToList()
                    })
                    .ToList(),

                CableConnections = fuseBox.CableConnections
                    .Select(connection => new
                    {
                        Cable = connection.Cable == null
                            ? null
                            : new
                            {
                                // В модели первая буква С — кириллическая.
                                Colour = connection.Cable.Сolour
                            },

                        CabelWay = new
                        {
                            IndexStart =
                                connection.CabelWay.IndexStart,

                            IndexFinish =
                                connection.CabelWay.IndexFinish
                        }
                    })
                    .ToList()
            };

            return Content(
                JsonConvert.SerializeObject(
                    result,
                    Formatting.Indented),
                "application/json");
        }

        [HttpGet("project/{id}/configuration")]
        public async Task<IActionResult> GetProjectConfiguration(
            int id)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .Include(p => p.InitialSettings)
                .Include(p => p.FuseBox)
                .Include(p => p.Floors)
                    .ThenInclude(floor => floor.Rooms)
                        .ThenInclude(room => room.Consumer)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project?.FuseBox == null ||
                project.InitialSettings == null)
            {
                return NotFound();
            }

            // Старые значения тока нельзя выдавать за мощность.
            bool hasUnknownPower = project.Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Consumer)
                .Any(consumer => !consumer.PowerWatts.HasValue);

            if (hasUnknownPower)
            {
                return Conflict(new
                {
                    message =
                        "Этот проект создан до сохранения мощности " +
                        "в ваттах. Его схему можно просмотреть, " +
                        "но для редактирования нужно восстановить " +
                        "исходную мощность потребителей."
                });
            }

            var settings = project.InitialSettings;
            var devices = project.FuseBox;

            var result = new
            {
                version = 1,
                projectId = project.Id,

                primarySettings = new
                {
                    settings.PhasesCount,
                    settings.MainAmperage,
                    settings.ShieldWidth,
                    settings.VoltageStandard,
                    settings.PowerCoefficient
                },

                shieldDevices = new
                {
                    devices.MainBreaker,
                    devices.Main3PN,
                    devices.SurgeProtection,
                    devices.LoadSwitch2P,
                    devices.ModularContactor,
                    devices.RailMeter,
                    devices.FireUZO,
                    devices.VoltageRelay,
                    devices.RailSocket,

                    NDisconnectableLine = devices.NDiscLine,

                    devices.LoadSwitch,
                    devices.CrossModule,
                    devices.DINLines,
                    devices.Price
                },

                floors = project.Floors
                    .OrderBy(floor => floor.Id)
                    .Select(floor => new
                    {
                        id = $"floor-{floor.Id}",
                        name = floor.Name,

                        rooms = floor.Rooms
                            .OrderBy(room => room.Id)
                            .Select(room => new
                            {
                                id = $"room-{room.Id}",
                                name = room.Name,

                                consumers = room.Consumer
                                    .OrderBy(consumer => consumer.Id)
                                    .Select(consumer => new
                                    {
                                        icon = "⚡",
                                        name = consumer.Name,
                                        ab =
                                            $"C{consumer.BreakerAmperage}",
                                        watt =
                                            consumer.PowerWatts!.Value,
                                        rcdMilliAmps =
                                            consumer.RcdMilliAmps
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList()
            };

            return Content(
                JsonConvert.SerializeObject(result),
                "application/json");
        }

        private static void PrepareConsumerLoads(Project project)
        {
            if (project.InitialSettings == null)
            {
                throw new ArgumentException(
                    "Не переданы основные настройки проекта.");
            }

            int voltage = project.InitialSettings.VoltageStandard;

            if (voltage != 220 && voltage != 230)
            {
                throw new ArgumentException(
                    "Напряжение однофазного потребителя " +
                    "должно быть 220 или 230 В.");
            }

            int[] allowedBreakers =
            {
                2, 4, 6, 10, 16, 20, 25, 32
            };

            double totalPowerWatts = 0;

            foreach (var floor in project.Floors)
            {
                foreach (var room in floor.Rooms)
                {
                    foreach (var consumer in room.Consumer)
                    {
                        if (!consumer.PowerWatts.HasValue ||
                            !double.IsFinite(
                                consumer.PowerWatts.Value) ||
                            consumer.PowerWatts.Value < 0)
                        {
                            throw new ArgumentException(
                                "Укажите корректную мощность " +
                                $"для «{consumer.Name}».");
                        }

                        if (!allowedBreakers.Contains(
                            consumer.BreakerAmperage))
                        {
                            throw new ArgumentException(
                                "Недопустимый автомат " +
                                $"для «{consumer.Name}».");
                        }

                        if (consumer.RcdMilliAmps != 10 &&
                            consumer.RcdMilliAmps != 30)
                        {
                            throw new ArgumentException(
                                "Недопустимая чувствительность RCD " +
                                $"для «{consumer.Name}».");
                        }

                        // Все потребители пока однофазные.
                        // Упрощение текущей модели:
                        // коэффициент мощности равен 1.
                        consumer.Amper =
                            consumer.PowerWatts.Value / voltage;

                        if (consumer.Amper >
                            consumer.BreakerAmperage)
                        {
                            throw new ArgumentException(
                                $"«{consumer.Name}»: расчётный ток " +
                                $"{consumer.Amper:F2} А превышает " +
                                "выбранный автомат " +
                                $"C{consumer.BreakerAmperage}.");
                        }

                        // До сохранения RoomId может быть равен нулю.
                        // Для группировки используем объект комнаты.
                        consumer.Room = room;

                        totalPowerWatts +=
                            consumer.PowerWatts.Value;
                    }
                }
            }

            if (!double.IsFinite(totalPowerWatts))
            {
                throw new ArgumentException(
                    "Суммарная мощность проекта выходит " +
                    "за допустимый диапазон.");
            }

            project.TotalPower = totalPowerWatts;
        }
    }
}