"use client";
import React, { useState } from 'react';
import { Star, Users, Wifi, Tv, Wine, Waves, Dumbbell, Coffee, Wind, Bath, Building2, Lock, Car, Sparkles, ChevronLeft, ChevronRight, Info, MapPin } from 'lucide-react';
import type { RoomCardViewModel, AmenityItem, RoomBadge } from '../types';
import { formatVND } from '../utils/data-mappers';

// ============================================================
// Icon Renderer
// ============================================================
const ICON_MAP: Record<string, React.ReactNode> = {
  Wifi: <Wifi size={14} />,
  Tv: <Tv size={14} />,
  Wine: <Wine size={14} />,
  Waves: <Waves size={14} />,
  Dumbbell: <Dumbbell size={14} />,
  Coffee: <Coffee size={14} />,
  Wind: <Wind size={14} />,
  Bath: <Bath size={14} />,
  Building2: <Building2 size={14} />,
  Lock: <Lock size={14} />,
  Car: <Car size={14} />,
  Sparkles: <Sparkles size={14} />,
};

// ============================================================
// Badge Chip Component (Luxury Style)
// ============================================================
const BADGE_STYLES: Record<RoomBadge['variant'], string> = {
  success: 'bg-emerald-500/10 text-emerald-700 border-emerald-500/20 backdrop-blur-md',
  info: 'bg-blue-500/10 text-blue-700 border-blue-500/20 backdrop-blur-md',
  warning: 'bg-amber-500/10 text-amber-700 border-amber-500/20 backdrop-blur-md',
  destructive: 'bg-rose-500 text-white font-semibold shadow-sm',
};

function BadgeChip({ badge }: { badge: RoomBadge }) {
  return (
    <span className={`inline-flex items-center gap-1 text-[11px] font-medium px-2.5 py-1 rounded-full border ${BADGE_STYLES[badge.variant]}`}>
      {badge.label}
    </span>
  );
}

// ============================================================
// Amenity Chip
// ============================================================
function AmenityChip({ amenity }: { amenity: AmenityItem }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-[12px] text-zinc-600 bg-zinc-100/70 border border-zinc-200/50 rounded-full px-3 py-1.5 transition-colors hover:bg-zinc-200/50">
      <span className="text-zinc-500">{ICON_MAP[amenity.icon] ?? null}</span>
      {amenity.label}
    </span>
  );
}

// ============================================================
// Image Carousel (Aspect Ratio 4:3)
// ============================================================
function ImageCarousel({ images, name }: { images: string[]; name: string }) {
  const [current, setCurrent] = useState(0);

  function prev(e: React.MouseEvent) {
    e.stopPropagation();
    setCurrent(c => (c === 0 ? images.length - 1 : c - 1));
  }

  function next(e: React.MouseEvent) {
    e.stopPropagation();
    setCurrent(c => (c === images.length - 1 ? 0 : c + 1));
  }

  return (
    <div className="relative w-full aspect-4/3 md:aspect-auto md:h-full overflow-hidden group/carousel">
      {images.map((src, i) => (
        <img
          key={i}
          src={src}
          alt={`${name} - ảnh ${i + 1}`}
          className={`absolute inset-0 w-full h-full object-cover transition-opacity duration-500 ${i === current ? 'opacity-100' : 'opacity-0'}`}
        />
      ))}

      {/* Prev/Next Ghost Buttons */}
      {images.length > 1 && (
        <>
          <button
            onClick={prev}
            className="absolute left-3 top-1/2 -translate-y-1/2 w-8 h-8 rounded-full bg-white/30 backdrop-blur-md flex items-center justify-center text-white opacity-0 group-hover/carousel:opacity-100 transition-all hover:bg-white hover:text-zinc-900 hover:scale-105"
            aria-label="Ảnh trước"
          >
            <ChevronLeft size={18} />
          </button>
          <button
            onClick={next}
            className="absolute right-3 top-1/2 -translate-y-1/2 w-8 h-8 rounded-full bg-white/30 backdrop-blur-md flex items-center justify-center text-white opacity-0 group-hover/carousel:opacity-100 transition-all hover:bg-white hover:text-zinc-900 hover:scale-105"
            aria-label="Ảnh tiếp"
          >
            <ChevronRight size={18} />
          </button>
          {/* Pagination dots */}
          <div className="absolute bottom-3 left-1/2 -translate-x-1/2 flex gap-1.5 z-10">
            {images.map((_, i) => (
              <button
                key={i}
                onClick={e => { e.stopPropagation(); setCurrent(i); }}
                className={`h-1.5 rounded-full transition-all duration-300 ${i === current ? 'bg-white w-4 shadow-sm' : 'bg-white/50 w-1.5 hover:bg-white/80'}`}
              />
            ))}
          </div>
        </>
      )}

      {/* Top Floating Badge */}
      <div className="absolute top-3 left-3 z-10">
        <span className="bg-black/40 backdrop-blur-md border border-white/20 text-white text-[11px] font-medium tracking-wide px-2.5 py-1 rounded-lg">
          Lumina Collection
        </span>
      </div>
      
      {/* Gradient overlay for bottom dots visibility */}
      <div className="absolute inset-x-0 bottom-0 h-16 bg-linear-to-t from-black/40 to-transparent pointer-events-none" />
    </div>
  );
}

