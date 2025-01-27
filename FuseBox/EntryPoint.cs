
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
                Console.OutputEncoding = Encoding.UTF8;                                       // вывод русских символов в консоль
                ConfigurationService configurationService = new ConfigurationService();       // создание экземпляра сервиса конфигурации

                string jsonData = @"
                {
                  ""floorGrouping"": {
                    ""FloorGroupingP"": true,
                    ""separateUZO"": true
                    },
                  ""globalGrouping"": {
                    ""Sockets"": 1,
                    ""Lighting"": 1,
                    ""Conditioners"": 1
                  },
                  ""initialSettings"": {
                    ""PhaseCount"": 1,
                    ""MainAmperage"": 25,
                    ""ShieldWidth"": 16,
                    ""VoltageStandard"": 220,
                    ""PowerCoefficient"": 1
                  },
                  ""shield"": {
                    ""MainBreaker"": true,
                    ""Main3PN"": false,
                    ""SurgeProtection"": true,
                    ""LoadSwitch2P"": true,
                    ""ModularContactor"": true,
                    ""RailMeter"": true,
                    ""FireUZO"": true,
                    ""VoltageRelay"": true,
                    ""RailSocket"": true,
                    ""NDisconnectableLine"": true,
                    ""LoadSwitch"": true,
                    ""CrossModule"": true,
                    ""DINLines"": 1
                  },
                    ""floors"": [
                      {
                      ""floorName"": ""Ground Floor"",
                      ""rooms"": [
                        {
                          ""name"": ""Living Room"",
                          ""area"": false,
                          ""rating"": 5,
                          ""consumer"": [
                            {
                              ""id"": 1,
                              ""name"": ""TV"",
                              ""watt"": 150,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            },
                            {
                              ""id"": 2,
                              ""name"": ""Air Conditioner"",
                              ""watt"": 2000,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            },
                            {
                              ""id"": 3,
                              ""name"": ""Lighting"",
                              ""watt"": 300,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            }
                          ],
                          ""tPower"": 2450
                        },
                        {
                          ""name"": ""Kitchen"",
                          ""area"": true,
                          ""rating"": 4,
                          ""consumer"": [
                            {
                              ""id"": 4,
                              ""name"": ""Refrigerator"",
                              ""watt"": 800,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            },
                            {
                              ""id"": 5,
                              ""name"": ""Microwave"",
                              ""watt"": 1200,
                              ""contactor"": false,
                              ""separateRCD"": true,
                              ""isCritical"": false
                            },
                            {
                              ""id"": 6,
                              ""name"": ""Oven"",
                              ""watt"": 2500,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            }
                          ],
                          ""tPower"": 4500
                        }
                      ]
                    },
                    {
                      ""floorName"": ""First Floor"",
                      ""rooms"": [
                        {
                          ""name"": ""Bedroom 1"",
                          ""area"": true,
                          ""rating"": 3,
                          ""consumer"": [
                            {
                              ""id"": 7,
                              ""name"": ""Heater"",
                              ""watt"": 1000,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": true
                            },
                            {
                              ""id"": 8,
                              ""name"": ""Fan"",
                              ""watt"": 100,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            }
                          ],
                          ""tPower"": 1100
                        },
                        {
                          ""name"": ""Bathroom"",
                          ""area"": false,
                          ""rating"": 4,
                          ""consumer"": [
                            {
                              ""id"": 9,
                              ""name"": ""Water Heater"",
                              ""watt"": 3000,
                              ""contactor"": true,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            },
                            {
                              ""id"": 10,
                              ""name"": ""Hair Dryer"",
                              ""watt"": 1500,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            }
                          ],
                          ""tPower"": 4500
                        }
                      ]
                    },
                    {
                      ""floorName"": ""Second Floor"",
                      ""rooms"": [
                        {
                          ""name"": ""Office"",
                          ""area"": true,
                          ""rating"": 4,
                          ""consumer"": [
                            {
                              ""id"": 11,
                              ""name"": ""Computer"",
                              ""watt"": 400,
                              ""contactor"": false,
                              ""separateRCD"": true,
                              ""isCritical"": false
                            },
                            {
                              ""id"": 12,
                              ""name"": ""Printer"",
                              ""watt"": 200,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            },
                            {
                              ""id"": 13,
                              ""name"": ""Lighting"",
                              ""watt"": 300,
                              ""contactor"": false,
                              ""separateRCD"": false,
                              ""isCritical"": false
                            }
                          ],
                          ""tPower"": 900
                        }
                      ]
                    }
                  ]
                }";

                Project project = JsonConvert.DeserializeObject<Project>(jsonData);        // десериализация данных

                var pc = configurationService.GenerateConfiguration(project);
                var data = JsonConvert.SerializeObject(pc, Formatting.Indented);

                Console.Write(data);

            }
        }
    }
}
