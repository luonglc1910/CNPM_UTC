"use client";
import React from 'react';
import { MapPin, Phone, Mail, Clock } from 'lucide-react';

export default function ContactPage() {
  return (
    <div className="min-h-screen bg-zinc-50 pb-20">
      {/* Hero Section */}
      <div className="relative h-[50vh] min-h-[400px] flex items-center justify-center">
        <div className="absolute inset-0 z-0">
          <img 
            src="/images/contact.jpg" 
            alt="Lumina Contact" 
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-luxury-navy/70 backdrop-blur-[2px]" />
        </div>
        
        <div className="relative z-10 text-center px-4 max-w-4xl mx-auto mt-16">
          <h1 className="text-4xl md:text-5xl font-bold text-white tracking-tight mb-4">
            Liên hệ với chúng tôi
          </h1>
          <p className="text-lg text-zinc-300 font-light">
            Chúng tôi luôn ở đây, sẵn sàng biến mọi yêu cầu của bạn thành hiện thực.
          </p>
        </div>
      </div>

      {/* Content */}
      <div className="container mx-auto px-4 md:px-8 mt-16">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-16">
          
          {/* Contact Info */}
          <div>
            <h2 className="text-3xl font-bold text-zinc-900 mb-8">Thông tin liên hệ</h2>
            <div className="space-y-8">
              <div className="flex gap-4 items-start">
                <div className="w-12 h-12 bg-white rounded-2xl shadow-sm flex items-center justify-center shrink-0 border border-zinc-200">
                  <MapPin size={24} className="text-luxury-navy" />
                </div>
                <div>
                  <h3 className="text-lg font-bold text-zinc-900 mb-1">Địa chỉ</h3>
                  <p className="text-zinc-600 leading-relaxed">
                    Số 1, Đại lộ Ánh Sáng, Quận Hoàn Kiếm, <br/>
                    Thủ đô Hà Nội, Việt Nam
                  </p>
                </div>
              </div>

              <div className="flex gap-4 items-start">
                <div className="w-12 h-12 bg-white rounded-2xl shadow-sm flex items-center justify-center shrink-0 border border-zinc-200">
                  <Phone size={24} className="text-luxury-navy" />
                </div>
                <div>
                  <h3 className="text-lg font-bold text-zinc-900 mb-1">Điện thoại</h3>
                  <p className="text-zinc-600">Đặt phòng: <strong className="text-zinc-900">+84 24 0123 4567</strong></p>
                  <p className="text-zinc-600">Lễ tân 24/7: <strong className="text-zinc-900">+84 24 0123 4568</strong></p>
                </div>
              </div>

              <div className="flex gap-4 items-start">
                <div className="w-12 h-12 bg-white rounded-2xl shadow-sm flex items-center justify-center shrink-0 border border-zinc-200">
                  <Mail size={24} className="text-luxury-navy" />
                </div>
                <div>
                  <h3 className="text-lg font-bold text-zinc-900 mb-1">Email</h3>
                  <p className="text-zinc-600">hello@luminahotel.com</p>
                </div>
              </div>
            </div>
          </div>

          {/* Contact Form */}
          <div className="bg-white rounded-3xl shadow-xl border border-zinc-100 p-8 md:p-10">
            <h2 className="text-2xl font-bold text-zinc-900 mb-6">Gửi tin nhắn cho chúng tôi</h2>
            <form className="space-y-5" onSubmit={(e) => { e.preventDefault(); alert("Tin nhắn đã được gửi!"); }}>
              <div>
                <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">Họ và tên</label>
                <input required type="text" className="w-full px-4 py-3 bg-zinc-50 border border-zinc-200 rounded-xl focus:border-luxury-navy focus:ring-1 focus:ring-luxury-navy outline-none" placeholder="Nguyễn Văn An" />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                <div>
                  <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">Email</label>
                  <input required type="email" className="w-full px-4 py-3 bg-zinc-50 border border-zinc-200 rounded-xl focus:border-luxury-navy focus:ring-1 focus:ring-luxury-navy outline-none" placeholder="email@example.com" />
                </div>
                <div>
                  <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">Số điện thoại</label>
                  <input type="tel" className="w-full px-4 py-3 bg-zinc-50 border border-zinc-200 rounded-xl focus:border-luxury-navy focus:ring-1 focus:ring-luxury-navy outline-none" placeholder="0912 345 678" />
                </div>
              </div>
              <div>
                <label className="block text-[13px] font-bold text-zinc-900 mb-2 uppercase tracking-wide">Lời nhắn</label>
                <textarea required rows={4} className="w-full px-4 py-3 bg-zinc-50 border border-zinc-200 rounded-xl focus:border-luxury-navy focus:ring-1 focus:ring-luxury-navy outline-none resize-none" placeholder="Bạn cần hỗ trợ gì?"></textarea>
              </div>
              <button type="submit" className="w-full bg-luxury-navy text-white font-bold py-4 rounded-xl hover:bg-black transition-colors shadow-luxury">
                Gửi Yêu Cầu
              </button>
            </form>
          </div>

        </div>
      </div>
    </div>
  );
}