// ============================================================
// Pricing Breakdown Tooltip
// ============================================================
function PriceTooltip({ vm }: { vm: RoomCardViewModel }) {
  const [open, setOpen] = useState(false);
  return (
    <div className="relative inline-block ml-1">
      <button
        onMouseEnter={() => setOpen(true)}
        onMouseLeave={() => setOpen(false)}
        onClick={(e) => { e.stopPropagation(); setOpen(v => !v); }}
        className="text-zinc-400 hover:text-zinc-700 transition-colors"
        aria-label="Chi tiết giá"
      >
        <Info size={14} />
      </button>
      {open && (
        <div className="absolute bottom-full right-0 mb-2 w-64 bg-white/95 backdrop-blur-xl rounded-2xl shadow-luxury border border-zinc-200/70 p-4 text-[13px] z-50">
          <p className="font-bold text-zinc-900 mb-3">Chi tiết giá</p>
          <div className="space-y-2 text-zinc-600">
            <div className="flex justify-between">
              <span>Giá theo đêm:</span>
              <span className="font-medium text-zinc-900">{formatVND(vm.pricePerNight)} đ</span>
            </div>
            <div className="flex justify-between">
              <span>Giờ đầu tiên:</span>
              <span className="font-medium text-zinc-900">{formatVND(vm.priceFirstHour)} đ</span>
            </div>
            <div className="flex justify-between">
              <span>Mỗi giờ thêm:</span>
              <span className="font-medium text-zinc-900">{formatVND(vm.priceExtraHour)} đ/h</span>
            </div>
            <div className="flex justify-between">
              <span>Gói qua đêm:</span>
              <span className="font-medium text-zinc-900">{formatVND(vm.priceOvernight)} đ</span>
            </div>
            {vm.extraGuestFee > 0 && (
              <div className="flex justify-between border-t border-zinc-100 pt-2 mt-1">
                <span>Phụ thu thêm người:</span>
                <span className="font-medium text-zinc-900">{formatVND(vm.extraGuestFee)} đ</span>
              </div>
            )}
          </div>
          <p className="text-[11px] text-zinc-400 mt-3 pt-3 border-t border-zinc-100">
            * Giá chưa bao gồm thuế & phí dịch vụ
          </p>
        </div>
      )}
    </div>
  );
}

