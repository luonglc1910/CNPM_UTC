"use client";
import React from 'react';
import { SlidersHorizontal, X, Check } from 'lucide-react';
import type { FilterState } from '../types';

// ============================================================
// Price Histogram (Luxury visual bar chart)
// ============================================================
function PriceHistogram({ min, max, selectedMin, selectedMax }: {
  min: number; max: number; selectedMin: number; selectedMax: number;
}) {
  const bars = Array.from({ length: 24 }, (_, i) => {
    const t = i / 23;
    const height = Math.round(15 + 85 * Math.exp(-Math.pow(t - 0.45, 2) / 0.08));
    const barPrice = min + t * (max - min);
    const isSelected = barPrice >= selectedMin && barPrice <= selectedMax;
    return { height, isSelected };
  });

  return (
    <div className="flex items-end gap-0.5 h-12 mt-4 mb-2 px-1">
      {bars.map((bar, i) => (
        <div
          key={i}
          style={{ height: `${bar.height}%` }}
          className={`flex-1 rounded-t-sm transition-colors duration-300 ${bar.isSelected ? 'bg-luxury-navy' : 'bg-zinc-200'}`}
        />
      ))}
    </div>
  );
}

// ============================================================
// Dual Range Price Slider (Custom CSS via style to fix layout)
// ============================================================
function DualRangeSlider({ min, max, low, high, onChange }: {
  min: number; max: number; low: number; high: number;
  onChange: (low: number, high: number) => void;
}) {
  const pct = (v: number) => ((v - min) / (max - min)) * 100;

  function handleLow(e: React.ChangeEvent<HTMLInputElement>) {
    const v = Math.min(Number(e.target.value), high - 100000);
    onChange(v, high);
  }
  function handleHigh(e: React.ChangeEvent<HTMLInputElement>) {
    const v = Math.max(Number(e.target.value), low + 100000);
    onChange(low, v);
  }

  return (
    <div className="relative h-6 flex items-center group w-full">
      {/* Track Background */}
      <div className="absolute w-full h-1 bg-zinc-200 rounded-full" />
      {/* Selected Range */}
      <div
        className="absolute h-1 bg-luxury-navy rounded-full transition-all duration-75"
        style={{ left: `${pct(low)}%`, right: `${100 - pct(high)}%` }}
      />
      {/* Low Thumb */}
      <input
        type="range" min={min} max={max} step={50000}
        value={low} onChange={handleLow}
        className="absolute w-full h-full appearance-none bg-transparent pointer-events-none [&::-webkit-slider-thumb]:pointer-events-auto [&::-webkit-slider-thumb]:w-5 [&::-webkit-slider-thumb]:h-5 [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-white [&::-webkit-slider-thumb]:border-[3px] [&::-webkit-slider-thumb]:border-luxury-navy [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:shadow-md [&::-webkit-slider-thumb]:cursor-grab active:[&::-webkit-slider-thumb]:cursor-grabbing [&::-webkit-slider-thumb]:transition-transform hover:[&::-webkit-slider-thumb]:scale-110"
      />
      {/* High Thumb */}
      <input
        type="range" min={min} max={max} step={50000}
        value={high} onChange={handleHigh}
        className="absolute w-full h-full appearance-none bg-transparent pointer-events-none [&::-webkit-slider-thumb]:pointer-events-auto [&::-webkit-slider-thumb]:w-5 [&::-webkit-slider-thumb]:h-5 [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-white [&::-webkit-slider-thumb]:border-[3px] [&::-webkit-slider-thumb]:border-luxury-navy [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:shadow-md [&::-webkit-slider-thumb]:cursor-grab active:[&::-webkit-slider-thumb]:cursor-grabbing [&::-webkit-slider-thumb]:transition-transform hover:[&::-webkit-slider-thumb]:scale-110"
      />
    </div>
  );
}

