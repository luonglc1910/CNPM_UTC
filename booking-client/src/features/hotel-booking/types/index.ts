// ============================================================
// Data Contracts — ánh xạ 1-1 với DB Schema của ASP.NET Core backend
// ============================================================

/** Khớp với Models/Entities/RoomType.cs */
export interface DbRoomType {
  id: number;
  code: string;
  name: string;
  standardCapacity: number;
  maxCapacity: number;
  basePricePerNight: number;
  extraGuestFeePerNight: number;
  extraBedFeePerNight: number;
  priceFirstHour: number;
  priceExtraHour: number;
  priceOvernight: number;
  amenities: string | null; // comma-separated string trong DB
  description: string | null;
  isActive: boolean;
  rooms: DbRoom[];
}

/** Khớp với Models/Entities/Room.cs + RoomStatus enum */
export interface DbRoom {
  id: number;
  roomNumber: string;
  floor: number;
  roomTypeId: number;
  status:
    | "Available"
    | "Reserved"
    | "Occupied"
    | "Dirty"
    | "Maintenance"
    | "OutOfService";
  isActive: boolean;
}

/** Khớp với AvailableRoom DTO trong AvailabilityService.cs */
export interface DbAvailableRoom {
  roomId: number;
  roomNumber: string;
  floor: number;
  roomTypeId: number;
  roomTypeCode: string;
  roomTypeName: string;
  pricePerNight: number;
  priceFirstHour: number;
  priceExtraHour: number;
  priceOvernight: number;
  standardCapacity: number;
  maxCapacity: number;
  needsCleaning: boolean;
}

// ============================================================
// ViewModels — UI-optimized, mapped từ DB contracts
// ============================================================

/** Tiện nghi đã parse từ string Amenities của DB */
export interface AmenityItem {
  key: string;
  label: string;
  icon: string; // Lucide icon name
}

/** Nhãn (badge) hiển thị trên Room Card */
export interface RoomBadge {
  label: string;
  variant: "success" | "info" | "warning" | "destructive";
}

/** ViewModel hoàn chỉnh cho RoomCard & FilterSidebar */
export interface RoomCardViewModel {
  roomTypeId: number;
  code: string;
  name: string;
  description: string;

  // Giá (theo loại thuê)
  pricePerNight: number;
  priceFirstHour: number;
  priceExtraHour: number;
  priceOvernight: number;
  extraGuestFee: number;

  // Sức chứa
  standardCapacity: number;
  maxCapacity: number;

  // Phòng còn trống (computed từ AvailableRoom[])
  availableRooms: number;
  totalRooms: number;

  // Tiện nghi
  amenities: AmenityItem[];

  // Badges UI
  badges: RoomBadge[];

  // Ảnh (TODO: backend cần thêm field ImageUrls — hiện dùng placeholder)
  images: string[];

  // Điểm đánh giá (computed field — backend cần tích hợp review module)
  rating: number | null;
  reviewCount: number | null;
}

/** State của bộ lọc, sync với URL query params */
export interface FilterState {
  checkIn: string; // ISO date string
  checkOut: string; // ISO date string
  adults: number;
  children: number;
  rooms: number;
  minPrice: number;
  maxPrice: number;
  amenities: string[]; // các key amenity được chọn
  rentalType: "daily" | "hourly" | "overnight" | "all";
}
