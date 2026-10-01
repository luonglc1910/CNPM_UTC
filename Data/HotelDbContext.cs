using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Data;

/// <summary>
/// DbContext chính của hệ thống. Mô hình dữ liệu bám theo REQUIREMENTS.md mục 7.
///
/// Tạo lại database:
///   dotnet ef migrations add InitialCreate
///   dotnet ef database update
/// </summary>
public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Id nhân viên đang thao tác, dùng để điền CreatedBy/UpdatedBy tự động.
    /// Tầng controller gán giá trị này sau khi xác thực.
    /// </summary>
    public int? CurrentUserId { get; set; }

    // Danh mục & cấu hình
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<HotelService> HotelServices => Set<HotelService>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    // Khách hàng
    public DbSet<Guest> Guests => Set<Guest>();

    // Đặt phòng
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationRoom> ReservationRooms => Set<ReservationRoom>();

    // Lưu trú
    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<StayGuest> StayGuests => Set<StayGuest>();
    public DbSet<RoomChangeLog> RoomChangeLogs => Set<RoomChangeLog>();

    // Buồng phòng
    public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    // Thu ngân
    public DbSet<Folio> Folios => Set<Folio>();
    public DbSet<FolioItem> FolioItems => Set<FolioItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();

    // Quản trị
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Mọi cột tiền dùng decimal(18,2) - tránh cảnh báo mất độ chính xác của EF Core.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCatalog(modelBuilder);
        ConfigureGuests(modelBuilder);
        ConfigureReservations(modelBuilder);
        ConfigureStays(modelBuilder);
        ConfigureHousekeeping(modelBuilder);
        ConfigureBilling(modelBuilder);
        ConfigureAdmin(modelBuilder);

        // Mặc định không xóa dây chuyền: dữ liệu đã phát sinh giao dịch không được xóa cứng
        // (REQUIREMENTS mục 7). Các quan hệ cha-con thực sự sẽ bật Cascade riêng bên dưới.
        foreach (var fk in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys())
                     .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade))
        {
            if (!CascadeRelationships.Contains((fk.DeclaringEntityType.ClrType, fk.PrincipalEntityType.ClrType)))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    /// <summary>Các quan hệ cha-con được phép xóa dây chuyền (con không có ý nghĩa nếu thiếu cha).</summary>
    private static readonly HashSet<(Type Dependent, Type Principal)> CascadeRelationships = new()
    {
        (typeof(ReservationRoom), typeof(Reservation)),
        (typeof(StayGuest), typeof(Stay)),
        (typeof(RoomChangeLog), typeof(Stay)),
        (typeof(Folio), typeof(Stay)),
        (typeof(FolioItem), typeof(Folio))
    };

    private static void ConfigureCatalog(ModelBuilder b)
    {
        b.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Status);
        });

        b.Entity<RoomType>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<Room>(e =>
        {
            // FR-A02: số phòng duy nhất toàn hệ thống.
            e.HasIndex(x => x.RoomNumber).IsUnique();
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.RoomType)
                .WithMany(t => t.Rooms)
                .HasForeignKey(x => x.RoomTypeId);
        });

        b.Entity<HotelService>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<InventoryTransaction>(e =>
        {
            e.HasIndex(x => new { x.HotelServiceId, x.CreatedAt });

            e.HasOne(x => x.HotelService)
                .WithMany(s => s.InventoryTransactions)
                .HasForeignKey(x => x.HotelServiceId);

            e.HasOne(x => x.FolioItem)
                .WithMany()
                .HasForeignKey(x => x.FolioItemId);
        });

        b.Entity<SystemSetting>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
        });

        b.Entity<NumberSequence>(e =>
        {
            // Cấp số chứng từ: mỗi cặp (tiền tố, kỳ) là một dòng đếm duy nhất — FR-C04, FR-F05.
            e.HasIndex(x => new { x.Prefix, x.Period }).IsUnique();
        });
    }

    private static void ConfigureGuests(ModelBuilder b)
    {
        b.Entity<Guest>(e =>
        {
            // Số giấy tờ duy nhất, nhưng cho phép nhiều bản ghi bỏ trống (khách chưa xuất trình).
            e.HasIndex(x => x.IdNumber)
                .IsUnique()
                .HasFilter("[IdNumber] <> ''");

            e.HasIndex(x => x.PhoneNumber);
            e.HasIndex(x => x.FullName);
        });
    }

    private static void ConfigureReservations(ModelBuilder b)
    {
        b.Entity<Reservation>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();

            // Bộ lọc mặc định của màn hình danh sách đơn (SCR-C01).
            e.HasIndex(x => new { x.CheckInDate, x.Status });

            e.HasOne(x => x.PrimaryGuest)
                .WithMany(g => g.Reservations)
                .HasForeignKey(x => x.PrimaryGuestId);
        });

        b.Entity<ReservationRoom>(e =>
        {
            // BR-06: truy vấn kiểm tra trùng phòng chạy trên cột này.
            e.HasIndex(x => x.RoomId);

            e.HasOne(x => x.Reservation)
                .WithMany(r => r.Rooms)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.RoomType)
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId);

            e.HasOne(x => x.Room)
                .WithMany(r => r.ReservationRooms)
                .HasForeignKey(x => x.RoomId);
        });
    }

    private static void ConfigureStays(ModelBuilder b)
    {
        b.Entity<Stay>(e =>
        {
            e.HasIndex(x => new { x.RoomId, x.ActualCheckIn, x.ActualCheckOut });
            e.HasIndex(x => x.Status);

            // Chốt chặn ở tầng database cho BR-06 / NFR-03: một phòng chỉ được có
            // đúng một lượt lưu trú đang mở. Kiểm tra ở tầng service vẫn cần, nhưng
            // index này chặn được trường hợp hai lễ tân bấm check-in cùng lúc.
            e.HasIndex(x => x.RoomId)
                .IsUnique()
                .HasDatabaseName("UX_Stay_ActiveRoom")
                .HasFilter("[Status] = 1");

            e.HasOne(x => x.Reservation)
                .WithMany(r => r.Stays)
                .HasForeignKey(x => x.ReservationId);

            e.HasOne(x => x.Room)
                .WithMany(r => r.Stays)
                .HasForeignKey(x => x.RoomId);

            e.HasOne(x => x.PrimaryGuest)
                .WithMany()
                .HasForeignKey(x => x.PrimaryGuestId);
        });

        b.Entity<StayGuest>(e =>
        {
            e.HasIndex(x => new { x.StayId, x.GuestId }).IsUnique();

            e.HasOne(x => x.Stay)
                .WithMany(s => s.Guests)
                .HasForeignKey(x => x.StayId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Guest)
                .WithMany(g => g.StayGuests)
                .HasForeignKey(x => x.GuestId);
        });

        b.Entity<RoomChangeLog>(e =>
        {
            e.HasOne(x => x.Stay)
                .WithMany(s => s.RoomChanges)
                .HasForeignKey(x => x.StayId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.FromRoom)
                .WithMany()
                .HasForeignKey(x => x.FromRoomId);

            e.HasOne(x => x.ToRoom)
                .WithMany()
                .HasForeignKey(x => x.ToRoomId);
        });
    }

    private static void ConfigureHousekeeping(ModelBuilder b)
    {
        b.Entity<HousekeepingTask>(e =>
        {
            e.HasIndex(x => new { x.RoomId, x.Status });

            // Một phòng chỉ có đúng một nhiệm vụ dọn đang mở (Pending=1 hoặc InProgress=2) —
            // SCR-E01. Chặn ở tầng database để hai lễ tân không tạo trùng nhiệm vụ cùng lúc.
            e.HasIndex(x => x.RoomId)
                .IsUnique()
                .HasDatabaseName("UX_Housekeeping_OpenPerRoom")
                .HasFilter("[Status] IN (1, 2)");

            e.HasOne(x => x.Room)
                .WithMany(r => r.HousekeepingTasks)
                .HasForeignKey(x => x.RoomId);

            e.HasOne(x => x.AssignedEmployee)
                .WithMany()
                .HasForeignKey(x => x.AssignedTo);

            e.HasOne(x => x.Stay)
                .WithMany()
                .HasForeignKey(x => x.StayId);
        });

        b.Entity<ServiceRequest>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => new { x.Status, x.Priority });

            e.HasOne(x => x.Room)
                .WithMany(r => r.ServiceRequests)
                .HasForeignKey(x => x.RoomId);

            e.HasOne(x => x.AssignedEmployee)
                .WithMany()
                .HasForeignKey(x => x.AssignedTo);
        });
    }

    private static void ConfigureBilling(ModelBuilder b)
    {
        b.Entity<Folio>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();

            // Mỗi lượt lưu trú đúng một folio, kể cả khi đổi phòng nhiều lần (BR-09).
            e.HasOne(x => x.Stay)
                .WithOne(s => s.Folio)
                .HasForeignKey<Folio>(x => x.StayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<FolioItem>(e =>
        {
            e.HasIndex(x => new { x.FolioId, x.ItemType });

            e.HasOne(x => x.Folio)
                .WithMany(f => f.Items)
                .HasForeignKey(x => x.FolioId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.HotelService)
                .WithMany()
                .HasForeignKey(x => x.HotelServiceId);
        });

        b.Entity<Invoice>(e =>
        {
            // FR-F05: số hóa đơn liên tục, không trùng, không cấp lại kể cả khi hủy.
            e.HasIndex(x => x.InvoiceNo).IsUnique();
            e.HasIndex(x => new { x.IssuedAt, x.Status });

            e.HasOne(x => x.Folio)
                .WithOne(f => f.Invoice)
                .HasForeignKey<Invoice>(x => x.FolioId);

            e.HasOne(x => x.CashierShift)
                .WithMany(s => s.Invoices)
                .HasForeignKey(x => x.CashierShiftId);
        });

        b.Entity<Payment>(e =>
        {
            e.HasIndex(x => new { x.CashierShiftId, x.PaidAt });

            e.HasOne(x => x.CashierShift)
                .WithMany(s => s.Payments)
                .HasForeignKey(x => x.CashierShiftId);

            e.HasOne(x => x.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(x => x.InvoiceId);

            e.HasOne(x => x.Deposit)
                .WithMany(d => d.Payments)
                .HasForeignKey(x => x.DepositId);

            e.HasOne(x => x.Reservation)
                .WithMany()
                .HasForeignKey(x => x.ReservationId);
        });

        b.Entity<Deposit>(e =>
        {
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.Reservation)
                .WithMany(r => r.Deposits)
                .HasForeignKey(x => x.ReservationId);

            e.HasOne(x => x.Stay)
                .WithMany(s => s.Deposits)
                .HasForeignKey(x => x.StayId);
        });

        b.Entity<CashierShift>(e =>
        {
            e.HasIndex(x => new { x.EmployeeId, x.Status });

            // Một nhân viên chỉ được có đúng một ca đang mở (Open=1) tại một thời điểm —
            // BR-10, SCR-F08. Chặn ở tầng database, không chỉ dựa vào kiểm tra ở service.
            e.HasIndex(x => x.EmployeeId)
                .IsUnique()
                .HasDatabaseName("UX_Shift_OpenPerEmployee")
                .HasFilter("[Status] = 1");

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.Shifts)
                .HasForeignKey(x => x.EmployeeId);
        });
    }

    private static void ConfigureAdmin(ModelBuilder b)
    {
        b.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.UserId);
        });
    }

    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Tự điền CreatedAt/CreatedBy/UpdatedAt/UpdatedBy — NFR-07.</summary>
    private void ApplyAuditFields()
    {
        var now = DateTime.Now;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= CurrentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = CurrentUserId;
                    // Không cho sửa ngược thông tin tạo bản ghi.
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedBy).IsModified = false;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditLog>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }
    }
}
