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
using FuseBox.App.Factorys;
using FuseBox.App.Interfaces;
using FuseBox.App.Services.Providers;

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
    public class FuseBoxUnitsController : ControllerBase{
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
            if (dto.FuseBox == null) return BadRequest("FuseBox пустой.");
            if (dto.Floors == null || !dto.Floors.Any()) return BadRequest("Нет этажей.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Получаем или создаем пользователя
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == "test@example.com");
                if (existingUser == null)
                {
                    existingUser = new User { Email = "test@example.com" };
                    _context.Users.Add(existingUser);
                    //_context.SaveChanges();
                }

                // DTO → Entity
                var project = _mapper.Map<Project>(dto);
                project.User = existingUser;


                // Генерация конфигурации
                try
                {
                    Console.WriteLine("⚙️ Генерация конфигурации начинается...");

                    // Создаём адаптер, который вытянет нужные настройки из проекта
                    IProjectSettings settingsProvider = new ProjectSettings(project); // передаем проект в адаптер

                    // Создаём фабрику компонентов
                    IComponentFactory componentFactory = new ComponentFactory();

                    var configService = new ConfigurationService(project, settingsProvider, componentFactory);
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

                // Сохранение проекта и всех компонентов в БД
                _context.Projects.Add(project);
                _context.SaveChanges();

                // DTO → Entity
                var resultDto = _mapper.Map<ProjectDTO>(project);


                // Выбираем только нужные поля
                var reducedResult = new
                {
                    resultDto.Id

                };

                var data = JsonConvert.SerializeObject(reducedResult, Formatting.Indented);

                // Возврат результата
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



        [HttpGet("project/{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            var componentIdCounter = 1; // начинаем с 1

            var project = await _context.Projects
                .Include(p => p.FuseBox.ComponentGroups)
                    .ThenInclude(g => g.Components)
                .Include(p => p.FuseBox.CableConnections)
                    .ThenInclude(c => c.Cable)
                .Include(p => p.FuseBox.CableConnections)
                    .ThenInclude(c => c.CabelWay)
                .FirstOrDefaultAsync(p => p.Id == id);


            if (project?.FuseBox?.ComponentGroups != null)
            {
                foreach (var group in project.FuseBox.ComponentGroups)
                {
                    group.Components = group.Components
                        .OrderBy(c => c.SerialNumber) // <-- здесь сортировка по нужному полю
                        .ToList();
                }
            }

            foreach (var group in project.FuseBox.ComponentGroups)
            {
                var nonEmptySlots = group.Components.Where(c => c.Name != "Empty Slot").ToList();
                var emptySlots = group.Components.Where(c => c.Name == "Empty Slot").ToList();

                group.Components = nonEmptySlots.Concat(emptySlots).ToList(); // Перезаписали новый порядок
            }

            if (project == null)
                return NotFound();

            var result = new
            {
                ComponentGroups = project.FuseBox.ComponentGroups
                    .Select(group => new
                    {
                        Components = group.Components.SelectMany(c =>
                        {
                            var list = new List<object>();

                            list.Add(new
                            {
                                //Id = componentIdCounter++,
                                Id = c.SerialNumber,
                                Name = c.Name?.Replace(" ", ""),
                                Slots = c.Slots,
                                Amper = c.Amper
                            });

                            //// Проверка через название
                            //if (c.Name == "RCD")
                            //{
                            //    // Теперь Electricals нужно доставать вручную,
                            //    // например через какое-то специальное поле или отдельный способ
                            //    var rcdWithElectricals = project.FuseBox.ComponentGroups
                            //        .SelectMany(g => g.Components)
                            //        .FirstOrDefault(x => x.Id == c.Id);


                            //    foreach (var electrical in )
                            //    {
                            //        list.Add(new
                            //        {
                            //            Id = componentIdCounter++,
                            //            Name = electrical.Name?.Replace(" ", "") ?? "Unknown",
                            //            Slots = 0,
                            //            Amper = electrical.Amper
                            //        });
                            //    }

                            //}


                            return list;

                        }).ToList()
                    }).ToList(),

                CableConnections = project.FuseBox.CableConnections
                    .Select(conn => new
                    {
                        Cable = conn.Cable == null ? null : new
                        {
                            Colour = conn.Cable.Сolour,
                        },
                        CabelWay = conn.CabelWay == null ? null : new
                        {
                            IndexStart = conn.CabelWay.IndexStart,
                            IndexFinish = conn.CabelWay.IndexFinish,
                        },

                    }).ToList()
            };

            var data = JsonConvert.SerializeObject(result, Formatting.Indented);

            return Content(data, "application/json");
        }
    }
}