// ============================================================
// Skeleton Loader (Shimmer effect)
// ============================================================
export function RoomCardSkeleton() {
  return (
    <div className="bg-white rounded-2xl border border-zinc-200/70 overflow-hidden flex flex-col md:flex-row shadow-sm">
      <div className="w-full aspect-4/3 md:aspect-auto md:w-[320px] bg-zinc-100 shrink-0 relative overflow-hidden">
        <div className="absolute inset-0 -translate-x-full animate-[shimmer_1.5s_infinite] bg-linear-to-r from-transparent via-white/40 to-transparent" />
      </div>
      <div className="flex-1 p-6 flex flex-col gap-4">
        <div className="flex gap-2">
          <div className="h-6 w-24 bg-zinc-100 rounded-full animate-pulse" />
          <div className="h-6 w-28 bg-zinc-100 rounded-full animate-pulse" />
        </div>
        <div className="h-7 w-3/4 bg-zinc-100 rounded-lg animate-pulse" />
        <div className="h-4 w-full bg-zinc-50 rounded animate-pulse" />
        <div className="h-4 w-2/3 bg-zinc-50 rounded animate-pulse" />
        
        <div className="flex gap-2 flex-wrap mt-2">
          {[1, 2, 3].map(i => <div key={i} className="h-8 w-28 bg-zinc-100 rounded-full animate-pulse" />)}
        </div>
        
        <div className="mt-auto flex items-end justify-between pt-4 border-t border-zinc-100">
          <div className="space-y-2">
            <div className="h-4 w-24 bg-zinc-100 rounded animate-pulse" />
            <div className="h-3 w-32 bg-zinc-50 rounded animate-pulse" />
          </div>
          <div className="space-y-2 flex flex-col items-end">
            <div className="h-4 w-20 bg-zinc-100 rounded animate-pulse" />
            <div className="h-8 w-32 bg-zinc-200 rounded-xl animate-pulse" />
          </div>
        </div>
      </div>
    </div>
  );
}

function getRatingText(rating: number) {
  if (rating >= 9.0) return "Tuyệt hảo";
  if (rating >= 8.0) return "Tuyệt vời";
  if (rating >= 7.0) return "Rất tốt";
  return "Tốt";
}

