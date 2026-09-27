import type { Metadata } from "next";
import Link from "next/link";
import { Noto_Sans } from "next/font/google";
import { Hotel, User, Menu } from "lucide-react";
import "./globals.css";

const notoSans = Noto_Sans({
  subsets: ["latin", "vietnamese"],
  weight: ["300", "400", "500", "600", "700"],
  variable: "--font-noto-sans",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Lumina Hotel | Đặt phòng khách sạn cao cấp",
  description: "Trải nghiệm lưu trú hoàn hảo tại Lumina Boutique Hotel.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" className="scroll-smooth">
      <body className={`${notoSans.variable} font-sans bg-zinc-50 text-zinc-900 antialiased`}>
        {/* Navigation Bar */}
        <header className="fixed top-0 left-0 right-0 z-50 bg-white/95 backdrop-blur-md shadow-sm border-b border-zinc-200/60 transition-all duration-300">
          <div className="container mx-auto px-4 md:px-8 h-20 flex items-center justify-between">
            {/* Logo */}
            <Link href="/" className="flex items-center gap-2.5 cursor-pointer group hover:opacity-90 transition-opacity">
              <div className="w-10 h-10 bg-luxury-navy rounded-xl flex items-center justify-center shadow-md">
                <Hotel size={20} className="text-white" />
              </div>
              <span className="text-2xl font-bold text-luxury-navy tracking-tight">
                lumina<span className="text-brand-primary">.</span>
              </span>
            </Link>

            {/* Desktop Nav */}
            <nav className="hidden md:flex items-center gap-8 text-[14.5px] font-medium text-zinc-600">
              <Link href="/search" className="hover:text-luxury-navy transition-colors">
                Khám phá
              </Link>
              <Link href="/experiences" className="hover:text-luxury-navy transition-colors">
                Trải nghiệm
              </Link>
              <Link href="/offers" className="hover:text-luxury-navy transition-colors">
                Ưu đãi
              </Link>
              <Link href="/contact" className="hover:text-luxury-navy transition-colors">
                Liên hệ
              </Link>
              
              <div className="flex items-center gap-3 ml-4 pl-8 border-l border-zinc-200">
                <Link href="/login" className="flex items-center gap-1.5 text-luxury-navy font-bold px-5 py-2.5 rounded-full hover:bg-zinc-100 transition-colors">
                  <User size={16} />
                  Tra cứu lịch trình
                </Link>
                <Link href="/register" className="bg-luxury-navy text-white font-bold px-6 py-2.5 rounded-full shadow-luxury hover:bg-black transition-colors hover:-translate-y-0.5">
                  Đặt phòng
                </Link>
              </div>
            </nav>

            {/* Mobile Nav Toggle */}
            <button className="md:hidden p-2 text-zinc-900">
              <Menu size={24} />
            </button>
          </div>
        </header>

        <main className="min-h-screen pt-20">
          {children}
        </main>

        {/* Footer */}
        <footer className="bg-white border-t border-zinc-200 py-16 mt-auto">
          <div className="container mx-auto px-4 md:px-8">
            <div className="grid grid-cols-1 md:grid-cols-4 gap-12">
              <div className="md:col-span-1">
                <Link href="/" className="flex items-center gap-2 mb-6 cursor-pointer">
                  <div className="w-8 h-8 bg-luxury-navy rounded-lg flex items-center justify-center">
                    <Hotel size={16} className="text-white" />
                  </div>
                  <span className="text-xl font-bold text-luxury-navy tracking-tight">
                    lumina.
                  </span>
                </Link>
                <p className="text-[14px] text-zinc-500 leading-relaxed mb-6">
                  Định hình lại trải nghiệm lưu trú của bạn bằng sự tận tâm, sang trọng và tiện nghi bậc nhất.
                </p>
              </div>
              
              <div>
                <h3 className="font-bold text-zinc-900 mb-5 uppercase tracking-wider text-[13px]">Về Lumina</h3>
                <ul className="text-[14px] text-zinc-600 space-y-3">
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Câu chuyện thương hiệu</a></li>
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Tuyển dụng</a></li>
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Giải thưởng</a></li>
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Tin tức</a></li>
                </ul>
              </div>
              
              <div>
                <h3 className="font-bold text-zinc-900 mb-5 uppercase tracking-wider text-[13px]">Hỗ trợ</h3>
                <ul className="text-[14px] text-zinc-600 space-y-3">
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Trung tâm trợ giúp</a></li>
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Hướng dẫn đặt phòng</a></li>
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Liên hệ lễ tân</a></li>
                </ul>
              </div>
              
              <div>
                <h3 className="font-bold text-zinc-900 mb-5 uppercase tracking-wider text-[13px]">Chính sách</h3>
                <ul className="text-[14px] text-zinc-600 space-y-3">
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Điều khoản & Điều kiện</a></li>
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Chính sách Quyền riêng tư</a></li>
                  <li><a href="#" className="hover:text-luxury-navy transition-colors">Quy định hoàn hủy</a></li>
                </ul>
              </div>
            </div>
            
            <div className="mt-16 pt-8 border-t border-zinc-100 flex flex-col md:flex-row items-center justify-between gap-4 text-[13px] text-zinc-500 font-medium">
              <div>&copy; {new Date().getFullYear()} Lumina Boutique Hotel. The Art of Hospitality.</div>
              <div className="flex items-center gap-6">
                <a href="#" className="hover:text-zinc-900 transition-colors">Facebook</a>
                <a href="#" className="hover:text-zinc-900 transition-colors">Instagram</a>
                <a href="#" className="hover:text-zinc-900 transition-colors">LinkedIn</a>
              </div>
            </div>
          </div>
        </footer>
      </body>
    </html>
  );
}
