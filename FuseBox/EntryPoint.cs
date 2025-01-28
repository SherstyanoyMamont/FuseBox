
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
            if (false)
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

                // JSON-строка с данными о проекте
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
                  ""FuseBox"": {
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
                    ""DINLines"": 3,
                    ""Price"": 1000,
                  },
                    ""floors"": [
                      {
                      ""Id"": 1,
                      ""Name"": ""Ground Floor"",
                      ""rooms"": [
                        {
                          ""name"": ""Living Room"",
                          ""Consumer"": [
                            {
                              ""id"": 1,
                              ""name"": ""TV"",
                              ""Amper"": 1,
                            },
                            {
                              ""id"": 2,
                              ""name"": ""Air Conditioner"",
                              ""Amper"": 8,
                            },
                            {
                              ""id"": 3,
                              ""name"": ""Lighting"",
                              ""Amper"": 1,
                            }
                          ],
                          ""tPower"": 10
                        },
                        {
                          ""name"": ""Kitchen"",
                          ""Consumer"": [
                            {
                              ""id"": 4,
                              ""name"": ""Refrigerator"",
                              ""Amper"": 3,
                            },
                            {
                              ""id"": 5,
                              ""name"": ""Microwave"",
                              ""Amper"": 5,
                            },
                            {
                              ""id"": 6,
                              ""name"": ""Oven"",
                              ""Amper"": 7,
                            }
                          ],
                          ""tPower"": 15
                        }
                      ]
                    },
                    {
                      ""Id"": 2,
                      ""Name"": ""First Floor"",
                      ""rooms"": [
                        {
                          ""name"": ""Bedroom 1"",
                          ""Consumer"": [
                            {
                              ""id"": 7,
                              ""name"": ""Heater"",
                              ""Amper"": 4,
                            },
                            {
                              ""id"": 8,
                              ""name"": ""Fan"",
                              ""Amper"": 1,
                            }
                          ],
                          ""tPower"": 5
                        },
                        {
                          ""name"": ""Bathroom"",
                          ""Consumer"": [
                            {
                              ""id"": 9,
                              ""name"": ""Water Heater"",
                              ""Amper"": 13,
                            },
                            {
                              ""id"": 10,
                              ""name"": ""Hair Dryer"",
                              ""Amper"": 7,
                            }
                          ],
                          ""tPower"": 20
                        }
                      ]
                    },
                    {
                      ""Id"": 3,      
                      ""Name"": ""Second Floor"",
                      ""rooms"": [
                        {
                          ""name"": ""Office"",
                          ""Consumer"": [
                            {
                              ""id"": 11,
                              ""name"": ""Computer"",
                              ""Amper"": 2,
                            },
                            {
                              ""id"": 12,
                              ""name"": ""Printer"",
                              ""Amper"": 1,
                            },
                            {
                              ""id"": 13,
                              ""name"": ""Lighting"",
                              ""Amper"": 2,
                            }
                          ],
                          ""tPower"": 40
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
