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



                // Генерация конфигурации (после сохранения всего выше)

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

                // Сохранение проекта и всех компонентов в БД
                _context.Projects.Add(project);
                _context.SaveChanges();


                // Возврат результата
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
