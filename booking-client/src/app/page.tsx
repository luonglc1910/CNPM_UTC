"use client";
import React from "react";
import { useRouter } from "next/navigation";
import SearchBar from "@/features/hotel-booking/components/SearchBar";

export default function Home() {
  const router = useRouter();

  return (
    <div className="flex flex-col w-full">
      {/* Hero Section */}
      <section className="relative w-full bg-[#f7f9fa]">
        {/* Background Image Container */}
        <div className="relative w-full h-80 md:h-105">
          <img
            src="/banner.jpg"
            alt="Lumina Hero"
            className="w-full h-full object-cover"
          />
          {/* Dark gradient overlay for text readability */}
          <div className="absolute inset-0 bg-linear-to-t from-black/85 via-black/30 to-transparent"></div>

          {/* Hero Text */}
          <div className="absolute bottom-32 left-0 right-0 px-4 md:px-8 max-w-7xl mx-auto">
            <h1 className="text-3xl md:text-[34px] text-white font-bold mb-2 leading-snug drop-shadow-md">
              Điểm đến tiếp theo của bạn?<br />Đặt phòng khách sạn giá tốt với Lumina
            </h1>
            <p className="text-white/90 text-sm md:text-base font-medium drop-shadow">
              Khám phá nhiều lựa chọn từ khách sạn, biệt thự, resort và hơn thế nữa
            </p>
          </div>
        </div>

        {/* SearchBar Floating */}
        <div className="relative z-10 -mt-16 px-4 md:px-8 max-w-7xl mx-auto">
          <SearchBar
            sticky={false}
            onSearch={(vals) => {
              const params = new URLSearchParams({
                location: vals.location,
                checkIn: vals.checkIn,
                checkOut: vals.checkOut,
                adults: String(vals.adults),
                children: String(vals.children),
                rooms: String(vals.rooms),
              });
              router.push(`/search?${params.toString()}`);
            }}
          />
        </div>
      </section>

      {/* Benefits Section */}
      <section className="max-w-7xl mx-auto px-4 md:px-8 mt-12 w-full">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="flex items-center gap-4 bg-white p-5 rounded-2xl border border-gray-200 shadow-sm hover:shadow-md transition-shadow">
            <div className="w-12 h-12 bg-[#f2f9ff] rounded-xl flex items-center justify-center text-[#0194f3]">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M19 4H18V2H16V4H8V2H6V4H5C3.89 4 3.01 4.9 3.01 6L3 20C3 21.1 3.89 22 5 22H19C20.1 22 21 21.1 21 20V6C21 4.9 20.1 4 19 4ZM19 20H5V10H19V20ZM19 8H5V6H19V8Z" fill="currentColor" /></svg>
            </div>
            <div>
              <h4 className="font-bold text-[#03121a] text-[15px]">Hủy miễn phí</h4>
              <p className="text-[13px] text-[#687176] mt-1">Hủy hoặc nhận hoàn tiền bất cứ khi nào bạn cần.</p>
            </div>
          </div>

          <div className="flex items-center gap-4 bg-white p-5 rounded-2xl border border-gray-200 shadow-sm hover:shadow-md transition-shadow">
            <div className="w-12 h-12 bg-[#f2f9ff] rounded-xl flex items-center justify-center text-[#0194f3]">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M21 4H3C1.89 4 1.01 4.89 1.01 6L1 18C1 19.11 1.89 20 3 20H21C22.11 20 23 19.11 23 18V6C23 4.89 22.11 4 21 4ZM21 18H3V12H21V18ZM21 8H3V6H21V8Z" fill="currentColor" /></svg>
            </div>
            <div>
              <h4 className="font-bold text-[#03121a] text-[15px]">Nhiều phương thức thanh toán</h4>
              <p className="text-[13px] text-[#687176] mt-1">Lựa chọn thanh toán đáng tin cậy dành cho bạn.</p>
            </div>
          </div>

          <div className="flex items-center gap-4 bg-white p-5 rounded-2xl border border-gray-200 shadow-sm hover:shadow-md transition-shadow">
            <div className="w-12 h-12 bg-[#f2f9ff] rounded-xl flex items-center justify-center text-[#0194f3]">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M11.99 2C6.47 2 2 6.48 2 12S6.47 22 11.99 22C17.52 22 22 17.52 22 12S17.52 2 11.99 2ZM12 20C7.58 20 4 16.42 4 12S7.58 4 12 4 20 7.58 20 12 16.42 20 12 20ZM12.5 7H11V13L16.25 16.15L17 14.92L12.5 12.25V7Z" fill="currentColor" /></svg>
            </div>
            <div>
              <h4 className="font-bold text-[#03121a] text-[15px]">Trung tâm hỗ trợ 24/7</h4>
              <p className="text-[13px] text-[#687176] mt-1">Bạn có thể liên hệ chúng tôi bất cứ lúc nào.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Recommended Rooms */}
      <section className="max-w-7xl mx-auto px-4 md:px-8 mt-16 mb-24 w-full">
        <h2 className="text-2xl font-bold text-[#03121a] mb-6">Đặt phòng khách sạn giá tốt</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
          {[
            { name: "Lumina Superior Room", price: "1.200.000", rating: "8.5", reviews: "124", image: "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&q=80&w=600" },
            { name: "Deluxe City View", price: "1.800.000", rating: "9.2", reviews: "356", image: "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&q=80&w=600" },
            { name: "Lumina Suite Premium", price: "3.500.000", rating: "9.8", reviews: "89", image: "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&q=80&w=600" },
            { name: "Family Connecting Room", price: "2.400.000", rating: "8.9", reviews: "210", image: "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&q=80&w=600" }
          ].map((room, idx) => (
            <div key={idx} onClick={() => router.push('/search')} className="bg-white rounded-2xl overflow-hidden border border-gray-200 shadow-sm hover:shadow-lg transition-all cursor-pointer group flex flex-col h-full">
              <div className="relative h-40 overflow-hidden">
                <img src={room.image} alt={room.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
              </div>
              <div className="p-4 flex flex-col grow">
                <h3 className="font-bold text-[#03121a] text-[16px] mb-2 line-clamp-2 leading-snug">{room.name}</h3>
                <div className="flex items-center gap-1 mb-4">
                  <div className="flex items-center text-[#0194f3] font-bold text-[14px]">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor" xmlns="http://www.w3.org/2000/svg" className="mr-1">
                      <path d="M12 17.27L18.18 21L16.54 13.97L22 9.24L14.81 8.63L12 2L9.19 8.63L2 9.24L7.46 13.97L5.82 21L12 17.27Z" />
                    </svg>
                    {room.rating}
                  </div>
                  <span className="text-[#687176] text-[13px]">({room.reviews} đánh giá)</span>
                </div>
                <div className="mt-auto pt-2 flex flex-col items-end border-t border-gray-100">
                  <span className="text-[12px] text-[#687176]">Giá mỗi đêm từ</span>
                  <span className="text-[#ff5e1f] font-bold text-[18px]">VND {room.price}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
