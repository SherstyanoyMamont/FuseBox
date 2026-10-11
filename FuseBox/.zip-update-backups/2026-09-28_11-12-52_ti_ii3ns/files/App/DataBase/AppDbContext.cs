using FuseBox.App.Models;
using FuseBox.App.Models.Shild_Comp;
using FuseBox.FuseBox;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.DataBase
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<FloorGrouping> FloorGroupings { get; set; }
        public DbSet<GlobalGrouping> GlobalGroupings { get; set; }
        public DbSet<InitialSettings> InitialSettings { get; set; }
        public DbSet<FuseBoxUnit> FuseBoxes { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Consumer> Consumer { get; set; }
        public DbSet<CableConnection> Connections { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Cable> Cables { get; set; }
        public DbSet<Component> Component { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<FuseBoxComponentGroup> ComponentGroups { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FuseBoxUnit>().ToTable("FuseBoxes");

            modelBuilder.Entity<Component>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Component>("Component")
                .HasValue<Fuse>("Fuse")
                .HasValue<RCD>("RCD")
                .HasValue<RCDFire>("RCDFire")
                .HasValue<Introductory>("Introductory")
                .HasValue<EmptySlot>("EmptySlot")
                .HasValue<Contactor>("Contactor");

            // User account data.
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(user => user.Email)
                    .IsRequired()
                    .HasMaxLength(254);

                entity.Property(user => user.NormalizedEmail)
                    .IsRequired()
                    .HasMaxLength(254);

                entity.HasIndex(user => user.NormalizedEmail)
                    .IsUnique();

                entity.Property(user => user.PasswordHash)
                    .HasMaxLength(512);

                entity.Property(user => user.SecurityStamp)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.HasMany(user => user.Projects)
                    .WithOne(project => project.User)
                    .HasForeignKey(project => project.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(user => user.PasswordResetTokens)
                    .WithOne(token => token.User)
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.Property(token => token.TokenHash)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.HasIndex(token => token.TokenHash)
                    .IsUnique();

                entity.HasIndex(token => new
                {
                    token.UserId,
                    token.ExpiresAtUtc
                });
            });

            modelBuilder.Entity<Project>(entity =>
            {
                // Name is inherited from BaseEntity but project names have a
                // stricter database contract than generic entity names.
                entity.Property(project => project.Name)
                    .IsRequired()
                    .HasMaxLength(120);
            });

            modelBuilder.Entity<Project>()
                .HasOne(project => project.FloorGrouping)
                .WithOne(grouping => grouping.Project)
                .HasForeignKey<FloorGrouping>(grouping => grouping.ProjectId);

            modelBuilder.Entity<Project>()
                .HasOne(project => project.GlobalGrouping)
                .WithOne(grouping => grouping.Project)
                .HasForeignKey<GlobalGrouping>(grouping => grouping.ProjectId);

            modelBuilder.Entity<Project>()
                .HasOne(project => project.InitialSettings)
                .WithOne(settings => settings.Project)
                .HasForeignKey<InitialSettings>(settings => settings.ProjectId);

            modelBuilder.Entity<Project>()
                .HasOne(project => project.FuseBox)
                .WithOne(fuseBox => fuseBox.Project)
                .HasForeignKey<FuseBoxUnit>(fuseBox => fuseBox.ProjectId);

            modelBuilder.Entity<Project>()
                .HasMany(project => project.Floors)
                .WithOne(floor => floor.Project)
                .HasForeignKey(floor => floor.ProjectId);

            modelBuilder.Entity<Floor>()
                .HasMany(floor => floor.Rooms)
                .WithOne(room => room.Floor)
                .HasForeignKey(room => room.FloorId);

            modelBuilder.Entity<Room>()
                .HasMany(room => room.Consumer)
                .WithOne(consumer => consumer.Room)
                .HasForeignKey(consumer => consumer.RoomId);

            modelBuilder.Entity<FuseBoxUnit>()
                .HasMany(fuseBox => fuseBox.ComponentGroups)
                .WithOne(group => group.FuseBoxUnit)
                .HasForeignKey(group => group.FuseBoxUnitId);

            modelBuilder.Entity<FuseBoxUnit>()
                .HasMany(fuseBox => fuseBox.CableConnections)
                .WithOne(connection => connection.FuseBoxUnit)
                .HasForeignKey(connection => connection.FuseBoxUnitId);

            modelBuilder.Entity<CableConnection>()
                .HasOne(connection => connection.CabelWay)
                .WithOne(position => position.Connection)
                .HasForeignKey<Position>(position => position.ConnectionPositionId);

            modelBuilder.Entity<CableConnection>()
                .HasOne(connection => connection.Cable)
                .WithOne(cable => cable.Connection)
                .HasForeignKey<Cable>(cable => cable.ConnectionCableId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Component>()
                .HasOne(component => component.FuseBoxComponentGroup)
                .WithMany(group => group.Components)
                .HasForeignKey(component => component.FuseBoxComponentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Component>()
                .HasMany(component => component.Ports)
                .WithOne(port => port.Component)
                .HasForeignKey(port => port.ComponentId);

            modelBuilder.Entity<FuseBoxComponentGroup>()
                .HasMany(group => group.Components)
                .WithOne(component => component.FuseBoxComponentGroup)
                .HasForeignKey(component => component.FuseBoxComponentGroupId);

            modelBuilder.Entity<Consumer>()
                .Property(consumer => consumer.BreakerAmperage)
                .HasDefaultValue(16);

            modelBuilder.Entity<Consumer>()
                .Property(consumer => consumer.RcdMilliAmps)
                .HasDefaultValue(30);
        }
    }
}
