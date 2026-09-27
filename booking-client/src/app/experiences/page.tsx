"use client";
import React from 'react';
import { ArrowRight, Coffee, Waves, Heart, Sparkles } from 'lucide-react';
import Link from 'next/link';

export default function ExperiencesPage() {
  return (
    <div className="min-h-screen bg-zinc-50 pb-20">
      {/* Hero Section */}
      <div className="relative h-[60vh] min-h-[500px] flex items-center justify-center">
        <div className="absolute inset-0 z-0">
          <img 
            src="/images/experience.jpg" 
            alt="Lumina Experiences" 
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-zinc-900/90 via-zinc-900/40 to-transparent" />
        </div>
        
        <div className="relative z-10 text-center px-4 max-w-4xl mx-auto mt-20">
          <h1 className="text-4xl md:text-6xl font-bold text-white tracking-tight mb-6 leading-tight">
            Nâng tầm trải nghiệm <br className="hidden md:block" /> đánh thức mọi giác quan
          </h1>
          <p className="text-lg text-zinc-200 mb-10 max-w-2xl mx-auto font-light">
            Tại Lumina Boutique Hotel, mỗi khoảnh khắc đều được thiết kế để mang lại cho bạn sự thư giãn tuyệt đối và những kỷ niệm khó quên.
          </p>
          <Link href="/search" className="inline-flex items-center gap-2 bg-brand-primary text-white font-bold px-8 py-4 rounded-full hover:bg-blue-600 transition-colors shadow-lg">
            Đặt phòng ngay <ArrowRight size={18} />
          </Link>
        </div>
      </div>

      {/* Content Section */}
      <div className="container mx-auto px-4 md:px-8 mt-20">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-16 items-center mb-24">
          <div className="space-y-6">
            <div className="w-12 h-12 bg-emerald-50 text-emerald-600 rounded-2xl flex items-center justify-center mb-4">
              <Waves size={24} />
            </div>
            <h2 className="text-3xl font-bold text-zinc-900">Hồ bơi vô cực ngắm hoàng hôn</h2>
            <p className="text-zinc-600 leading-relaxed text-lg">
              Đắm mình trong làn nước xanh mát của hồ bơi vô cực tọa lạc tại tầng thượng. 
              Thưởng thức một ly cocktail đặc trưng và ngắm nhìn hoàng hôn buông xuống, 
              nhuộm màu rực rỡ lên chân trời. Một trải nghiệm tĩnh lặng tuyệt đối giữa lòng thành phố.
            </p>
          </div>
          <div className="rounded-3xl overflow-hidden shadow-2xl">
            <img src="/images/experience.jpg" alt="Infinity Pool" className="w-full h-[400px] object-cover hover:scale-105 transition-transform duration-700" />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-16 items-center mb-24 flex-col-reverse md:flex-row-reverse">
          <div className="space-y-6">
            <div className="w-12 h-12 bg-rose-50 text-rose-600 rounded-2xl flex items-center justify-center mb-4">
              <Heart size={24} />
            </div>
            <h2 className="text-3xl font-bold text-zinc-900">Lumina Spa & Wellness</h2>
            <p className="text-zinc-600 leading-relaxed text-lg">
              Đánh thức năng lượng từ sâu bên trong với các liệu pháp massage độc quyền kết hợp 
              giữa thảo mộc phương Đông và kỹ thuật phương Tây. Không gian thiền định ngập tràn 
              hương tinh dầu sẽ giúp bạn rũ bỏ mọi căng thẳng.
            </p>
          </div>
          <div className="rounded-3xl overflow-hidden shadow-2xl">
            <img src="/images/spa.jpg" alt="Lumina Spa" className="w-full h-[400px] object-cover hover:scale-105 transition-transform duration-700" />
          </div>
        </div>
      </div>
    </div>
  );
}
