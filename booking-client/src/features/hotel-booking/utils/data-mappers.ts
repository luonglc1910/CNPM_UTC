import type {
  DbRoomType,
  DbAvailableRoom,
  AmenityItem,
  RoomBadge,
  RoomCardViewModel,
} from "../types";

// ============================================================
// Amenity Parser — DB lưu dạng "Wifi,TV,Minibar,Pool,Gym"
// ============================================================
const AMENITY_MAP: Record<string, { label: string; icon: string }> = {
  wifi: { label: "WiFi miễn phí", icon: "Wifi" },
  tv: { label: "TV màn hình phẳng", icon: "Tv" },
  minibar: { label: "Minibar", icon: "Wine" },
  pool: { label: "Hồ bơi", icon: "Waves" },
  gym: { label: "Phòng gym", icon: "Dumbbell" },
  breakfast: { label: "Bao gồm ăn sáng", icon: "Coffee" },
  ac: { label: "Điều hòa", icon: "Wind" },
  bathtub: { label: "Bồn tắm", icon: "Bath" },
  balcony: { label: "Ban công", icon: "Building2" },
  safe: { label: "Két an toàn", icon: "Lock" },
  parking: { label: "Bãi đỗ xe", icon: "Car" },
  spa: { label: "Spa & Wellness", icon: "Sparkles" },
};

function parseAmenities(raw: string | null): AmenityItem[] {
  if (!raw) return [];
  return raw
    .split(",")
    .map((k) => k.trim().toLowerCase())
    .filter((k) => AMENITY_MAP[k])
    .map((k) => ({ key: k, ...AMENITY_MAP[k] }));
}

// ============================================================
// Badge Builder — từ các thuộc tính của RoomType
// ============================================================
function buildBadges(rt: DbRoomType, availableCount: number): RoomBadge[] {
  const badges: RoomBadge[] = [];

  // Badge "Hủy miễn phí" — business rule: hiện có sẵn, backend cần thêm CancellationPolicy
  badges.push({ label: "Hủy miễn phí", variant: "success" });

  // Badge dựa trên amenities
  const amenityKeys = (rt.amenities ?? "")
    .toLowerCase()
    .split(",")
    .map((k) => k.trim());
  if (amenityKeys.includes("breakfast")) {
    badges.push({ label: "Bao gồm ăn sáng", variant: "info" });
  }
  if (amenityKeys.includes("pool")) {
    badges.push({ label: "Hồ bơi", variant: "info" });
  }

  // Badge cảnh báo phòng sắp hết
  if (availableCount === 1) {
    badges.push({ label: "Chỉ còn 1 phòng!", variant: "destructive" });
  } else if (availableCount <= 3) {
    badges.push({ label: `Còn ${availableCount} phòng`, variant: "warning" });
  }

  return badges;
}

// ============================================================
// Placeholder images — TODO: Backend cần bổ sung ImageUrls vào RoomType
// ============================================================
const ROOM_IMAGE_PLACEHOLDERS: Record<string, string[]> = {
  default: [
    "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=800&q=80",
    "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=800&q=80",
  ],
  suite: [
    "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80",
    "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800&q=80",
  ],
  deluxe: [
    "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?auto=format&fit=crop&w=800&q=80",
    "https://images.unsplash.com/photo-1615460549969-36fa19521a4f?auto=format&fit=crop&w=800&q=80",
  ],
};

function getRoomImages(name: string): string[] {
  const lower = name.toLowerCase();
  if (lower.includes("suite")) return ROOM_IMAGE_PLACEHOLDERS.suite;
  if (lower.includes("deluxe") || lower.includes("superior"))
    return ROOM_IMAGE_PLACEHOLDERS.deluxe;
  return ROOM_IMAGE_PLACEHOLDERS.default;
}

// ============================================================
// Main Mapper: DbRoomType + AvailableRoom[] → RoomCardViewModel
// ============================================================
export function mapRoomTypeToViewModel(
  roomType: DbRoomType,
  availableRooms: DbAvailableRoom[],
): RoomCardViewModel {
  const available = availableRooms.filter((r) => r.roomTypeId === roomType.id);
  const availableCount = available.length;
  const totalRooms = roomType.rooms?.length ?? 0;

  return {
    roomTypeId: roomType.id,
    code: roomType.code,
    name: roomType.name,
    description:
      roomType.description ??
      `Phòng ${roomType.name} tiêu chuẩn tại Lumina Hotel.`,
    pricePerNight: roomType.basePricePerNight,
    priceFirstHour: roomType.priceFirstHour,
    priceExtraHour: roomType.priceExtraHour,
    priceOvernight: roomType.priceOvernight,
    extraGuestFee: roomType.extraGuestFeePerNight,
    standardCapacity: roomType.standardCapacity,
    maxCapacity: roomType.maxCapacity,
    availableRooms: availableCount,
    totalRooms,
    amenities: parseAmenities(roomType.amenities),
    badges: buildBadges(roomType, availableCount),
    images: getRoomImages(roomType.name),
    // Computed fields — TODO: tích hợp review module từ backend
    rating: null,
    reviewCount: null,
  };
}

/** Group AvailableRoom[] theo roomTypeId → dùng cho listing page */
export function groupAvailableByRoomType(
  roomTypes: DbRoomType[],
  available: DbAvailableRoom[],
): RoomCardViewModel[] {
  return roomTypes
    .filter((rt) => rt.isActive)
    .map((rt) => mapRoomTypeToViewModel(rt, available))
    .filter((vm) => vm.availableRooms > 0);
}

// ============================================================
// Price formatter
// ============================================================
export function formatVND(amount: number): string {
  return new Intl.NumberFormat("vi-VN", {
    style: "decimal",
    minimumFractionDigits: 0,
  }).format(amount);
}
