using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models;
using HotelManagement.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

public interface ILoyaltyService
{
    Task<int> GetGuestTotalPointsAsync(int guestId);
    Task<bool> AddEarnPointsAsync(int guestId, int? bookingId, decimal totalAmount, string description);
    Task<bool> RedeemPointsAsync(int guestId, int? bookingId, int pointsToRedeem, string description);
    Task EvaluateTierAsync(int guestId);
}

public class LoyaltyService : ILoyaltyService
{
    private readonly HotelDbContext _db;

    public LoyaltyService(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetGuestTotalPointsAsync(int guestId)
    {
        var total = await _db.PointTransactions
            .Where(x => x.GuestId == guestId)
            .SumAsync(x => x.Amount);
        
        return total;
    }

    public async Task<bool> AddEarnPointsAsync(int guestId, int? bookingId, decimal totalAmount, string description)
    {
        // Sử dụng Transaction
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var guest = await _db.Guests.FindAsync(guestId);
            if (guest == null) return false;

            // Tính hệ số nhân dựa theo hạng thẻ
            decimal multiplier = guest.Tier switch
            {
                MemberTier.Diamond => 2.0m,
                MemberTier.Gold => 1.5m,
                MemberTier.Silver => 1.2m,
                _ => 1.0m
            };

            // Tiêu 100k = 1 điểm x hệ số nhân
            int pointsEarned = (int)Math.Floor((totalAmount / 100000m) * multiplier);

            if (pointsEarned <= 0) return true; // Không có điểm để cộng thì vẫn coi là xử lý xong

            var pt = new PointTransaction
            {
                GuestId = guestId,
                BookingId = bookingId,
                Type = PointTransactionType.Earn,
                Amount = pointsEarned,
                Description = description,
                CreatedAt = DateTime.Now
            };

            _db.PointTransactions.Add(pt);
            
            // Cập nhật điểm cache trên Guest (để load nhanh thay vì tính SUM liên tục)
            guest.RewardPoints += pointsEarned;
            
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Đánh giá thăng hạng ngay sau khi có điểm mới
            await EvaluateTierAsync(guestId);

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> RedeemPointsAsync(int guestId, int? bookingId, int pointsToRedeem, string description)
    {
        if (pointsToRedeem <= 0) return false;

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var guest = await _db.Guests.FindAsync(guestId);
            if (guest == null) return false;

            // Tính toán tổng điểm chính xác từ Sổ cái
            var currentPoints = await _db.PointTransactions
                .Where(x => x.GuestId == guestId)
                .SumAsync(x => x.Amount);

            if (currentPoints < pointsToRedeem)
            {
                // Không đủ điểm
                return false;
            }

            var pt = new PointTransaction
            {
                GuestId = guestId,
                BookingId = bookingId,
                Type = PointTransactionType.Redeem,
                Amount = -pointsToRedeem,
                Description = description,
                CreatedAt = DateTime.Now
            };

            _db.PointTransactions.Add(pt);

            // Cập nhật điểm cache trên Guest
            guest.RewardPoints = currentPoints - pointsToRedeem;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task EvaluateTierAsync(int guestId)
    {
        var guest = await _db.Guests.FindAsync(guestId);
        if (guest == null) return;

        // Tính tổng điểm trong 365 ngày qua
        var pointsInLastYear = await _db.PointTransactions
            .Where(x => x.GuestId == guestId 
                        && x.Type == PointTransactionType.Earn 
                        && x.CreatedAt >= DateTime.Now.AddDays(-365))
            .SumAsync(x => x.Amount);

        // Logic xếp hạng
        MemberTier newTier = MemberTier.Standard;
        if (pointsInLastYear >= 500)
            newTier = MemberTier.Diamond;
        else if (pointsInLastYear >= 200)
            newTier = MemberTier.Gold;
        else if (pointsInLastYear >= 50)
            newTier = MemberTier.Silver;

        // Nếu hạng mới cao hơn hạng hiện tại, hoặc hệ thống quét hàng năm hạ hạng
        if (guest.Tier != newTier)
        {
            guest.Tier = newTier;
            await _db.SaveChangesAsync();
        }
    }
}
