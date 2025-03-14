using FuseBox.App.Models;
using FuseBox.App.Models.Shild_Comp;
using Microsoft.EntityFrameworkCore;
using Mysqlx.Crud;

namespace FuseBox.App.Controllers
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<FuseBox> FuseBoxes { get; set; }
        public DbSet<FloorGrouping> FloorGroupings { get; set; }
        public DbSet<GlobalGrouping> GlobalGroupings { get; set; }
        public DbSet<InitialSettings> InitialSettings { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Consumer> Consumer { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Cable> Cables { get; set; }
        public DbSet<Component> Components { get; set; }
        public DbSet<Port> Ports { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //// Пример конфигурации связи между таблицами
            //modelBuilder.Entity<Order>()
            //    .HasOne(o => o.User)       // Один заказ принадлежит одному пользователю
            //    .WithMany(u => u.Orders)   // У пользователя может быть много заказов
            //    .HasForeignKey(o => o.UserId); // Связь через поле UserId

            

            // Наследование класа Component элементов щита
            // На выходе получаем одну таблицу Components со всеми компонентами в ней
            modelBuilder.Entity<Component>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Component>("Component")
                .HasValue<Fuse>("Fuse")
                .HasValue<RCD>("RCD")
                .HasValue<RCDFire>("RCDFire")
                .HasValue<Introductory>("Introductory")
                .HasValue<EmptySlot>("EmptySlot")
                .HasValue<Contactor>("Contactor");

            /////////////////////////////////////////////////////////////////////

            // Пример конфигурации связи между таблицами
            modelBuilder.Entity<User>()
                .HasMany(o => o.Projects)
                .WithOne()
                .HasForeignKey(fb => fb.UserId);

            /////////////////////////////////////////////////////////////////////

            // Project → FloorGrouping (один к одному)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.FloorGrouping)
                .WithOne(fb => fb.Project)
                .HasForeignKey<FloorGrouping>(fb => fb.ProjectId);

            // Project → GlobalGrouping (один к одному)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.GlobalGrouping)
                .WithOne(fb => fb.Project)
                .HasForeignKey<GlobalGrouping>(fb => fb.ProjectId);

            // Project → InitialSettings (один к одному)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.InitialSettings)
                .WithOne(fb => fb.Project)
                .HasForeignKey<InitialSettings>(fb => fb.ProjectId);

            /////////////////////////////////////////////////////////////////////

            // Project → FuseBox (один к одному)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.FuseBox)
                .WithOne(fb => fb.Project)
                .HasForeignKey<FuseBox>(fb => fb.ProjectId);

            // FuseBox → CableConnections (один ко многим)
            modelBuilder.Entity<FuseBox>()
                .HasMany(p => p.CableConnections)
                .WithOne()
                .HasForeignKey(f => f.FuseBoxId);

            // CableConnections → CableConnections (один ко одному)
            modelBuilder.Entity<Connection>()
                .HasOne(p => p.CabelWay)
                .WithOne(fb => fb.Connection)
                .HasForeignKey<Position>(fb => fb.ConnectionId);

            // CableConnections → CableConnections (один ко одному)
            modelBuilder.Entity<Connection>()
                .HasOne(p => p.Cable)
                .WithOne(fb => fb.Connection)
                .HasForeignKey<Cable>(fb => fb.ConnectionId);

            // CableConnections

            //// FuseBox → ComponentGroups (один ко многим)
            //modelBuilder.Entity<FuseBox>()
            //    .HasMany(fb => fb.ComponentGroups)
            //    .WithOne(cg => cg.FuseBox)
            //    .HasForeignKey(cg => cg.Id);

            // ComponentGroup → BaseElectrical (один ко многим)
            //modelBuilder.Entity<FuseBoxComponentGroup>()
            //    .HasMany(cg => cg.Components)
            //    .WithOne(c => c.FuseBoxComponentGroup)
            //    .HasForeignKey(c => c.FuseBoxComponentGroupId);

            ////////////////////////////////////////////////////////////////////

            // Project → Floors (один ко многим)
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Floors)
                .WithOne()
                .HasForeignKey(f => f.ProjectId);

            // Floor → Rooms (один ко многим)
            modelBuilder.Entity<Floor>()
                .HasMany(p => p.Rooms)
                .WithOne()
                .HasForeignKey(f => f.FloorId);

            // Room → Consumers (один ко многим)
            modelBuilder.Entity<Room>()
                .HasMany(p => p.Consumers)
                .WithOne()
                .HasForeignKey(f => f.RoomId);

            ///////////////////////////////////////////////////////////////////

        }

        // Cтрока подключения
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseMySql("server=localhost;database=mydb;user=myuser;password=mypassword", ServerVersion.AutoDetect("server=localhost"));

    }
}
