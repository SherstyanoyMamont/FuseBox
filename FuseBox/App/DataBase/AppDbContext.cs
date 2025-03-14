using FuseBox.App.Controllers;
using FuseBox.App.Models;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using FuseBox.FuseBox;
using Microsoft.EntityFrameworkCore;
using Mysqlx.Crud;

namespace FuseBox.App.DataBase
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<FuseBoxUnit> FuseBoxes { get; set; }
        public DbSet<FloorGrouping> FloorGroupings { get; set; }
        public DbSet<GlobalGrouping> GlobalGroupings { get; set; }
        public DbSet<InitialSettings> InitialSettings { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Consumer> Consumer { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Cable> Cables { get; set; }
        //public DbSet<Component> Components { get; set; }
        public DbSet<Port> Ports { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<BaseElectrical>()
            //    .HasDiscriminator<string>("Type")
            //    .HasValue<SomeElectrical>("SomeElectrical");

            // Пример игнорирования какого-то поля
            //modelBuilder.Ignore<BaseElectrical>();

            //Наследование класа Component элементов щита
            //На выходе получаем одну таблицу Components со всеми компонентами в ней
            modelBuilder.Entity<Component>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Component>("Component")
                .HasValue<Fuse>("Fuse")
                .HasValue<RCD>("RCD")
                .HasValue<RCDFire>("RCDFire")
                .HasValue<Introductory>("Introductory")
                //.HasValue<EmptySlot>("EmptySlot")
                .HasValue<Contactor>("Contactor");

            /////////////////////////////////////////////////////////////////////

            //// Project → FloorGrouping (один к одному)
            //modelBuilder.Entity<Project>()
            //    .HasOne(p => p.FloorGrouping)
            //    .WithOne(fb => fb.Project)
            //    .HasForeignKey<FloorGrouping>(fb => fb.ProjectId);

            // Project → GlobalGrouping (один к одному)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.GlobalGrouping)
                .WithOne(fb => fb.Project)
                .HasForeignKey<GlobalGrouping>(fb => fb.ProjectId);

            //// Project → InitialSettings (один к одному)
            //modelBuilder.Entity<Project>()
            //    .HasOne(p => p.InitialSettings)
            //    .WithOne(fb => fb.Project)
            //    .HasForeignKey<InitialSettings>(fb => fb.ProjectId);

            /////////////////////////////////////////////////////////////////////

            //Project → FuseBox(один к одному)
            //modelBuilder.Entity<Project>()
            //    .HasOne(p => p.FuseBoxUnit)
            //    .WithOne(fb => fb.Project)
            //    .HasForeignKey<FuseBoxUnit>(fb => fb.ProjectId);

            ////FuseBox → CableConnections(один ко многим)
            //modelBuilder.Entity<FuseBox>()
            //    .HasMany(p => p.CableConnections)
            //    .WithOne()
            //    .HasForeignKey(f => f.FuseBoxId);

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

            //CableConnections

            //// FuseBox → ComponentGroups (один ко многим)
            //modelBuilder.Entity<FuseBoxUnit>()
            //    .HasMany(fb => fb.Components)
            //    .WithOne()
            //    .HasForeignKey(cg => cg.);

            //ComponentGroup → BaseElectrical(один ко многим)
            //modelBuilder.Entity<FuseBoxComponentGroup>()
            //    .HasMany(cg => cg.Components)
            //    .WithOne()
            //    .HasForeignKey(c => c.Id);

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
                .HasMany(p => p.Consumer)
                .WithOne()
                .HasForeignKey(f => f.RoomId);

            ///////////////////////////////////////////////////////////////////

        }
    }
}
