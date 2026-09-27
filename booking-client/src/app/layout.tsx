import type { Metadata } from "next";
import { Inter, Playfair_Display } from "next/font/google";
import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

const playfair = Playfair_Display({
  subsets: ["latin", "vietnamese"],
  variable: "--font-playfair",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Lumina Boutique Hotel | Tinh Hoa Lưu Trú",
  description: "Trải nghiệm kỳ nghỉ dưỡng sang trọng, ấm áp tại Lumina Boutique Hotel với dịch vụ cá nhân hóa và thiết kế tinh tế.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" className="scroll-smooth">
      <body className={`${inter.variable} ${playfair.variable} font-sans bg-stone-50 text-stone-900 antialiased`}>
        {/* Navigation Bar */}
        <header className="fixed top-0 left-0 right-0 z-50 bg-white/90 backdrop-blur-md border-b border-stone-200/50 shadow-sm transition-all duration-300">
          <div className="container mx-auto px-6 py-4 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="font-playfair text-2xl font-bold tracking-widest text-amber-900">
                LUMINA
              </span>
            </div>
            <nav className="hidden md:flex items-center gap-8 text-sm font-semibold tracking-widest uppercase">
              <a href="#" className="text-stone-600 hover:text-amber-700 transition-colors">Trang chủ</a>
              <a href="#rooms" className="text-stone-600 hover:text-amber-700 transition-colors">Phòng nghỉ</a>
              <a href="#services" className="text-stone-600 hover:text-amber-700 transition-colors">Dịch vụ</a>
              <a href="#contact" className="text-stone-600 hover:text-amber-700 transition-colors">Liên hệ</a>
            </nav>
            <button className="bg-amber-700 hover:bg-amber-800 text-white px-6 py-2.5 text-sm font-semibold tracking-widest uppercase transition-all shadow-md shadow-amber-700/20 hover:shadow-lg hover:shadow-amber-700/40">
              Đặt phòng
            </button>
          </div>
        </header>

        <main className="min-h-screen pt-[73px]">
          {children}
        </main>

        {/* Footer */}
        <footer className="bg-stone-900 text-stone-400 py-16">
          <div className="container mx-auto px-6 grid grid-cols-1 md:grid-cols-4 gap-12">
            <div className="md:col-span-2">
              <h3 className="font-playfair text-2xl text-white mb-6 tracking-wider">LUMINA BOUTIQUE</h3>
              <p className="text-sm leading-relaxed max-w-sm">
                Nơi trú ẩn sang trọng ngập tràn ánh sáng, mang lại trải nghiệm tinh tế và dịch vụ cá nhân hóa cho mỗi kỳ nghỉ của bạn tại trung tâm thành phố.
              </p>
            </div>
            <div>
              <h3 className="text-sm font-bold tracking-widest text-white mb-6 uppercase">Liên hệ</h3>
              <ul className="text-sm space-y-3">
                <li>123 Đường Tôn Đức Thắng, Q.1, TP.HCM</li>
                <li>(+84) 123 456 789</li>
                <li>hello@luminahotel.com</li>
              </ul>
            </div>
            <div>
              <h3 className="text-sm font-bold tracking-widest text-white mb-6 uppercase">Khám phá</h3>
              <ul className="text-sm space-y-3">
                <li><a href="#" className="hover:text-amber-500 transition-colors">Về chúng tôi</a></li>
                <li><a href="#" className="hover:text-amber-500 transition-colors">Điều khoản & Chính sách</a></li>
                <li><a href="#" className="hover:text-amber-500 transition-colors">Hỗ trợ khách hàng</a></li>
              </ul>
            </div>
          </div>
          <div className="container mx-auto px-6 mt-12 pt-8 border-t border-stone-800 text-sm text-center flex flex-col md:flex-row justify-between items-center">
            <span>&copy; {new Date().getFullYear()} Lumina Boutique Hotel. Bản quyền đã được bảo hộ.</span>
            <div className="flex gap-4 mt-4 md:mt-0">
              <a href="#" className="hover:text-white transition-colors">Facebook</a>
              <a href="#" className="hover:text-white transition-colors">Instagram</a>
            </div>
          </div>
        </footer>
      </body>
    </html>
  );
}
