using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Data
{
    // DbContext chính của hệ thống. Các DbSet (RoomType, Room, Reservation, Stay, Folio...)
    // sẽ được bổ sung khi thiết kế Database, sau đó tạo migration:
    //   dotnet ef migrations add InitialCreate
    //   dotnet ef database update
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
        {
        }
    }
}
