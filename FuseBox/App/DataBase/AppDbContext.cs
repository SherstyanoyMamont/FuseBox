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
        public DbSet<FloorGrouping> FloorGroupings { get; set; }
        public DbSet<GlobalGrouping> GlobalGroupings { get; set; }
        public DbSet<InitialSettings> InitialSettings { get; set; }
        public DbSet<FuseBoxUnit> FuseBoxes { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Consumer> Consumer { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Cable> Cables { get; set; }
        public DbSet<Component> Component { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<FuseBoxComponentGroup> ComponentGroups { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Пример игнорирования какого-то поля
            //modelBuilder.Ignore<BaseElectrical>();

            modelBuilder.Entity<FuseBoxUnit>().ToTable("FuseBoxes");


            //Наследование класа Component элементов щита
            //На выходе получаем одну таблицу Components со всеми компонентами в ней
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

            // User → Projects (один ко многим)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Projects)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Или другой DeleteBehavior, если нужно

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

            //Project → FuseBoxUnit(один к одному)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.FuseBox)
                .WithOne(fb => fb.Project)
                .HasForeignKey<FuseBoxUnit>(fb => fb.ProjectId);

            /////////////////////////////////////////////////////////////////////

            // Project → Floors (один ко многим)
            modelBuilder.Entity<Project>() 
                .HasMany(p => p.Floors)
                .WithOne(p => p.Project)
                .HasForeignKey(f => f.ProjectId);

            // Floor → Rooms (один ко многим)
            modelBuilder.Entity<Floor>()
                .HasMany(p => p.Rooms)
                .WithOne(p => p.Floor)
                .HasForeignKey(f => f.FloorId);

            // Room → Consumers (один ко многим)
            modelBuilder.Entity<Room>()
                .HasMany(p => p.Consumer)
                .WithOne(p => p.Room)
                .HasForeignKey(f => f.RoomId);

            ///////////////////////////////////////////////////////////////////

            ////FuseBox → Consumer(один ко многим)
            //modelBuilder.Entity<Consumer>()
            //    .HasOne(c => c.FuseBoxUnit)
            //    .WithMany(fb => fb.Contactor)
            //    .HasForeignKey(c => c.FuseBoxUnitId)
            //    .OnDelete(DeleteBehavior.Cascade);

            // FuseBox → ComponentGroups (один ко многим)
            modelBuilder.Entity<FuseBoxUnit>()
                .HasMany(fb => fb.ComponentGroups)
                .WithOne(fb => fb.FuseBoxUnit)
                .HasForeignKey(cg => cg.FuseBoxUnitId);

            // Component → FuseBoxUnit (для Electricals)
            //modelBuilder.Entity<Component>()
            //    .HasOne(c => c.FuseBoxUnit)
            //    .WithMany(fb => fb.Electricals)
            //    .HasForeignKey(c => c.FuseBoxUnitId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //FuseBox → CableConnections(один ко многим)
            modelBuilder.Entity<FuseBoxUnit>()
                .HasMany(p => p.CableConnections)
                .WithOne(p => p.FuseBoxUnit)
                .HasForeignKey(f => f.FuseBoxUnitId);

            ////////////////////////////////////////////////////////////////////

            // CableConnections → Connections (один ко одному)
            modelBuilder.Entity<Connection>()
                .HasOne(p => p.CabelWay)
                .WithOne(fb => fb.Connection)
                .HasForeignKey<Position>(fb => fb.ConnectionPositionId);

            // CableConnections → Cable (один ко одному)
            modelBuilder.Entity<Connection>()
                .HasOne(p => p.Cable)
                .WithOne(fb => fb.Connection)
                .HasForeignKey<Cable>(fb => fb.ConnectionCableId);

            ////////////////////////////////////////////////////////////////////

            //ComponentGroup → Component(один ко многим)
            modelBuilder.Entity<Component>()
                .HasOne(c => c.FuseBoxComponentGroup)
                .WithMany(fbg => fbg.Components)
                .HasForeignKey(c => c.FuseBoxComponentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            //Component → Port(один ко м11ногим)
            modelBuilder.Entity<Component>()
                .HasMany(c => c.Ports)
                .WithOne(c => c.Component)
                .HasForeignKey(p => p.ComponentId);

            ////////////////////////////////////////////////////////////////////

            modelBuilder.Entity<FuseBoxComponentGroup>()
                .HasOne(f => f.FuseBoxUnit)
                .WithMany(u => u.ComponentGroups)
                .HasForeignKey(f => f.FuseBoxUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            //modelBuilder.Entity<FuseBoxComponentGroup>()
            //    .HasMany(cg => cg.Components)
            //    .WithOne(cg => cg.FuseBoxComponentGroup)
            //    .HasForeignKey(c => c.FuseBoxComponentGroupId);

            //// FuseBox → Electricals (один ко многим)
            //modelBuilder.Entity<FuseBoxUnit>()
            //    .HasMany(fb => fb.Electricals)
            //    .WithOne()
            //    .HasForeignKey(cg => cg.FuseBoxUnitId);

            //// Component → FuseBoxUnit (для Contactor)
            //modelBuilder.Entity<Consumer>()
            //    .HasOne(c => c.FuseBoxUnit)
            //    .WithMany(fb => fb.Contactor)
            //    .HasForeignKey(c => c.FuseBoxUnitId)
            //    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