// ============================================================
// FilterSidebar Props & Component
// ============================================================
const AMENITY_OPTIONS = [
  { key: 'wifi', label: 'WiFi tốc độ cao' },
  { key: 'breakfast', label: 'Bao gồm ăn sáng' },
  { key: 'pool', label: 'Hồ bơi vô cực' },
  { key: 'gym', label: 'Phòng Gym' },
  { key: 'bathtub', label: 'Bồn tắm' },
  { key: 'balcony', label: 'Ban công hướng view' },
  { key: 'parking', label: 'Bãi đỗ xe' },
  { key: 'spa', label: 'Spa & Wellness' },
];

const RENTAL_OPTIONS = [
  { value: 'all', label: 'Tất cả' },
  { value: 'daily', label: 'Theo ngày' },
  { value: 'hourly', label: 'Theo giờ' },
  { value: 'overnight', label: 'Qua đêm' },
];

const MIN_PRICE = 200000;
const MAX_PRICE = 10000000;

interface FilterSidebarProps {
  filter: FilterState;
  onFilterChange: (f: Partial<FilterState>) => void;
  resultCount: number;
  isMobileOpen: boolean;
  onMobileClose: () => void;
  minPrice?: number;
  maxPrice?: number;
}

export default function FilterSidebar({
  filter, onFilterChange, resultCount, isMobileOpen, onMobileClose,
}: FilterSidebarProps) {
  const activeFilterCount = [
    filter.minPrice > MIN_PRICE || filter.maxPrice < MAX_PRICE,
    filter.amenities.length > 0,
    filter.rentalType !== 'all',
  ].filter(Boolean).length;

  function toggleAmenity(key: string) {
    const current = filter.amenities;
    onFilterChange({
      amenities: current.includes(key)
        ? current.filter(k => k !== key)
        : [...current, key],
    });
  }

  function resetAll() {
    onFilterChange({
      minPrice: MIN_PRICE,
      maxPrice: MAX_PRICE,
      amenities: [],
      rentalType: 'all',
    });
  }

  const formatPrice = (val: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0
    }).format(val);
  };

  const content = (
    <div className="flex flex-col gap-8 w-full max-w-full overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <SlidersHorizontal size={18} className="text-zinc-900" />
          <h2 className="font-bold text-zinc-900 text-[16px]">Bộ lọc tìm kiếm</h2>
          {activeFilterCount > 0 && (
            <span className="bg-luxury-navy text-white text-[11px] font-bold w-5 h-5 rounded-full flex items-center justify-center shadow-sm">
              {activeFilterCount}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {activeFilterCount > 0 && (
            <button onClick={resetAll} className="text-[13px] text-zinc-500 font-medium hover:text-zinc-900 transition-colors">
              Xóa lọc
            </button>
          )}
          {/* Mobile close button */}
          <button onClick={onMobileClose} className="md:hidden p-1.5 rounded-full hover:bg-zinc-100 transition-colors">
            <X size={18} className="text-zinc-500" />
          </button>
        </div>
      </div>

      <div className="bg-zinc-50 border border-zinc-200/60 rounded-xl px-4 py-3 -mt-3">
        <p className="text-[13px] text-zinc-600 font-medium">
          Tìm thấy <strong className="text-zinc-900">{resultCount}</strong> lựa chọn phù hợp
        </p>
      </div>

      {/* ── Khoảng giá ── */}
      <div className="w-full">
        <h3 className="font-bold text-zinc-900 text-[14.5px] mb-1">Mức giá mỗi đêm</h3>
        <p className="text-[12px] text-zinc-500">Bao gồm thuế và phí</p>
        
        <div className="w-full max-w-full relative px-1">
          <PriceHistogram
            min={MIN_PRICE} max={MAX_PRICE}
            selectedMin={filter.minPrice} selectedMax={filter.maxPrice}
          />
          <DualRangeSlider
            min={MIN_PRICE} max={MAX_PRICE}
            low={filter.minPrice} high={filter.maxPrice}
            onChange={(low, high) => onFilterChange({ minPrice: low, maxPrice: high })}
          />
        </div>
        
        <div className="flex justify-between items-center text-[13px] text-zinc-900 mt-5 gap-4">
          <div className="flex-1 bg-white border border-zinc-200 rounded-xl px-3 py-2 shadow-sm text-center font-medium">
            {formatPrice(filter.minPrice).replace(' ₫', 'đ')}
          </div>
          <div className="text-zinc-300">-</div>
          <div className="flex-1 bg-white border border-zinc-200 rounded-xl px-3 py-2 shadow-sm text-center font-medium">
            {formatPrice(filter.maxPrice).replace(' ₫', 'đ')}
          </div>
        </div>
      </div>

      {/* ── Hình thức thuê ── */}
      <div className="border-t border-zinc-100 pt-6">
        <h3 className="font-bold text-zinc-900 text-[14.5px] mb-4">Hình thức đặt</h3>
        <div className="flex flex-wrap gap-2">
          {RENTAL_OPTIONS.map(opt => {
            const isActive = filter.rentalType === opt.value;
            return (
              <button
                key={opt.value}
                onClick={() => onFilterChange({ rentalType: opt.value as FilterState['rentalType'] })}
                className={`px-4 py-2 rounded-full text-[13px] font-medium transition-all duration-300 border ${
                  isActive
                    ? 'bg-luxury-navy border-luxury-navy text-white shadow-md'
                    : 'bg-white border-zinc-200 text-zinc-600 hover:border-zinc-300 hover:bg-zinc-50'
                }`}
              >
                {opt.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* ── Tiện nghi ── */}
      <div className="border-t border-zinc-100 pt-6">
        <h3 className="font-bold text-zinc-900 text-[14.5px] mb-4">Tiện ích ưa thích</h3>
        <div className="flex flex-col gap-3">
          {AMENITY_OPTIONS.map(opt => {
            const isActive = filter.amenities.includes(opt.key);
            return (
              <label key={opt.key} className="flex items-center gap-3.5 cursor-pointer group w-full">
                <div
                  className={`w-5 h-5 rounded-[6px] flex items-center justify-center border-[1.5px] transition-all duration-200 shrink-0 ${
                    isActive
                      ? 'bg-luxury-navy border-luxury-navy'
                      : 'bg-white border-zinc-300 group-hover:border-luxury-navy'
                  }`}
                >
                  {isActive && <Check size={13} className="text-white" strokeWidth={3} />}
                </div>
                <span className={`text-[14px] transition-colors ${isActive ? 'text-zinc-900 font-medium' : 'text-zinc-600 group-hover:text-zinc-900'}`}>
                  {opt.label}
                </span>
              </label>
            );
          })}
        </div>
      </div>
    </div>
  );

  return (
    <>
      {/* Desktop sidebar */}
      <aside className="hidden md:block w-72 shrink-0">
        <div className="bg-white rounded-3xl border border-zinc-200/70 shadow-sm p-6 sticky top-28 overflow-hidden">
          {content}
        </div>
      </aside>

      {/* Mobile bottom sheet */}
      <div className={`md:hidden fixed inset-0 z-50 transition-all duration-300 ${isMobileOpen ? 'visible' : 'invisible'}`}>
        <div
          className={`absolute inset-0 bg-black/50 backdrop-blur-sm transition-opacity duration-300 ${isMobileOpen ? 'opacity-100' : 'opacity-0'}`}
          onClick={onMobileClose}
        />
        <div
          className={`absolute bottom-0 left-0 right-0 bg-white rounded-t-3xl p-6 transition-transform duration-300 ease-out shadow-[0_-10px_40px_rgba(0,0,0,0.1)] max-h-[85vh] overflow-y-auto ${
            isMobileOpen ? 'translate-y-0' : 'translate-y-full'
          }`}
        >
          <div className="w-12 h-1.5 bg-zinc-200 rounded-full mx-auto mb-6" />
          {content}
          
          <div className="sticky bottom-0 bg-gradient-to-t from-white via-white to-transparent pt-4 mt-6">
            <button
              onClick={onMobileClose}
              className="w-full bg-luxury-navy text-white font-bold py-3.5 rounded-xl shadow-luxury hover:bg-black transition-colors"
            >
              Hiển thị kết quả
            </button>
          </div>
        </div>
      </div>
    </>
  );
}
