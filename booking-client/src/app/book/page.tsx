"use client";
import React, { useState, useEffect, Suspense } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { CheckCircle, AlertCircle, Calendar, Users, Building2, CreditCard, Phone, Mail, User, FileText, ArrowLeft, RefreshCw } from "lucide-react";
import { getAvailability, createReservation, type ApiAvailabilityGroup } from "@/features/hotel-booking/api/booking-api";
import { formatVND } from "@/features/hotel-booking/utils/search-helpers";

// ────────────────────────────────────────────────────────────
function BookPageContent() {
  const searchParams = useSearchParams();
  const router = useRouter();

  const roomTypeId = Number(searchParams.get("roomTypeId") ?? 0);
  const checkIn = searchParams.get("checkIn") ?? "";
  const checkOut = searchParams.get("checkOut") ?? "";
  const adults = Number(searchParams.get("adults") ?? 2);
  const children = Number(searchParams.get("children") ?? 0);

  const [room, setRoom] = useState<ApiAvailabilityGroup | null>(null);
  const [isLoadingRoom, setIsLoadingRoom] = useState(true);

  // Guest form state
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [email, setEmail] = useState("");
  const [idNumber, setIdNumber] = useState("");
  const [specialRequests, setSpecialRequests] = useState("");

  // Booking state
  const [isBooking, setIsBooking] = useState(false);
  const [bookingResult, setBookingResult] = useState<{
    reservationCode: string;
    roomNumber: string;
    roomTypeName: string;
    nights: number;
    estimatedTotal: number;
    message: string;
  } | null>(null);
  const [bookingError, setBookingError] = useState<string | null>(null);

  // Auto-fill từ sessionStorage nếu đã đăng ký
  useEffect(() => {
    try {
      const saved = sessionStorage.getItem("lumina_guest");
      if (saved) {
        const g = JSON.parse(saved);
        if (g.fullName) setFullName(g.fullName);
        if (g.phoneNumber) setPhoneNumber(g.phoneNumber);
        if (g.email) setEmail(g.email);
        if (g.idNumber) setIdNumber(g.idNumber);
      }
    } catch { /* ignore */ }
  }, []);

  // Load room details
  useEffect(() => {
    if (!roomTypeId || !checkIn || !checkOut) {
      setIsLoadingRoom(false);
      return;
    }
    getAvailability(checkIn, checkOut, adults)
      .then(data => {
        const group = data.groups.find(g => g.roomTypeId === roomTypeId);
        setRoom(group ?? null);
      })
      .catch(() => setRoom(null))
      .finally(() => setIsLoadingRoom(false));
  }, [roomTypeId, checkIn, checkOut, adults]);

  const nights = room?.nights ?? 1;
  const totalPrice = room ? room.pricePerNight * nights : 0;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!room) return;

    setIsBooking(true);
    setBookingError(null);

    try {
      const result = await createReservation({
        checkIn,
        checkOut,
        roomTypeId,
        adults,
        children,
        specialRequests: specialRequests.trim() || undefined,
        guest: {
          fullName: fullName.trim(),
          phoneNumber: phoneNumber.trim(),
          email: email.trim() || undefined,
          idNumber: idNumber.trim() || undefined,
        },
      });
      setBookingResult({
        reservationCode: result.reservationCode,
        roomNumber: result.roomNumber,
        roomTypeName: result.roomTypeName,
        nights: result.nights,
        estimatedTotal: result.estimatedTotal,
        message: result.message,
      });
    } catch (err: unknown) {
      setBookingError(err instanceof Error ? err.message : "Đã xảy ra lỗi. Vui lòng thử lại.");
    } finally {
      setIsBooking(false);
    }
  }

  // ── Success Screen ──
  if (bookingResult) {
    return (
      <div className="min-h-screen bg-[#f7f9fa] flex items-center justify-center px-4 py-12">
        <div className="max-w-lg w-full bg-white rounded-3xl shadow-[0_8px_32px_rgba(3,18,26,0.1)] p-8 text-center">
          <div className="w-20 h-20 bg-emerald-50 rounded-full flex items-center justify-center mx-auto mb-6">
            <CheckCircle size={40} className="text-emerald-500" />
          </div>
          <h1 className="text-2xl font-bold text-[#03121a] mb-2">Đặt phòng thành công!</h1>
          <p className="text-[#687176] text-[14px] mb-6">{bookingResult.message}</p>

          <div className="bg-[#f7f9fa] rounded-2xl p-5 text-left space-y-3 mb-6">
            <div className="flex items-center justify-between">
              <span className="text-[13px] text-[#687176]">Mã đặt phòng:</span>
              <span className="font-bold text-[#0194f3] text-[18px]">{bookingResult.reservationCode}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-[13px] text-[#687176]">Loại phòng:</span>
              <span className="font-semibold text-[#03121a]">{bookingResult.roomTypeName}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-[13px] text-[#687176]">Phòng số:</span>
              <span className="font-semibold text-[#03121a]">{bookingResult.roomNumber}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-[13px] text-[#687176]">Số đêm:</span>
              <span className="font-semibold text-[#03121a]">{bookingResult.nights} đêm</span>
            </div>
            <div className="flex items-center justify-between border-t border-gray-200 pt-3">
              <span className="text-[13px] font-bold text-[#03121a]">Tổng dự kiến:</span>
              <span className="font-bold text-[#ff5e1f] text-[20px]">{formatVND(bookingResult.estimatedTotal)} đ</span>
            </div>
          </div>

          <p className="text-[12px] text-[#687176] mb-6">
            📞 Khách sạn sẽ gọi xác nhận trong vòng <strong>30 phút</strong>. Vui lòng giữ máy.
          </p>

          <div className="flex gap-3">
            <button onClick={() => router.push("/")} className="flex-1 border border-gray-300 text-[#03121a] font-bold py-3 rounded-xl hover:bg-gray-50 transition-colors">
              Về trang chủ
            </button>
            <button onClick={() => router.push("/search")} className="flex-1 bg-[#0194f3] text-white font-bold py-3 rounded-xl hover:bg-[#007ce8] transition-colors">
              Đặt phòng khác
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ── Loading ──
  if (isLoadingRoom) {
    return (
      <div className="min-h-screen bg-[#f7f9fa] flex items-center justify-center">
        <RefreshCw size={32} className="animate-spin text-[#0194f3]" />
      </div>
    );
  }

  // ── Error: no room ──
  if (!room) {
    return (
      <div className="min-h-screen bg-[#f7f9fa] flex items-center justify-center px-4">
        <div className="text-center">
          <AlertCircle size={48} className="text-red-400 mx-auto mb-4" />
          <h2 className="text-xl font-bold text-[#03121a] mb-2">Phòng không còn trống</h2>
          <p className="text-[#687176] mb-6">Phòng này đã được đặt hoặc không còn khả dụng.</p>
          <button onClick={() => router.back()} className="bg-[#0194f3] text-white font-bold px-8 py-3 rounded-xl hover:bg-[#007ce8]">
            Quay lại tìm phòng khác
          </button>
        </div>
      </div>
    );
  }

  // ── Main booking form ──
  return (
    <div className="min-h-screen bg-[#f7f9fa] py-8">
      <div className="max-w-5xl mx-auto px-4 md:px-8">
        {/* Back button */}
        <button
          onClick={() => router.back()}
          className="flex items-center gap-2 text-[14px] text-[#687176] hover:text-[#0194f3] mb-6 transition-colors"
        >
          <ArrowLeft size={16} /> Quay lại
        </button>

        <h1 className="text-2xl font-bold text-[#03121a] mb-6">Hoàn tất đặt phòng</h1>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* ── Form ── */}
          <form onSubmit={handleSubmit} className="lg:col-span-2 flex flex-col gap-4">
            {/* Guest info */}
            <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
              <h2 className="font-bold text-[#03121a] text-[16px] mb-5 flex items-center gap-2">
                <User size={18} className="text-[#0194f3]" /> Thông tin khách hàng
              </h2>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-[13px] font-bold text-[#03121a] mb-1.5">
                    Họ và tên <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <User size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#687176]" />
                    <input
                      required value={fullName} onChange={e => setFullName(e.target.value)}
                      placeholder="Nguyễn Văn A"
                      className="w-full pl-9 pr-4 py-2.5 text-[14px] border border-gray-300 rounded-xl focus:border-[#0194f3] focus:ring-1 focus:ring-[#0194f3] outline-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-[13px] font-bold text-[#03121a] mb-1.5">
                    Số điện thoại <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <Phone size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#687176]" />
                    <input
                      required value={phoneNumber} onChange={e => setPhoneNumber(e.target.value)}
                      placeholder="0912 345 678" type="tel"
                      className="w-full pl-9 pr-4 py-2.5 text-[14px] border border-gray-300 rounded-xl focus:border-[#0194f3] focus:ring-1 focus:ring-[#0194f3] outline-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-[13px] font-bold text-[#03121a] mb-1.5">Email</label>
                  <div className="relative">
                    <Mail size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#687176]" />
                    <input
                      value={email} onChange={e => setEmail(e.target.value)}
                      placeholder="example@email.com" type="email"
                      className="w-full pl-9 pr-4 py-2.5 text-[14px] border border-gray-300 rounded-xl focus:border-[#0194f3] focus:ring-1 focus:ring-[#0194f3] outline-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-[13px] font-bold text-[#03121a] mb-1.5">Số CCCD/CMND</label>
                  <div className="relative">
                    <FileText size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#687176]" />
                    <input
                      value={idNumber} onChange={e => setIdNumber(e.target.value)}
                      placeholder="0123456789012"
                      className="w-full pl-9 pr-4 py-2.5 text-[14px] border border-gray-300 rounded-xl focus:border-[#0194f3] focus:ring-1 focus:ring-[#0194f3] outline-none"
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Special requests */}
            <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
              <h2 className="font-bold text-[#03121a] text-[16px] mb-4 flex items-center gap-2">
                <FileText size={18} className="text-[#0194f3]" /> Yêu cầu đặc biệt
              </h2>
              <textarea
                value={specialRequests} onChange={e => setSpecialRequests(e.target.value)}
                placeholder="Ví dụ: phòng tầng cao, giường đôi, bữa sáng sớm..."
                rows={3}
                className="w-full px-4 py-3 text-[14px] border border-gray-300 rounded-xl focus:border-[#0194f3] focus:ring-1 focus:ring-[#0194f3] outline-none resize-none"
              />
              <p className="text-[12px] text-[#687176] mt-1.5">* Yêu cầu không được đảm bảo nhưng chúng tôi sẽ cố gắng đáp ứng.</p>
            </div>

            {/* Payment notice */}
            <div className="bg-[#f2f9ff] border border-blue-200 rounded-2xl p-5 flex items-start gap-3">
              <CreditCard size={20} className="text-[#0194f3] shrink-0 mt-0.5" />
              <div>
                <p className="font-bold text-[#03121a] text-[14px] mb-1">Thanh toán tại khách sạn</p>
                <p className="text-[13px] text-[#687176]">
                  Bạn chỉ cần điền thông tin để giữ chỗ. Thanh toán sẽ thực hiện khi nhận phòng. Chúng tôi chấp nhận tiền mặt, chuyển khoản và thẻ ngân hàng.
                </p>
              </div>
            </div>

            {bookingError && (
              <div className="bg-red-50 border border-red-200 rounded-xl p-4 flex items-center gap-3 text-red-700">
                <AlertCircle size={18} className="shrink-0" />
                <span className="text-[14px] font-medium">{bookingError}</span>
              </div>
            )}

            <button
              type="submit"
              disabled={isBooking}
              className="w-full bg-[#0194f3] hover:bg-[#007ce8] disabled:bg-gray-300 disabled:cursor-not-allowed text-white font-bold text-[16px] py-4 rounded-2xl transition-all flex items-center justify-center gap-2 shadow-md"
            >
              {isBooking ? (
                <><RefreshCw size={18} className="animate-spin" /> Đang xử lý...</>
              ) : (
                <><CheckCircle size={18} /> Xác nhận đặt phòng</>
              )}
            </button>
          </form>

          {/* ── Summary Card ── */}
          <div className="lg:col-span-1">
            <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-5 sticky top-28">
              <h3 className="font-bold text-[#03121a] text-[15px] mb-4">Chi tiết đặt phòng</h3>

              <img
                src="https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=600&q=80"
                alt={room.roomTypeName}
                className="w-full h-36 object-cover rounded-xl mb-4"
              />

              <p className="font-bold text-[#03121a] text-[15px]">{room.roomTypeName}</p>
              <p className="text-[12px] text-[#687176] mb-4">Lumina Hotel</p>

              <div className="space-y-2.5 text-[13px]">
                <div className="flex items-center gap-2 text-[#687176]">
                  <Calendar size={14} />
                  <span>Nhận: <strong className="text-[#03121a]">{checkIn}</strong></span>
                </div>
                <div className="flex items-center gap-2 text-[#687176]">
                  <Calendar size={14} />
                  <span>Trả: <strong className="text-[#03121a]">{checkOut}</strong></span>
                </div>
                <div className="flex items-center gap-2 text-[#687176]">
                  <Users size={14} />
                  <span>{adults} người lớn{children > 0 ? `, ${children} trẻ em` : ""}</span>
                </div>
                <div className="flex items-center gap-2 text-[#687176]">
                  <Building2 size={14} />
                  <span>Sức chứa tối đa {room.maxCapacity} người</span>
                </div>
              </div>

              <div className="border-t border-gray-100 mt-4 pt-4 space-y-2 text-[13px]">
                <div className="flex justify-between text-[#687176]">
                  <span>{formatVND(room.pricePerNight)} đ × {nights} đêm</span>
                  <span>{formatVND(room.pricePerNight * nights)} đ</span>
                </div>
                <div className="flex justify-between font-bold text-[15px] text-[#03121a] pt-1 border-t border-gray-100">
                  <span>Tổng dự kiến</span>
                  <span className="text-[#ff5e1f]">{formatVND(totalPrice)} đ</span>
                </div>
              </div>

              <p className="text-[11px] text-[#687176] mt-3">
                * Giá chưa bao gồm VAT. Thanh toán tại khách sạn khi nhận phòng.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function BookPage() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center"><RefreshCw className="animate-spin text-[#0194f3]" size={32} /></div>}>
      <BookPageContent />
    </Suspense>
  );
}
