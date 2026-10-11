
using FuseBox.App.Accounts;
using FuseBox.App.Administration;
using FuseBox.App.Billing;
using FuseBox.App.Catalog;
using FuseBox.App.Models;
using FuseBox.App.SchemaEditor;
using FuseBox.App.Subscriptions;
using FuseBox.App.Usage;
using FuseBox.App.Models.Shild_Comp;
using FuseBox.FuseBox;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.DataBase
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<UserEntitlementOverride> UserEntitlementOverrides { get; set; }
        public DbSet<BillingWebhookEvent> BillingWebhookEvents { get; set; }
        public DbSet<ManagedComponentCatalogItem> ManagedComponentCatalogItems { get; set; }
        public DbSet<UsageCounter> UsageCounters { get; set; }
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
        public DbSet<SchemaEditorState> SchemaEditorStates { get; set; }

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

                entity.Property(user => user.Role)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasDefaultValue(UserRole.User);

                entity.Property(user => user.Status)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasDefaultValue(UserStatus.Active);

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

                entity.HasOne(user => user.Subscription)
                    .WithOne(subscription => subscription.User)
                    .HasForeignKey<UserSubscription>(subscription => subscription.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.Property(subscription => subscription.Provider)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasDefaultValue(SubscriptionProvider.Internal);

                entity.Property(subscription => subscription.ProviderEnvironment)
                    .HasMaxLength(16);

                entity.Property(subscription => subscription.ProviderCustomerId)
                    .HasMaxLength(64);

                entity.Property(subscription => subscription.ProviderSubscriptionId)
                    .HasMaxLength(64);

                entity.Property(subscription => subscription.Plan)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasDefaultValue(PlanCode.Free);

                entity.Property(subscription => subscription.Status)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasDefaultValue(SubscriptionStatus.Active);

                entity.Property(subscription => subscription.CancelAtPeriodEnd)
                    .HasDefaultValue(false);

                entity.HasIndex(subscription => subscription.UserId)
                    .IsUnique();

                entity.HasIndex(subscription => subscription.ProviderCustomerId)
                    .IsUnique();

                entity.HasIndex(subscription => subscription.ProviderSubscriptionId)
                    .IsUnique();
            });

            modelBuilder.Entity<UserEntitlementOverride>(entity =>
            {
                entity.ToTable("UserEntitlementOverrides");

                entity.Property(item => item.Plan)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(16);

                entity.HasIndex(item => item.UserId)
                    .IsUnique();

                entity.HasIndex(item => item.ExpiresAtUtc);

                entity.HasOne(item => item.User)
                    .WithOne(user => user.EntitlementOverride)
                    .HasForeignKey<UserEntitlementOverride>(item => item.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AdminAuditLog>(entity =>
            {
                entity.ToTable("AdminAuditLogs");

                entity.Property(item => item.ActorEmail)
                    .IsRequired()
                    .HasMaxLength(254);

                entity.Property(item => item.Action)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(item => item.TargetEmail)
                    .HasMaxLength(254);

                entity.Property(item => item.Details)
                    .HasMaxLength(1024);

                entity.HasIndex(item => item.CreatedAtUtc);
                entity.HasIndex(item => item.ActorUserId);
                entity.HasIndex(item => item.TargetUserId);
                entity.HasIndex(item => item.Action);
            });

            modelBuilder.Entity<ManagedComponentCatalogItem>(entity =>
            {
                entity.ToTable("ManagedComponentCatalogItems");

                entity.Property(item => item.CatalogTypeId)
                    .IsRequired()
                    .HasMaxLength(96);

                entity.Property(item => item.TemplateCatalogTypeId)
                    .IsRequired()
                    .HasMaxLength(96);

                entity.Property(item => item.DisplayName)
                    .IsRequired()
                    .HasMaxLength(160);

                entity.Property(item => item.Category)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(item => item.Description)
                    .HasMaxLength(2000);

                entity.Property(item => item.Manufacturer)
                    .HasMaxLength(160);

                entity.Property(item => item.Mpn)
                    .HasMaxLength(128);

                entity.Property(item => item.ReferencePriceEur)
                    .HasPrecision(12, 2);

                entity.Property(item => item.DatasheetUrl)
                    .HasMaxLength(1024);

                entity.Property(item => item.ImageUrl)
                    .HasMaxLength(1024);

                entity.Property(item => item.Model3dUrl)
                    .HasMaxLength(1024);

                entity.Property(item => item.ModelScale)
                    .HasDefaultValue(1d);

                entity.Property(item => item.IsDeleted)
                    .HasDefaultValue(false);

                entity.HasIndex(item => item.CatalogTypeId)
                    .IsUnique();

                entity.HasIndex(item => item.TemplateCatalogTypeId);
                entity.HasIndex(item => item.Category);
                entity.HasIndex(item => new { item.IsDeleted, item.IsActive });
                entity.HasIndex(item => item.SortOrder);
            });

            modelBuilder.Entity<BillingWebhookEvent>(entity =>
            {
                entity.ToTable("BillingWebhookEvents");

                entity.Property(item => item.Provider)
                    .IsRequired()
                    .HasMaxLength(16);

                entity.Property(item => item.ProviderEnvironment)
                    .IsRequired()
                    .HasMaxLength(16);

                entity.Property(item => item.ProviderEventId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(item => item.EventType)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(item => item.PayloadSha256)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.HasIndex(item => new
                    {
                        item.ProviderEnvironment,
                        item.ProviderEventId
                    })
                    .IsUnique();
            });

            modelBuilder.Entity<UsageCounter>(entity =>
            {
                entity.ToTable("UsageCounters");

                entity.HasKey(counter => new
                {
                    counter.UserId,
                    counter.Metric,
                    counter.PeriodStartUtc
                });

                entity.Property(counter => counter.Metric)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(counter => counter.Count)
                    .HasDefaultValue(0);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(counter => counter.UserId)
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

            modelBuilder.Entity<SchemaEditorState>(entity =>
            {
                entity.ToTable("SchemaEditorStates");
                entity.HasKey(state => state.ProjectId);

                entity.Property(state => state.Mode)
                    .IsRequired()
                    .HasMaxLength(16);

                // Revision is the optimistic-concurrency token for editor saves
                // and project regenerations.
                entity.Property(state => state.Revision)
                    .IsConcurrencyToken();

                entity.Property(state => state.DocumentJson)
                    .HasColumnType("longtext");

                entity.HasOne(state => state.Project)
                    .WithOne(project => project.SchemaEditorState)
                    .HasForeignKey<SchemaEditorState>(state => state.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
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

            // Unconnected terminals have no cable; removing a wire must not
            // delete the terminal that belongs to an electrical component.
            modelBuilder.Entity<Port>()
                .HasOne(port => port.cableType)
                .WithMany()
                .HasForeignKey(port => port.CableId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Component>()
                .HasMany(component => component.Ports)
                .WithOne(port => port.Component)
                .HasForeignKey(port => port.ComponentId);

            modelBuilder.Entity<FuseBoxComponentGroup>()
                .HasMany(group => group.Components)
                .WithOne(component => component.FuseBoxComponentGroup)
                .HasForeignKey(component => component.FuseBoxComponentGroupId);

            modelBuilder.Entity<Consumer>()
                .Property(consumer => consumer.CatalogTypeId)
                .HasMaxLength(64);

            modelBuilder.Entity<Consumer>()
                .Property(consumer => consumer.PowerSource)
                .HasMaxLength(16);

            modelBuilder.Entity<Consumer>()
                .Property(consumer => consumer.BreakerAmperage)
                .HasDefaultValue(16);

            modelBuilder.Entity<Consumer>()
                .Property(consumer => consumer.RcdMilliAmps)
                .HasDefaultValue(30);
        }
    }
}
