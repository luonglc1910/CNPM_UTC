"use client";
import React, { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Phone, CheckCircle, RefreshCw, ArrowLeft, Hotel, Ticket, ArrowRight } from "lucide-react";
import { lookupReservation } from "@/features/hotel-booking/api/booking-api";
import { formatVND } from "@/features/hotel-booking/utils";

type Tab = "lookup" | "found";

export default function LoginPage() {
  const router = useRouter();
  
  const [tab, setTab] = useState<Tab>("lookup");
  const [code, setCode] = useState("");
  const [phone, setPhone] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reservationData, setReservationData] = useState<{
    code: string;
    guestName: string;
    status: string;
    checkIn: string;
    checkOut: string;
    nights: number;
    estimatedTotal: number;
    rooms: { roomNumber: string; roomTypeName: string; adults: number; children: number; pricePerNight: number }[];
  } | null>(null);

  async function handleLookup(e: React.FormEvent) {
    e.preventDefault();
    if (!code.trim() || !phone.trim()) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await lookupReservation(code.trim().toUpperCase(), phone.trim());
      setReservationData(data);
      setTab("found");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Không tìm thấy đơn đặt phòng.");
    } finally {
      setIsLoading(false);
    }
  }

  const statusMap: Record<string, { label: string; color: string }> = {
    Draft: { label: "Chờ xác nhận", color: "text-amber-700 bg-amber-50 border-amber-200" },
    Confirmed: { label: "Đã xác nhận", color: "text-emerald-700 bg-emerald-50 border-emerald-200" },
    CheckedIn: { label: "Đang lưu trú", color: "text-blue-700 bg-blue-50 border-blue-200" },
    CheckedOut: { label: "Đã trả phòng", color: "text-zinc-600 bg-zinc-50 border-zinc-200" },
    Cancelled: { label: "Đã hủy", color: "text-rose-600 bg-rose-50 border-rose-200" },
    NoShow: { label: "Không đến", color: "text-rose-600 bg-rose-50 border-rose-200" },
  };

  return (
    <div className="min-h-screen flex bg-white">
      {/* Left side: Image (Hidden on mobile) */}
      <div className="hidden lg:block lg:w-[45%] relative">
        <img 
          src="/images/experience.jpg" 
          alt="Lumina Resort" 
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-luxury-navy/90 via-luxury-navy/30 to-transparent" />
        
        {/* Brand Overlay */}
        <div className="absolute inset-0 flex flex-col justify-between p-12">
          <Link href="/" className="inline-flex items-center gap-2.5 cursor-pointer">
            <div className="w-10 h-10 bg-white/20 backdrop-blur-md rounded-xl flex items-center justify-center border border-white/30">
              <Hotel size={20} className="text-white" />
            </div>
            <span className="text-2xl font-bold text-white tracking-tight">
              lumina.
            </span>
          </Link>

          <div>
            <span className="inline-block px-3 py-1 bg-white/20 backdrop-blur-md rounded-full text-white text-[12px] font-bold uppercase tracking-wider mb-4 border border-white/30">
              Guest Portal
            </span>
            <h2 className="text-4xl font-bold text-white leading-tight mb-4">
              Dịch vụ quản gia <br/> kỹ thuật số của bạn.
            </h2>
            <p className="text-zinc-200 text-lg max-w-md font-light">
              Tra cứu nhanh chóng lịch trình, yêu cầu thêm tiện ích và quản lý đặt phòng ngay từ điện thoại của bạn.
            </p>
          </div>
        </div>
      </div>

      {/* Right side: Form */}
      <div className="w-full lg:w-[55%] flex flex-col justify-center px-6 py-12 lg:px-24 relative overflow-y-auto">
        <div className="max-w-[440px] w-full mx-auto">
          
          {/* Mobile Logo */}
          <Link href="/" className="lg:hidden inline-flex items-center gap-2.5 mb-10 cursor-pointer">
            <div className="w-10 h-10 bg-luxury-navy rounded-xl flex items-center justify-center">
              <Hotel size={20} className="text-white" />
            </div>
            <span className="text-2xl font-bold text-luxury-navy tracking-tight">
              lumina.
            </span>
          </Link>

          {tab === "lookup" ? (
            <>
              <div className="mb-10">
                <h1 className="text-3xl font-bold text-zinc-900 leading-tight mb-3 tracking-tight">Tra cứu đặt phòng</h1>
                <p className="text-[15px] text-zinc-500">
                  Nhập mã số vé hoặc mã đặt phòng cùng số điện thoại mà bạn đã sử dụng.
                </p>
              </div>

              <form onSubmit={handleLookup} className="space-y-6">
                <div>
                  <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">
                    Mã đặt phòng
                  </label>
                  <div className="relative group">
                    <Ticket size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400 group-focus-within:text-brand-primary transition-colors" />
                    <input
                      required value={code} onChange={e => setCode(e.target.value.toUpperCase())}
                      placeholder="VD: RSV-260928-0001"
                      className="w-full pl-12 pr-4 py-4 text-[15px] bg-zinc-50 border border-zinc-200 rounded-xl focus:border-brand-primary focus:ring-1 focus:ring-brand-primary outline-none font-mono tracking-wider uppercase transition-all hover:bg-zinc-100/50"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">
                    Số điện thoại đặt phòng
                  </label>
                  <div className="relative group">
                    <Phone size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400 group-focus-within:text-brand-primary transition-colors" />
                    <input
                      required value={phone} onChange={e => setPhone(e.target.value)}
                      placeholder="VD: 0912345678" type="tel"
                      className="w-full pl-12 pr-4 py-4 text-[15px] bg-zinc-50 border border-zinc-200 rounded-xl focus:border-brand-primary focus:ring-1 focus:ring-brand-primary outline-none transition-all hover:bg-zinc-100/50"
                    />
                  </div>
                </div>

                {error && (
                  <div className="bg-rose-50 border border-rose-200 rounded-xl p-4 text-[13.5px] text-rose-600 flex items-center gap-2">
                    <span className="shrink-0 font-bold">⚠️</span>
                    <span>{error}</span>
                  </div>
                )}

                <button
                  type="submit" disabled={isLoading}
                  className="w-full bg-luxury-navy hover:bg-black active:scale-[0.98] disabled:bg-zinc-300 disabled:active:scale-100 text-white font-bold py-4 rounded-xl transition-all shadow-luxury flex items-center justify-center gap-2 text-[15px] mt-4 group"
                >
                  {isLoading ? <><RefreshCw size={18} className="animate-spin" /> Đang truy vấn...</> : <>Tiến hành kiểm tra <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" /></>}
                </button>
              </form>

              {/* Tip */}
              <div className="mt-10 bg-zinc-50 border border-zinc-100 rounded-xl p-4 flex gap-4 items-start">
                <div className="w-10 h-10 bg-white rounded-full flex items-center justify-center shrink-0 shadow-sm">
                  <span className="text-[18px]">💡</span>
                </div>
                <p className="text-[13px] text-zinc-500 leading-relaxed pt-1">
                  Mã đặt phòng được gửi qua SMS/Email sau khi đặt thành công, bắt đầu bằng tiền tố <strong>RSV-</strong>.
                </p>
              </div>

              <div className="mt-10 text-center">
                <p className="text-[14.5px] text-zinc-500">
                  Chưa có lịch trình nào?{" "}
                  <Link href="/search" className="text-brand-primary font-bold hover:underline">
                    Đặt phòng ngay
                  </Link>
                </p>
              </div>
            </>
          ) : (
            /* Found result */
            <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
              <button onClick={() => { setTab("lookup"); setReservationData(null); }}
                className="flex items-center gap-1.5 text-[14px] font-medium text-zinc-500 hover:text-zinc-900 mb-8 transition-colors group">
                <ArrowLeft size={18} className="group-hover:-translate-x-1 transition-transform" /> 
                Tra cứu mã khác
              </button>

              <div className="flex items-center gap-5 mb-8">
                <div className="w-16 h-16 bg-emerald-50 border border-emerald-100 rounded-2xl flex items-center justify-center shrink-0 shadow-sm">
                  <CheckCircle size={32} className="text-emerald-500" />
                </div>
                <div>
                  <p className="font-bold text-zinc-900 text-[22px] tracking-tight">Tìm thấy hồ sơ!</p>
                  <p className="text-[15px] text-zinc-500">Xin chào, {reservationData?.guestName}</p>
                </div>
              </div>

              {reservationData && (
                <div className="space-y-6">
                  <div className="bg-white border border-zinc-200 rounded-3xl p-6 md:p-8 space-y-4 shadow-xl">
                    <div className="flex items-center justify-between pb-4 border-b border-zinc-100">
                      <span className="text-[14px] text-zinc-500 font-medium">Mã số vé</span>
                      <span className="font-bold text-luxury-navy font-mono text-[15px] bg-zinc-50 px-3 py-1 rounded-lg border border-zinc-200">{reservationData.code}</span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-[14px] text-zinc-500">Trạng thái</span>
                      <span className={`text-[12px] font-bold px-3 py-1 rounded-full border ${statusMap[reservationData.status]?.color ?? "text-zinc-600 bg-zinc-50 border-zinc-200"}`}>
                        {statusMap[reservationData.status]?.label ?? reservationData.status}
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-[14px] text-zinc-500">Ngày nhận</span>
                      <span className="font-semibold text-zinc-900 text-[15px]">
                        {new Date(reservationData.checkIn).toLocaleDateString("vi-VN", { weekday: "short", day: "2-digit", month: "short", year: "numeric" })}
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-[14px] text-zinc-500">Ngày trả</span>
                      <span className="font-semibold text-zinc-900 text-[15px]">
                        {new Date(reservationData.checkOut).toLocaleDateString("vi-VN", { weekday: "short", day: "2-digit", month: "short", year: "numeric" })}
                      </span>
                    </div>
                    
                    <div className="pt-2">
                      {reservationData.rooms.map((r, i) => (
                        <div key={i} className="flex items-center justify-between py-2 border-t border-zinc-100/80 border-dashed">
                          <span className="text-[14px] text-zinc-500">Phòng {r.roomNumber}</span>
                          <span className="font-semibold text-zinc-900 text-[14px]">{r.roomTypeName}</span>
                        </div>
                      ))}
                    </div>
                    
                    <div className="flex items-center justify-between pt-5 border-t border-zinc-200 mt-2">
                      <span className="text-[14px] font-bold text-zinc-900 uppercase">Tổng cộng</span>
                      <span className="font-bold text-luxury-amber text-[22px] tracking-tight">{formatVND(reservationData.estimatedTotal)} đ</span>
                    </div>
                  </div>

                  <p className="text-[13.5px] text-zinc-500 text-center py-2">
                    Lễ tân trực 24/7: <strong className="text-zinc-900">+84 24 0123 4567</strong>
                  </p>

                  <button onClick={() => router.push("/search")}
                    className="w-full bg-white border-2 border-luxury-navy text-luxury-navy font-bold py-4 rounded-xl hover:bg-zinc-50 active:scale-[0.98] transition-all text-[15px]">
                    Đặt thêm phòng mới
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
