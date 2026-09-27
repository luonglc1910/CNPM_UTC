import type { ApiAvailabilityGroup, ApiRoomType } from "../api/booking-api";
import type { AmenityItem, RoomBadge } from "../types";

// Map tiếng Việt từ DB → icon + label chuẩn để hiển thị
// DB lưu dạng: "Điều hòa, TV, Nóng lạnh, Wifi, Minibar, Bồn tắm"
const AMENITY_MAP: Record<string, { label: string; icon: string; key: string }> = {
  "điều hòa": { label: "Điều hòa", icon: "Wind", key: "ac" },
  "tv": { label: "TV màn hình phẳng", icon: "Tv", key: "tv" },
  "smart tv": { label: "Smart TV", icon: "Tv", key: "tv" },
  "nóng lạnh": { label: "Vòi hoa sen nóng lạnh", icon: "Bath", key: "shower" },
  "wifi": { label: "WiFi miễn phí", icon: "Wifi", key: "wifi" },
  "minibar": { label: "Minibar", icon: "Wine", key: "minibar" },
  "bồn tắm": { label: "Bồn tắm", icon: "Bath", key: "bathtub" },
  "phòng khách riêng": { label: "Phòng khách riêng", icon: "Building2", key: "living" },
  "ban công": { label: "Ban công", icon: "Building2", key: "balcony" },
  "hồ bơi": { label: "Hồ bơi", icon: "Waves", key: "pool" },
  "phòng gym": { label: "Phòng gym", icon: "Dumbbell", key: "gym" },
  "gym": { label: "Phòng gym", icon: "Dumbbell", key: "gym" },
  "ăn sáng": { label: "Bao gồm ăn sáng", icon: "Coffee", key: "breakfast" },
  "breakfast": { label: "Bao gồm ăn sáng", icon: "Coffee", key: "breakfast" },
  "két an toàn": { label: "Két an toàn", icon: "Lock", key: "safe" },
  "bãi đỗ xe": { label: "Bãi đỗ xe", icon: "Car", key: "parking" },
  "spa": { label: "Spa & Wellness", icon: "Sparkles", key: "spa" },
  "air conditioning": { label: "Điều hòa", icon: "Wind", key: "ac" },
  "bathtub": { label: "Bồn tắm", icon: "Bath", key: "bathtub" },
  "balcony": { label: "Ban công", icon: "Building2", key: "balcony" },
  "pool": { label: "Hồ bơi", icon: "Waves", key: "pool" },
};

export function parseAmenities(raw: string | null): AmenityItem[] {
  if (!raw) return [];
  const seen = new Set<string>();
  return raw
    .split(",")
    .map((k) => k.trim().toLowerCase())
    .filter((k) => k.length > 0 && AMENITY_MAP[k])
    .filter((k) => {
      const key = AMENITY_MAP[k].key;
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    })
    .map((k) => ({ key: AMENITY_MAP[k].key, label: AMENITY_MAP[k].label, icon: AMENITY_MAP[k].icon }));
}

export function buildBadgesFromApi(
  group: ApiAvailabilityGroup,
  rt: ApiRoomType | undefined
): RoomBadge[] {
  const badges: RoomBadge[] = [];
  badges.push({ label: "Hủy miễn phí", variant: "success" });
  const amenityKeys = (rt?.amenities ?? "").toLowerCase().split(",").map((k) => k.trim());
  if (amenityKeys.some((k) => k.includes("ăn sáng") || k === "breakfast"))
    badges.push({ label: "Bao gồm ăn sáng", variant: "info" });
  if (amenityKeys.some((k) => k.includes("hồ bơi") || k === "pool"))
    badges.push({ label: "Hồ bơi", variant: "info" });
  if (amenityKeys.some((k) => k.includes("spa")))
    badges.push({ label: "Spa & Wellness", variant: "info" });
  if (group.availableCount === 1)
    badges.push({ label: "Chỉ còn 1 phòng!", variant: "destructive" });
  else if (group.availableCount <= 3)
    badges.push({ label: `Còn ${group.availableCount} phòng`, variant: "warning" });
  return badges;
}

export function formatVND(amount: number): string {
  return new Intl.NumberFormat("vi-VN", { style: "decimal", minimumFractionDigits: 0 }).format(amount);
}
