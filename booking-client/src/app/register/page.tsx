"use client";
import React, { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { User, Phone, Mail, FileText, CheckCircle, RefreshCw, ShieldCheck, Hotel, ArrowRight } from "lucide-react";

export default function RegisterPage() {
  const router = useRouter();

  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [idNumber, setIdNumber] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [success, setSuccess] = useState(false);

  function validate() {
    const e: Record<string, string> = {};
    if (!fullName.trim()) e.fullName = "Vui lòng nhập họ và tên.";
    if (!phone.trim()) e.phone = "Vui lòng nhập số điện thoại.";
    else if (!/^(0[3-9]\d{8})$/.test(phone.replace(/\s/g, "")))
      e.phone = "Số điện thoại không đúng định dạng.";
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))
      e.email = "Email không hợp lệ.";
    return e;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const errs = validate();
    if (Object.keys(errs).length > 0) { setErrors(errs); return; }
    setErrors({});
    setIsLoading(true);
    try {
      sessionStorage.setItem("lumina_guest", JSON.stringify({
        fullName: fullName.trim(),
        phoneNumber: phone.replace(/\s/g, ""),
        email: email.trim(),
        idNumber: idNumber.trim(),
      }));
      setSuccess(true);
      setTimeout(() => router.push("/search"), 1500);
    } catch {
      setErrors({ general: "Đã xảy ra lỗi. Vui lòng thử lại." });
    } finally {
      setIsLoading(false);
    }
  }

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-zinc-50">
        <div className="text-center p-12 bg-white rounded-3xl shadow-xl border border-zinc-100 max-w-sm w-full mx-4">
          <div className="w-20 h-20 bg-emerald-50 rounded-full flex items-center justify-center mx-auto mb-6">
            <CheckCircle size={40} className="text-emerald-500" />
          </div>
          <h2 className="text-2xl font-bold text-zinc-900 mb-3 tracking-tight">Hồ sơ đã sẵn sàng!</h2>
          <p className="text-zinc-500 font-medium">Đang đưa bạn đến trang chọn phòng...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex bg-white">
      {/* Left side: Image (Hidden on mobile) */}
      <div className="hidden lg:block lg:w-[45%] relative">
        <img 
          src="/images/earlybird.jpg" 
          alt="Lumina Lifestyle" 
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent" />
        
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
              Đặc quyền thành viên
            </span>
            <h2 className="text-4xl font-bold text-white leading-tight mb-4">
              Khởi đầu cho những <br/> kỳ nghỉ đáng nhớ.
            </h2>
            <p className="text-zinc-200 text-lg max-w-md font-light">
              Gia nhập Lumina để tận hưởng các ưu đãi độc quyền, tích lũy đêm nghỉ và dịch vụ cá nhân hóa thượng lưu.
            </p>
          </div>
        </div>
      </div>

      {/* Right side: Form */}
      <div className="w-full lg:w-[55%] flex flex-col justify-center px-6 py-12 lg:px-24 relative">
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

          <div className="mb-10">
            <h1 className="text-3xl font-bold text-zinc-900 leading-tight mb-3 tracking-tight">Tạo hồ sơ khách hàng</h1>
            <p className="text-[15px] text-zinc-500">Hoàn tất thủ tục nhanh chóng để tiến hành đặt phòng.</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Full name */}
            <div>
              <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">
                Họ và tên <span className="text-rose-500">*</span>
              </label>
              <div className="relative group">
                <User size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400 group-focus-within:text-luxury-navy transition-colors" />
                <input
                  value={fullName} onChange={e => setFullName(e.target.value)}
                  placeholder="Ví dụ: Nguyễn Văn An"
                  className={`w-full pl-12 pr-4 py-3.5 text-[15px] bg-zinc-50 border rounded-xl outline-none transition-all hover:bg-zinc-100/50 ${errors.fullName ? "border-rose-300 focus:border-rose-500 focus:ring-1 focus:ring-rose-500" : "border-zinc-200 focus:border-luxury-navy focus:ring-1 focus:ring-luxury-navy"}`}
                />
              </div>
              {errors.fullName && <p className="text-[12px] text-rose-500 mt-1.5 font-medium">{errors.fullName}</p>}
            </div>

            {/* Phone */}
            <div>
              <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">
                Số điện thoại <span className="text-rose-500">*</span>
              </label>
              <div className="relative group">
                <Phone size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400 group-focus-within:text-luxury-navy transition-colors" />
                <input
                  value={phone} onChange={e => setPhone(e.target.value)}
                  placeholder="Ví dụ: 0912 345 678" type="tel"
                  className={`w-full pl-12 pr-4 py-3.5 text-[15px] bg-zinc-50 border rounded-xl outline-none transition-all hover:bg-zinc-100/50 ${errors.phone ? "border-rose-300 focus:border-rose-500 focus:ring-1 focus:ring-rose-500" : "border-zinc-200 focus:border-luxury-navy focus:ring-1 focus:ring-luxury-navy"}`}
                />
              </div>
              {errors.phone && <p className="text-[12px] text-rose-500 mt-1.5 font-medium">{errors.phone}</p>}
            </div>

            {/* Email */}
            <div>
              <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">Email liên hệ</label>
              <div className="relative group">
                <Mail size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400 group-focus-within:text-luxury-navy transition-colors" />
                <input
                  value={email} onChange={e => setEmail(e.target.value)}
                  placeholder="hello@example.com" type="email"
                  className={`w-full pl-12 pr-4 py-3.5 text-[15px] bg-zinc-50 border rounded-xl outline-none transition-all hover:bg-zinc-100/50 ${errors.email ? "border-rose-300 focus:border-rose-500 focus:ring-1 focus:ring-rose-500" : "border-zinc-200 focus:border-luxury-navy focus:ring-1 focus:ring-luxury-navy"}`}
                />
              </div>
              {errors.email && <p className="text-[12px] text-rose-500 mt-1.5 font-medium">{errors.email}</p>}
            </div>

            {/* ID Number */}
            <div>
              <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">Số CCCD / Hộ chiếu</label>
              <div className="relative group">
                <FileText size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400 group-focus-within:text-luxury-navy transition-colors" />
                <input
                  value={idNumber} onChange={e => setIdNumber(e.target.value)}
                  placeholder="Điền số định danh của bạn" maxLength={12}
                  className="w-full pl-12 pr-4 py-3.5 text-[15px] bg-zinc-50 border border-zinc-200 rounded-xl focus:border-luxury-navy focus:ring-1 focus:ring-luxury-navy outline-none transition-all hover:bg-zinc-100/50"
                />
              </div>
            </div>

            <div className="bg-zinc-50 border border-zinc-200/60 rounded-xl p-4 flex gap-3 items-start mt-2">
              <ShieldCheck size={20} className="text-zinc-400 shrink-0 mt-0.5" />
              <p className="text-[12.5px] text-zinc-500 leading-relaxed">
                Thông tin của bạn được mã hóa cấp độ cao và chỉ dùng để quản lý dịch vụ lưu trú theo tiêu chuẩn bảo mật toàn cầu.
              </p>
            </div>

            {errors.general && (
              <div className="bg-rose-50 border border-rose-200 rounded-xl p-4 text-[13.5px] text-rose-600 flex items-center gap-2">
                <span className="shrink-0 font-bold">⚠️</span>
                <span>{errors.general}</span>
              </div>
            )}

            <button
              type="submit" disabled={isLoading}
              className="w-full bg-luxury-navy hover:bg-black active:scale-[0.98] disabled:bg-zinc-300 disabled:active:scale-100 text-white font-bold py-4 rounded-xl transition-all shadow-luxury flex items-center justify-center gap-2 text-[15px] mt-8 group"
            >
              {isLoading ? (
                <><RefreshCw size={18} className="animate-spin" /> Đang thiết lập...</>
              ) : (
                <>Hoàn tất & Tiếp tục <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" /></>
              )}
            </button>
          </form>

          <div className="mt-10 text-center">
            <p className="text-[14.5px] text-zinc-500">
              Đã có mã đặt phòng?{" "}
              <Link href="/login" className="text-brand-primary font-bold hover:underline">
                Tra cứu lịch trình
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
