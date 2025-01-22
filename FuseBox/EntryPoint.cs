
using FuseBox.Controllers;
using Newtonsoft.Json;
using System.Text;

namespace FuseBox
{

    internal class EntryPoint
    {
        //public static void Main(string[] args)
        static void Main(string[] args)
        {
            // testing switch
            if (true)
            {

                // создание нового экземпляра билдера веб-приложения
                var builder = WebApplication.CreateBuilder(args);

                // Добавляет поддержку контроллеров к фукнционалу веб-приложения
                // Контроллеры - это классы, которые отвечают за обработку входящих HTTP запросов
                // Контроллеры обрабатывают запросы и возвращают ответы
                builder.Services.AddControllers();
                // Добавляет поддержку генерации документации по API
                builder.Services.AddEndpointsApiExplorer();
                // Добавляет поддержку Swagger
                builder.Services.AddSwaggerGen();

                // Билдер создает новый экземпляр веб-приложения на основе указанных настроек
                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // Перенаправляет HTTP запросы на HTTPS
                app.UseHttpsRedirection();

                app.UseAuthorization();


                // Добавляет контроллеры к обработке запросов
                app.MapControllers();

                app.Run();

            }
            else
            {
                Console.OutputEncoding = Encoding.UTF8; // вывод русских символов в консоль
                ConfigurationService configurationService = new ConfigurationService();
                var project = new Project
                {
                    Floors = new List<Floor>
                {
                    new Floor
                    {
                        FloorName = "Первый этаж",
                        Rooms = new List<Room>
                        {
                            new Room
                            {
                                Name = "Кухня",
                                Equipments = new List<Consumer>
                                {
                                    new Consumer { Name = "Духовой шкаф", Watt = 2200 },
                                    new Consumer { Name = "Варочная поверхность", Watt = 2000 }
                                }
                            },
                            new Room
                            {
                                Name = "Гостиная",
                                Equipments = new List<Consumer>
                                {
                                    new Consumer { Name = "Освещение", Watt = 150 },
                                    new Consumer { Name = "Розетки", Watt = 300 }
                                }
                            }
                        }
                    },
                    new Floor
                    {
                        FloorName = "Второй этаж",
                        Rooms = new List<Room>
                        {
                            new Room
                            {
                                Name = "Спальня",
                                Equipments = new List<Consumer>
                                {
                                    new Consumer { Name = "Освещение", Watt = 100 },
                                    new Consumer { Name = "Розетки", Watt = 200 }
                                }
                            }
                        }
                    }
                }
                };

                var pc = configurationService.GenerateConfiguration(project);
                var data = JsonConvert.SerializeObject(pc, Formatting.Indented);

                Console.Write(data);

            }
        }
    }
}
