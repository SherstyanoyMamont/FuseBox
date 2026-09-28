using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using FuseBox.App.DataBase;

namespace FuseBox.App.Controllers
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.Extensions.Configuration;

    namespace FuseBox.App.DataBase
    {
        public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext(string[] args)
            {
                // 🔥 Отладка! Путь до конфигурации.
                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.Development.json")
                    .Build();

                var connectionString = config.GetConnectionString("DefaultConnection");

                // 🔥 Ещё отладка! Что внутри connectionString?
                Console.WriteLine($"Connection String: {connectionString}");

                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 41))
                );

                return new AppDbContext(optionsBuilder.Options);
            }
        }
    }
}
