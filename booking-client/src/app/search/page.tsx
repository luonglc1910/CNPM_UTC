"use client";
import React, { useState, useCallback, useMemo, useEffect, Suspense } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { SlidersHorizontal, ArrowUpDown, AlertCircle, RefreshCw } from "lucide-react";
import SearchBar from "@/features/hotel-booking/components/SearchBar";
import RoomCard, { RoomCardSkeleton } from "@/features/hotel-booking/components/RoomCard";
import FilterSidebar from "@/features/hotel-booking/components/FilterSidebar";
import type { RoomCardViewModel, FilterState } from "@/features/hotel-booking/types";
import {
  getAvailability,
  getRoomTypes,
  type ApiAvailabilityGroup,
  type ApiRoomType,
} from "@/features/hotel-booking/api/booking-api";
import { parseAmenities, buildBadgesFromApi, formatVND } from "@/features/hotel-booking/utils";


// ────────────────────────────────────────────────────────────
// Adapter: API → RoomCardViewModel
// ────────────────────────────────────────────────────────────
const ROOM_IMAGES: Record<string, string[]> = {
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

function getImages(name: string) {
  const lower = name.toLowerCase();
  if (lower.includes("suite")) return ROOM_IMAGES.suite;
  if (lower.includes("deluxe") || lower.includes("superior")) return ROOM_IMAGES.deluxe;
  return ROOM_IMAGES.default;
}

function adaptToViewModel(
  group: ApiAvailabilityGroup,
  roomTypeMap: Map<number, ApiRoomType>
): RoomCardViewModel {
  const rt = roomTypeMap.get(group.roomTypeId);
  return {
    roomTypeId: group.roomTypeId,
    code: group.roomTypeCode,
    name: group.roomTypeName,
    description: rt?.description ?? `Phòng ${group.roomTypeName} tiêu chuẩn tại Lumina Hotel.`,
    pricePerNight: group.pricePerNight,
    priceFirstHour: group.priceFirstHour,
    priceExtraHour: group.priceExtraHour,
    priceOvernight: group.priceOvernight,
    extraGuestFee: rt?.extraGuestFeePerNight ?? 0,
    standardCapacity: group.standardCapacity,
    maxCapacity: group.maxCapacity,
    availableRooms: group.availableCount,
    totalRooms: rt?.totalRooms ?? 0,
    amenities: parseAmenities(rt?.amenities ?? null),
    badges: buildBadgesFromApi(group, rt),
    images: getImages(group.roomTypeName),
    rating: null,
    reviewCount: null,
  };
}

// ────────────────────────────────────────────────────────────
function SearchPageContent() {
  const searchParams = useSearchParams();
  const router = useRouter();

  const today = new Date().toISOString().split("T")[0];
  const tomorrow = new Date(Date.now() + 86400000).toISOString().split("T")[0];

  const [filter, setFilter] = useState<FilterState>({
    checkIn: searchParams.get("checkIn") ?? today,
    checkOut: searchParams.get("checkOut") ?? tomorrow,
    adults: Number(searchParams.get("adults") ?? 2),
    children: Number(searchParams.get("children") ?? 0),
    rooms: Number(searchParams.get("rooms") ?? 1),
    minPrice: 0,
    maxPrice: 99999999,
    amenities: [],
    rentalType: "all",
  });

  const [rawGroups, setRawGroups] = useState<ApiAvailabilityGroup[]>([]);
  const [roomTypeMap, setRoomTypeMap] = useState<Map<number, ApiRoomType>>(new Map());
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sortBy, setSortBy] = useState<"price_asc" | "price_desc" | "rating">("price_asc");
  const [isFilterMobileOpen, setIsFilterMobileOpen] = useState(false);

  // Load dữ liệu từ API
  const loadData = useCallback(async (ci: string, co: string, guests: number) => {
    setIsLoading(true);
    setError(null);
    try {
      const [availability, roomTypes] = await Promise.all([
        getAvailability(ci, co, guests),
        getRoomTypes(),
      ]);
      const map = new Map<number, ApiRoomType>(roomTypes.map(rt => [rt.id, rt]));
      setRoomTypeMap(map);
      setRawGroups(availability.groups);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Không thể tải dữ liệu phòng. Vui lòng thử lại.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Load khi filter ngày/khách thay đổi
  useEffect(() => {
    loadData(filter.checkIn, filter.checkOut, filter.adults);
  }, [filter.checkIn, filter.checkOut, filter.adults, loadData]);

  const handleFilterChange = useCallback((partial: Partial<FilterState>) => {
    setFilter(prev => ({ ...prev, ...partial }));
  }, []);

  const handleSearch = useCallback((vals: {
    location: string; checkIn: string; checkOut: string;
    adults: number; children: number; rooms: number;
  }) => {
    handleFilterChange({
      checkIn: vals.checkIn, checkOut: vals.checkOut,
      adults: vals.adults, children: vals.children, rooms: vals.rooms,
    });
  }, [handleFilterChange]);

  // Filter + Sort phía client
  const displayRooms = useMemo<RoomCardViewModel[]>(() => {
    let rooms = rawGroups
      .filter(g => {
        if (g.pricePerNight < filter.minPrice || g.pricePerNight > filter.maxPrice) return false;
        if (filter.rentalType === "hourly" && g.priceFirstHour === 0) return false;
        if (filter.rentalType === "overnight" && g.priceOvernight === 0) return false;
        if (filter.amenities.length > 0) {
          const rt = roomTypeMap.get(g.roomTypeId);
          const rtAmenities = (rt?.amenities ?? "").toLowerCase().split(",").map(k => k.trim());
          if (!filter.amenities.every(k => rtAmenities.includes(k))) return false;
        }
        if (g.maxCapacity < filter.adults) return false;
        return true;
      })
      .map(g => adaptToViewModel(g, roomTypeMap));

    return [...rooms].sort((a, b) => {
      if (sortBy === "price_asc") return a.pricePerNight - b.pricePerNight;
      if (sortBy === "price_desc") return b.pricePerNight - a.pricePerNight;
      return 0;
    });
  }, [rawGroups, roomTypeMap, filter, sortBy]);

  const minPrice = rawGroups.length > 0 ? Math.min(...rawGroups.map(g => g.pricePerNight)) : 0;
  const maxPrice = rawGroups.length > 0 ? Math.max(...rawGroups.map(g => g.pricePerNight)) : 99999999;

  return (
    <div className="min-h-screen bg-[#f7f9fa]">
      {/* Hero Search Section */}
      <div className="relative h-[45vh] min-h-[350px] flex items-end pb-12">
        <div className="absolute inset-0 z-0">
          <img src="/images/search_hero.jpg" alt="Lumina Exterior" className="w-full h-full object-cover" />
          <div className="absolute inset-0 bg-gradient-to-t from-luxury-navy/90 via-luxury-navy/40 to-transparent" />
        </div>
        <div className="relative z-10 w-full px-4 md:px-8">
          <div className="max-w-7xl mx-auto">
            <h1 className="text-3xl md:text-5xl font-bold text-white mb-8 tracking-tight drop-shadow-md">
              Tìm chốn nghỉ dưỡng hoàn hảo
            </h1>
            <SearchBar onSearch={handleSearch} initialValues={filter} sticky />
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-4 md:px-8 py-6 flex gap-6">
        {/* Sidebar */}
        <FilterSidebar
          filter={filter}
          onFilterChange={handleFilterChange}
          resultCount={displayRooms.length}
          isMobileOpen={isFilterMobileOpen}
          onMobileClose={() => setIsFilterMobileOpen(false)}
          minPrice={minPrice}
          maxPrice={maxPrice}
        />

        {/* Main */}
        <div className="flex-1 min-w-0">
          {/* Toolbar */}
          <div className="flex items-center justify-between mb-6 flex-wrap gap-3 pb-4 border-b border-zinc-200/60">
            <div>
              <p className="text-[14px] text-zinc-500">
                {isLoading ? (
                  <span className="inline-flex items-center gap-2">
                    <RefreshCw size={14} className="animate-spin" /> Đang tìm kiếm...
                  </span>
                ) : error ? (
                  <span className="text-rose-500">{error}</span>
                ) : (
                  <>Tìm thấy <span className="font-bold text-zinc-900">{displayRooms.length}</span> loại phòng trống</>
                )}
              </p>
              <p className="text-[13.5px] text-brand-primary font-medium mt-1">
                {filter.checkIn} → {filter.checkOut} · {filter.adults} người lớn
              </p>
            </div>
            <div className="flex items-center gap-4">
              <button
                onClick={() => setIsFilterMobileOpen(true)}
                className="md:hidden flex items-center gap-1.5 text-[13px] font-bold text-brand-primary border-2 border-brand-primary/20 px-4 py-2 rounded-full"
              >
                <SlidersHorizontal size={14} /> Bộ lọc
              </button>
              <div className="flex items-center gap-2 bg-white border border-zinc-200 rounded-xl px-3 py-1 shadow-sm focus-within:border-brand-primary transition-colors">
                <ArrowUpDown size={14} className="text-zinc-400" />
                <select
                  value={sortBy}
                  onChange={e => setSortBy(e.target.value as typeof sortBy)}
                  className="text-[13px] text-zinc-700 font-medium bg-transparent py-1.5 outline-none cursor-pointer"
                >
                  <option value="price_asc">Giá: Thấp → Cao</option>
                  <option value="price_desc">Giá: Cao → Thấp</option>
                </select>
              </div>
            </div>
          </div>

          {/* Room List */}
          <div className="flex flex-col gap-4">
            {isLoading ? (
              Array.from({ length: 3 }).map((_, i) => <RoomCardSkeleton key={i} />)
            ) : error ? (
              <div className="bg-white rounded-2xl border border-red-100 p-10 text-center shadow-sm">
                <AlertCircle size={40} className="text-red-400 mx-auto mb-4" />
                <h3 className="text-[17px] font-bold text-[#03121a] mb-2">Không thể kết nối</h3>
                <p className="text-[14px] text-[#687176] mb-6">{error}</p>
                <button
                  onClick={() => loadData(filter.checkIn, filter.checkOut, filter.adults)}
                  className="bg-[#0194f3] text-white font-bold px-6 py-2.5 rounded-xl hover:bg-[#007ce8]"
                >
                  Thử lại
                </button>
              </div>
            ) : displayRooms.length === 0 ? (
              <div className="bg-white rounded-2xl border border-gray-100 p-12 text-center shadow-sm">
                <div className="text-5xl mb-4">🏨</div>
                <h3 className="text-[18px] font-bold text-[#03121a] mb-2">Không có phòng phù hợp</h3>
                <p className="text-[14px] text-[#687176] mb-6">Thử thay đổi ngày hoặc xóa bộ lọc.</p>
                <button
                  onClick={() => setFilter(f => ({ ...f, minPrice: 0, maxPrice: 99999999, amenities: [], rentalType: "all" }))}
                  className="bg-[#0194f3] text-white font-bold px-6 py-2.5 rounded-xl hover:bg-[#007ce8]"
                >
                  Xóa tất cả bộ lọc
                </button>
              </div>
            ) : (
              displayRooms.map(room => (
                <RoomCard
                  key={room.roomTypeId}
                  vm={room}
                  checkIn={filter.checkIn}
                  checkOut={filter.checkOut}
                  adults={filter.adults}
                  onBook={() => {
                    const params = new URLSearchParams({
                      roomTypeId: String(room.roomTypeId),
                      checkIn: filter.checkIn,
                      checkOut: filter.checkOut,
                      adults: String(filter.adults),
                      children: String(filter.children),
                    });
                    router.push(`/book?${params}`);
                  }}
                />
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default function SearchPage() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center"><RefreshCw className="animate-spin text-[#0194f3]" size={32} /></div>}>
      <SearchPageContent />
    </Suspense>
  );
}
