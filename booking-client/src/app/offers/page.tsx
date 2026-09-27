"use client";
import React from 'react';
import { ArrowRight, Tag, Gift, Percent } from 'lucide-react';
import Link from 'next/link';

export default function OffersPage() {
  return (
    <div className="min-h-screen bg-zinc-50 pb-20">
      {/* Hero Section */}
      <div className="relative h-[60vh] min-h-[500px] flex items-center justify-center">
        <div className="absolute inset-0 z-0">
          <img 
            src="/images/offers.jpg" 
            alt="Lumina Offers" 
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-zinc-900/90 via-zinc-900/40 to-transparent" />
        </div>
        
        <div className="relative z-10 text-center px-4 max-w-4xl mx-auto mt-20">
          <h1 className="text-4xl md:text-6xl font-bold text-white tracking-tight mb-6 leading-tight">
            Ưu đãi độc quyền <br className="hidden md:block" /> chỉ dành riêng cho bạn
          </h1>
          <p className="text-lg text-zinc-200 mb-10 max-w-2xl mx-auto font-light">
            Khám phá các gói nghỉ dưỡng cao cấp với mức giá ưu đãi nhất. Đặt phòng trực tiếp để nhận thêm những đặc quyền bất ngờ.
          </p>
        </div>
      </div>

      {/* Offers Grid */}
      <div className="container mx-auto px-4 md:px-8 mt-20">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          
          {/* Offer 1 */}
          <div className="bg-white rounded-3xl overflow-hidden shadow-lg border border-zinc-100 group">
            <div className="h-64 overflow-hidden relative">
              <img src="/images/offers.jpg" alt="Romantic Getaway" className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
              <div className="absolute top-4 left-4 bg-white/90 backdrop-blur-sm px-3 py-1 rounded-full text-[12px] font-bold text-rose-600 flex items-center gap-1">
                <HeartIcon size={14} /> Gói cặp đôi
              </div>
            </div>
            <div className="p-8">
              <h3 className="text-xl font-bold text-zinc-900 mb-3">Kỳ Nghỉ Lãng Mạn (Romantic Getaway)</h3>
              <p className="text-zinc-600 mb-6 line-clamp-3">
                Tận hưởng không gian lãng mạn với bữa tối fine-dining dưới ánh nến, hoa hồng trang trí phòng và một chai Champagne chào mừng.
              </p>
              <div className="flex items-center justify-between mt-auto">
                <div>
                  <p className="text-[12px] text-zinc-500 line-through">Từ 8,500,000đ</p>
                  <p className="text-lg font-bold text-luxury-amber">Từ 6,900,000đ</p>
                </div>
                <Link href="/search" className="bg-luxury-navy text-white px-5 py-2.5 rounded-xl font-medium hover:bg-black transition-colors">
                  Đặt ngay
                </Link>
              </div>
            </div>
          </div>

          {/* Offer 2 */}
          <div className="bg-white rounded-3xl overflow-hidden shadow-lg border border-zinc-100 group">
            <div className="h-64 overflow-hidden relative">
              <img src="/images/earlybird.jpg" alt="Early Bird Offer" className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
              <div className="absolute top-4 left-4 bg-white/90 backdrop-blur-sm px-3 py-1 rounded-full text-[12px] font-bold text-emerald-600 flex items-center gap-1">
                <Percent size={14} /> Đặt sớm
              </div>
            </div>
            <div className="p-8">
              <h3 className="text-xl font-bold text-zinc-900 mb-3">Early Bird - Giảm 20%</h3>
              <p className="text-zinc-600 mb-6 line-clamp-3">
                Lên kế hoạch sớm cho chuyến đi của bạn. Đặt phòng trước 30 ngày để nhận ngay ưu đãi giảm 20% giá phòng và miễn phí nâng hạng phòng.
              </p>
              <div className="flex items-center justify-between mt-auto">
                <p className="text-[13px] font-bold text-emerald-600">Áp dụng mọi hạng phòng</p>
                <Link href="/search" className="bg-luxury-navy text-white px-5 py-2.5 rounded-xl font-medium hover:bg-black transition-colors">
                  Đặt ngay
                </Link>
              </div>
            </div>
          </div>

          {/* Offer 3 */}
          <div className="bg-white rounded-3xl overflow-hidden shadow-lg border border-zinc-100 group">
            <div className="h-64 overflow-hidden relative">
              <img src="/images/member.jpg" alt="Member Exclusive Offer" className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
              <div className="absolute top-4 left-4 bg-white/90 backdrop-blur-sm px-3 py-1 rounded-full text-[12px] font-bold text-brand-primary flex items-center gap-1">
                <Gift size={14} /> Đặc quyền hội viên
              </div>
            </div>
            <div className="p-8">
              <h3 className="text-xl font-bold text-zinc-900 mb-3">Lumina Member Exclusive</h3>
              <p className="text-zinc-600 mb-6 line-clamp-3">
                Đăng ký tài khoản miễn phí để nhận ngay Voucher 500k cho dịch vụ Spa, miễn phí check-out trễ đến 14:00 và trái cây chào mừng.
              </p>
              <div className="flex items-center justify-between mt-auto">
                <p className="text-[13px] font-bold text-brand-primary">Dành cho hội viên</p>
                <Link href="/register" className="bg-luxury-navy text-white px-5 py-2.5 rounded-xl font-medium hover:bg-black transition-colors">
                  Đăng ký
                </Link>
              </div>
            </div>
          </div>
          
        </div>
      </div>
    </div>
  );
}

function HeartIcon({ size }: { size: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
    </svg>
  );
}
