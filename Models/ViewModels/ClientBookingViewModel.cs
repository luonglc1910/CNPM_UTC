using System;

namespace HotelManagement.Web.Models.ViewModels
{
    public class ClientBookingViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        
        public string RentalType { get; set; } = "Daily";
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        
        public int Guests { get; set; }
        public bool ExtraBed { get; set; }
        public string PromoCode { get; set; } = string.Empty;
        
        public decimal BasePrice { get; set; }
        public int NightsOrHours { get; set; }
        public decimal TotalRoomPrice { get; set; }
        public decimal ExtraGuestFee { get; set; }
        public decimal ExtraBedFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class ClientBookingPostModel
    {
        public int RoomId { get; set; }
        public string RentalType { get; set; } = "Daily";
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Guests { get; set; }
        public bool ExtraBed { get; set; }
        public string PromoCode { get; set; } = string.Empty;
        
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string ArrivalTime { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
