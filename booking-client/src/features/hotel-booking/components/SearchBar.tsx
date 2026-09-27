"use client";
import React, { useState, useEffect, useRef } from "react";
import { Search, MapPin, Calendar, Users, ChevronDown, ChevronUp } from "lucide-react";

interface SearchBarState {
  location: string;
  checkIn: string;
  checkOut: string;
  adults: number;
  children: number;
  rooms: number;
}

interface SearchBarProps {
  initialValues?: Partial<SearchBarState>;
  onSearch: (values: SearchBarState) => void;
  sticky?: boolean; // khi true: hiện dạng compact khi cuộn
}

const today = () => {
  const d = new Date();
  return d.toISOString().split('T')[0];
};
const tomorrow = () => {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().split('T')[0];
};
const formatDate = (iso: string) => {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: 'short' });
};
const nightsBetween = (ci: string, co: string) => {
  const diff = new Date(co).getTime() - new Date(ci).getTime();
  return Math.max(1, Math.ceil(diff / (1000 * 60 * 60 * 24)));
};

export default function SearchBar({ initialValues, onSearch, sticky = true }: SearchBarProps) {
  const [location, setLocation] = useState(initialValues?.location ?? '');
  const [checkIn, setCheckIn] = useState(initialValues?.checkIn ?? today());
  const [checkOut, setCheckOut] = useState(initialValues?.checkOut ?? tomorrow());
  const [adults, setAdults] = useState(initialValues?.adults ?? 2);
  const [children, setChildren] = useState(initialValues?.children ?? 0);
  const [rooms, setRooms] = useState(initialValues?.rooms ?? 1);

  const [guestOpen, setGuestOpen] = useState(false);
  const [isScrolled, setIsScrolled] = useState(false);
  const [isExpanded, setIsExpanded] = useState(true);
  const guestRef = useRef<HTMLDivElement>(null);

  const guestSummary = `${adults} người lớn, ${children} trẻ em, ${rooms} phòng`;
  const nights = nightsBetween(checkIn, checkOut);

  // Sticky scroll detection
  useEffect(() => {
    if (!sticky) return;
    const handleScroll = () => {
      const scrolled = window.scrollY > 320;
      setIsScrolled(scrolled);
      if (scrolled) setIsExpanded(false);
      else setIsExpanded(true);
    };
    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, [sticky]);

  // Close guest dropdown on outside click
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (guestRef.current && !guestRef.current.contains(e.target as Node)) {
        setGuestOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  function handleSearch() {
    onSearch({ location, checkIn, checkOut, adults, children, rooms });
  }

  // ── Compact (sticky) mode ──
  if (sticky && isScrolled && !isExpanded) {
    return (
      <div
        className="fixed top-20 left-0 right-0 z-40 bg-white/95 backdrop-blur-md border-b border-zinc-200/70 shadow-sm py-3 px-4"
        onClick={() => setIsExpanded(true)}
      >
        <div className="max-w-7xl mx-auto flex items-center justify-between cursor-pointer">
          <div className="flex items-center gap-6 text-[14px]">
            <span className="flex items-center gap-2 font-bold text-zinc-900">
              <MapPin size={16} className="text-brand-primary" />
              {location || 'Điểm đến'}
            </span>
            <span className="text-zinc-300">|</span>
            <span className="flex items-center gap-2 text-zinc-600 font-medium">
              <Calendar size={16} />
              {formatDate(checkIn)} – {formatDate(checkOut)} <span className="text-zinc-400 font-normal">({nights} đêm)</span>
            </span>
            <span className="text-zinc-300">|</span>
            <span className="flex items-center gap-2 text-zinc-600 font-medium">
              <Users size={16} />
              {guestSummary}
            </span>
          </div>
          <button className="bg-brand-primary text-white font-bold text-[13px] px-5 py-2 rounded-full flex items-center gap-2 shadow-[0_4px_12px_rgba(1,148,243,0.3)] hover:shadow-[0_6px_16px_rgba(1,148,243,0.4)] hover:-translate-y-0.5 transition-all">
            <Search size={14} />
            Sửa tìm kiếm
          </button>
        </div>
      </div>
    );
  }

  // ── Full mode ──
  return (
    <div className={`transition-all duration-300 ${sticky && isScrolled ? 'fixed top-20 left-0 right-0 z-40 bg-white/95 backdrop-blur-md border-b border-zinc-200/70 shadow-luxury py-4 px-4' : ''}`}>
      <div className={`mx-auto ${sticky && isScrolled ? 'max-w-7xl' : ''}`}>
        
        {/* The Search Bar Container */}
        <div className={`flex flex-col md:flex-row items-center bg-white border border-zinc-200/70 rounded-full shadow-luxury md:divide-x md:divide-zinc-200 relative overflow-visible ${sticky && isScrolled ? '' : 'p-2'}`}>
          
          {/* Location */}
          <div className="w-full md:flex-2 relative group flex-1">
            <div className="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none">
              <MapPin size={20} className="text-zinc-400 group-hover:text-brand-primary transition-colors" />
            </div>
            <input
              type="text"
              value={location}
              onChange={e => setLocation(e.target.value)}
              placeholder="Bạn muốn đi đâu?"
              className="w-full pl-12 pr-4 py-3 md:py-4 bg-transparent text-zinc-900 font-semibold placeholder-zinc-400 focus:outline-none rounded-t-2xl md:rounded-l-full md:rounded-tr-none hover:bg-zinc-50 transition-colors"
            />
            <div className="absolute top-2 left-12 text-[10px] uppercase font-bold text-zinc-400 tracking-wider">Điểm đến</div>
          </div>

          {/* Dates Wrapper */}
          <div className="w-full md:flex-2 flex divide-x divide-zinc-200">
            {/* Check-in */}
            <div className="w-1/2 relative group">
              <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                <Calendar size={18} className="text-zinc-400 group-hover:text-brand-primary transition-colors" />
              </div>
              <input
                type="date"
                value={checkIn}
                min={today()}
                onChange={e => {
                  setCheckIn(e.target.value);
                  if (e.target.value >= checkOut) {
                    const d = new Date(e.target.value);
                    d.setDate(d.getDate() + 1);
                    setCheckOut(d.toISOString().split('T')[0]);
                  }
                }}
                className="w-full pl-11 pr-2 py-3 md:py-4 bg-transparent text-zinc-900 font-semibold focus:outline-none hover:bg-zinc-50 transition-colors cursor-pointer"
              />
              <div className="absolute top-2 left-11 text-[10px] uppercase font-bold text-zinc-400 tracking-wider">Nhận phòng</div>
            </div>

            {/* Check-out */}
            <div className="w-1/2 relative group">
              <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                <Calendar size={18} className="text-zinc-400 group-hover:text-brand-primary transition-colors" />
              </div>
              <input
                type="date"
                value={checkOut}
                min={checkIn}
                onChange={e => setCheckOut(e.target.value)}
                className="w-full pl-11 pr-2 py-3 md:py-4 bg-transparent text-zinc-900 font-semibold focus:outline-none hover:bg-zinc-50 transition-colors cursor-pointer"
              />
              <div className="absolute top-2 left-11 text-[10px] uppercase font-bold text-zinc-400 tracking-wider">Trả phòng ({nights} đêm)</div>
            </div>
          </div>

          {/* Guests & Rooms */}
          <div className="w-full md:flex-[1.5] relative" ref={guestRef}>
            <div
              className={`w-full px-5 py-3 md:py-4 flex items-center justify-between cursor-pointer hover:bg-zinc-50 transition-colors ${guestOpen ? 'bg-zinc-50' : ''}`}
              onClick={() => setGuestOpen(v => !v)}
            >
              <div className="flex flex-col">
                <span className="text-[10px] uppercase font-bold text-zinc-400 tracking-wider mb-0.5">Khách & Phòng</span>
                <span className="text-zinc-900 font-semibold truncate">{guestSummary}</span>
              </div>
              {guestOpen ? <ChevronUp size={18} className="text-brand-primary shrink-0" /> : <ChevronDown size={18} className="text-zinc-400 shrink-0" />}
            </div>

            {/* Popover */}
            {guestOpen && (
              <div className="absolute top-full left-0 right-0 mt-3 bg-white rounded-2xl shadow-luxury border border-zinc-100 z-50 p-5 min-w-70">
                {[
                  { label: 'Người lớn', sub: 'Từ 13 tuổi trở lên', val: adults, set: setAdults, min: 1 },
                  { label: 'Trẻ em', sub: 'Dưới 13 tuổi', val: children, set: setChildren, min: 0 },
                  { label: 'Phòng', sub: null, val: rooms, set: setRooms, min: 1 },
                ].map((row, idx) => (
                  <div key={row.label} className={`flex items-center justify-between py-4 ${idx !== 2 ? 'border-b border-zinc-100' : ''}`}>
                    <div>
                      <div className="text-[15px] font-bold text-zinc-900">{row.label}</div>
                      {row.sub && <div className="text-[13px] text-zinc-500 mt-0.5">{row.sub}</div>}
                    </div>
                    <div className="flex items-center gap-3">
                      <button
                        onClick={() => row.set(Math.max(row.min, row.val - 1))}
                        disabled={row.val <= row.min}
                        className={`w-9 h-9 rounded-full flex items-center justify-center text-lg font-medium transition-colors ${row.val <= row.min ? 'border border-zinc-200 text-zinc-300 cursor-not-allowed' : 'border border-zinc-300 text-zinc-600 hover:border-zinc-800 hover:text-zinc-900 active:scale-95'}`}
                      >−</button>
                      <span className="w-5 text-center font-semibold text-[15px] text-zinc-900">{row.val}</span>
                      <button
                        onClick={() => row.set(row.val + 1)}
                        className="w-9 h-9 rounded-full flex items-center justify-center text-lg font-medium border border-zinc-300 text-zinc-600 hover:border-zinc-800 hover:text-zinc-900 active:scale-95 transition-all"
                      >+</button>
                    </div>
                  </div>
                ))}
                <button
                  onClick={() => setGuestOpen(false)}
                  className="mt-4 w-full bg-luxury-navy text-white font-bold py-3 rounded-xl hover:bg-black transition-colors"
                >Xác nhận</button>
              </div>
            )}
          </div>

          {/* Search CTA */}
          <div className="p-2 md:pl-2 shrink-0 w-full md:w-auto">
            <button
              onClick={handleSearch}
              className="w-full md:w-auto bg-luxury-amber hover:bg-amber-700 active:scale-95 text-white px-8 py-3.5 md:py-4 rounded-xl md:rounded-full font-bold text-[15px] transition-all duration-300 hover:-translate-y-0.5 shadow-[0_4px_14px_rgba(217,119,6,0.4)] hover:shadow-[0_6px_20px_rgba(217,119,6,0.5)] flex items-center justify-center gap-2"
            >
              <Search size={18} />
              Tìm kiếm
            </button>
          </div>

        </div>
      </div>
    </div>
  );
}