// ============================================================
// Main RoomCard Component
// ============================================================
export default function RoomCard({ vm, checkIn, checkOut, adults, onBook }: {
  vm: RoomCardViewModel;
  checkIn: string;
  checkOut: string;
  adults: number;
  onBook?: () => void;
}) {
  const nights = Math.max(1, Math.ceil(
    (new Date(checkOut).getTime() - new Date(checkIn).getTime()) / (1000 * 60 * 60 * 24)
  ));
  const totalPrice = vm.pricePerNight * nights;

  // Extra guest fee
  const extraGuests = Math.max(0, adults - vm.standardCapacity);
  const extraFee = extraGuests * vm.extraGuestFee * nights;
  const finalPrice = totalPrice + extraFee;
  
  // Fake original price for visual presentation (25% higher)
  const originalPrice = Math.round(finalPrice * 1.25);

  return (
    <div className="bg-white rounded-3xl border border-zinc-200/70 shadow-sm hover:shadow-luxury transition-all duration-300 overflow-hidden flex flex-col md:flex-row group cursor-pointer hover:-translate-y-1">
      {/* Image Carousel */}
      <div className="w-full md:w-[320px] shrink-0 p-3 md:pr-0">
        <div className="w-full h-full rounded-2xl overflow-hidden">
          <ImageCarousel images={vm.images} name={vm.name} />
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-5 md:p-6 flex flex-col">
        {/* Header: Badges & Rating */}
        <div className="flex items-start justify-between gap-4 mb-3">
          <div className="flex flex-wrap gap-2">
            {vm.badges.map((b, i) => <BadgeChip key={i} badge={b} />)}
          </div>
          
          {vm.rating !== null ? (
            <div className="flex items-center gap-2 text-right shrink-0">
              <div className="hidden sm:flex flex-col items-end">
                <span className="text-[13px] font-bold text-zinc-900">{getRatingText(vm.rating)}</span>
                <span className="text-[11px] text-zinc-500">{vm.reviewCount ?? 124} đánh giá</span>
              </div>
              <div className="w-10 h-10 rounded-xl bg-luxury-navy text-white flex items-center justify-center font-bold text-[15px] shadow-sm">
                {vm.rating.toFixed(1)}
              </div>
            </div>
          ) : null}
        </div>

        {/* Title */}
        <h3 className="text-[20px] font-bold text-zinc-900 leading-tight mb-1">{vm.name}</h3>
        
        {/* Location/Map link */}
        <div className="flex items-center gap-1 text-[12.5px] text-zinc-500 mb-3">
          <MapPin size={14} className="text-brand-primary" />
          <span>Quận Hoàn Kiếm, Hà Nội</span>
          <span className="mx-1.5">•</span>
          <span className="text-brand-primary font-medium hover:underline cursor-pointer">Xem trên bản đồ</span>
        </div>

        {/* Description */}
        <p className="text-[13.5px] text-zinc-600 line-clamp-2 leading-relaxed mb-4">
          {vm.description}
        </p>

        {/* Amenities */}
        <div className="flex flex-wrap gap-2 mb-4">
          <span className="inline-flex items-center gap-1.5 text-[12px] text-zinc-600 bg-zinc-50 border border-zinc-200/50 rounded-full px-3 py-1.5">
            <Users size={14} className="text-zinc-500" />
            Tối đa {vm.maxCapacity} khách
          </span>
          {vm.amenities.slice(0, 4).map(a => <AmenityChip key={a.key} amenity={a} />)}
          {vm.amenities.length > 4 && (
            <span className="text-[12px] text-zinc-500 font-medium flex items-center px-2">
              +{vm.amenities.length - 4} tiện ích
            </span>
          )}
        </div>

        {/* Divider */}
        <div className="flex-1" />
        
        {/* Footer: Availability & Price */}
        <div className="mt-2 pt-4 border-t border-zinc-100 flex flex-col sm:flex-row items-start sm:items-end justify-between gap-4">
          {/* Availability info */}
          <div className="text-[13px] text-zinc-600">
            {vm.availableRooms <= 3 ? (
              <span className="inline-flex items-center gap-1.5 text-rose-600 font-semibold bg-rose-50 px-2.5 py-1 rounded-md">
                <span className="relative flex h-2 w-2">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-2 w-2 bg-rose-500"></span>
                </span>
                Chỉ còn {vm.availableRooms} phòng trống!
              </span>
            ) : (
              <span className="text-emerald-700 font-medium bg-emerald-50 px-2.5 py-1 rounded-md">
                Phòng trống sẵn sàng
              </span>
            )}
          </div>

          {/* Pricing & CTA */}
          <div className="text-right shrink-0 w-full sm:w-auto">
            <div className="flex items-center justify-end gap-1.5 mb-1">
              <span className="text-[13px] text-zinc-500 line-through decoration-zinc-300">
                {formatVND(originalPrice)} đ
              </span>
              <span className="bg-rose-100 text-rose-700 text-[10px] font-bold px-1.5 py-0.5 rounded uppercase tracking-wider">
                -20%
              </span>
            </div>
            
            <div className="flex items-baseline justify-end gap-2 mb-1">
              <div className="text-[26px] font-bold text-zinc-900 tracking-tight">
                {formatVND(finalPrice)}<span className="text-[15px] text-zinc-500 font-normal ml-1">đ</span>
              </div>
            </div>
            
            <div className="flex items-center justify-end gap-1 text-[11px] text-zinc-500 mb-3">
              <span>Đã bao gồm thuế & phí ({nights} đêm)</span>
              <PriceTooltip vm={vm} />
            </div>

            <button
              onClick={(e) => { e.stopPropagation(); onBook?.(); }}
              className="w-full sm:w-auto bg-luxury-navy hover:bg-[#1E293B] text-white font-bold text-[14.5px] px-8 py-3 rounded-xl transition-all duration-300 shadow-[0_4px_14px_rgba(15,23,42,0.3)] hover:shadow-[0_6px_20px_rgba(15,23,42,0.4)] hover:-translate-y-0.5"
            >
              Chọn phòng
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
