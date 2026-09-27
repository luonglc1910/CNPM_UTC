import Image from "next/image";

export default function Home() {
  return (
    <div className="flex flex-col w-full">
      {/* Hero Section */}
      <section className="relative w-full h-[80vh] min-h-[600px] flex items-center justify-center">
        {/* Background Image */}
        <div className="absolute inset-0 w-full h-full">
          <img
            src="https://images.unsplash.com/photo-1542314831-c6a4d1424869?auto=format&fit=crop&q=80"
            alt="Lumina Boutique Hotel"
            className="w-full h-full object-cover"
          />
          {/* Overlay gradient */}
          <div className="absolute inset-0 bg-stone-900/40 bg-gradient-to-t from-stone-900/80 via-transparent to-stone-900/30"></div>
        </div>

        {/* Hero Content */}
        <div className="relative z-10 text-center px-6 mt-16 max-w-4xl mx-auto">
          <p className="text-amber-400 font-semibold tracking-[0.3em] uppercase text-sm mb-6">
            Chào mừng đến với Lumina
          </p>
          <h1 className="font-playfair text-5xl md:text-7xl text-white font-bold mb-6 drop-shadow-lg">
            Nơi Khơi Nguồn Cảm Hứng
          </h1>
          <p className="text-stone-100 text-lg md:text-xl font-light mb-12 max-w-2xl mx-auto drop-shadow-md">
            Trải nghiệm không gian nghỉ dưỡng tinh tế, dịch vụ hoàn hảo và những khoảnh khắc đáng nhớ tại trung tâm thành phố.
          </p>
        </div>

        {/* Booking Bar (Floating) */}
        <div className="absolute -bottom-16 left-0 right-0 z-20 px-6">
          <div className="max-w-5xl mx-auto bg-white shadow-2xl rounded-sm p-4 md:p-8 flex flex-col md:flex-row gap-4 items-end border border-stone-100">
            <div className="w-full md:flex-1">
              <label className="block text-xs font-bold text-stone-500 uppercase tracking-wider mb-2">
                Ngày nhận phòng
              </label>
              <input
                type="date"
                className="w-full p-3 border-b-2 border-stone-200 focus:border-amber-700 outline-none transition-colors text-stone-800 bg-transparent"
              />
            </div>
            <div className="w-full md:flex-1">
              <label className="block text-xs font-bold text-stone-500 uppercase tracking-wider mb-2">
                Ngày trả phòng
              </label>
              <input
                type="date"
                className="w-full p-3 border-b-2 border-stone-200 focus:border-amber-700 outline-none transition-colors text-stone-800 bg-transparent"
              />
            </div>
            <div className="w-full md:w-48">
              <label className="block text-xs font-bold text-stone-500 uppercase tracking-wider mb-2">
                Số khách
              </label>
              <select className="w-full p-3 border-b-2 border-stone-200 focus:border-amber-700 outline-none transition-colors text-stone-800 bg-transparent appearance-none cursor-pointer">
                <option>1 Người lớn</option>
                <option>2 Người lớn</option>
                <option>2 Người lớn, 1 Trẻ em</option>
                <option>Gia đình</option>
              </select>
            </div>
            <button className="w-full md:w-auto bg-amber-700 hover:bg-amber-800 text-white px-8 py-4 font-semibold tracking-widest uppercase transition-colors shadow-lg mt-4 md:mt-0 whitespace-nowrap">
              Kiểm tra phòng
            </button>
          </div>
        </div>
      </section>

      {/* Featured Rooms Section */}
      <section id="rooms" className="py-32 px-6 bg-stone-50 mt-16 md:mt-0">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="font-playfair text-4xl text-stone-900 mb-4">Hạng Phòng Nổi Bật</h2>
            <div className="w-24 h-1 bg-amber-700 mx-auto mb-6"></div>
            <p className="text-stone-500 max-w-2xl mx-auto">
              Từ những căn phòng Superior ấm cúng đến Suite sang trọng đẳng cấp, mỗi không gian tại Lumina đều được thiết kế tỉ mỉ để mang lại sự thư giãn tuyệt đối.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {/* Room 1 */}
            <div className="group bg-white rounded-sm overflow-hidden shadow-sm hover:shadow-xl transition-all duration-300 border border-stone-100">
              <div className="relative h-64 overflow-hidden">
                <img
                  src="https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&q=80"
                  alt="Superior Room"
                  className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700"
                />
              </div>
              <div className="p-8 text-center">
                <h3 className="font-playfair text-2xl text-stone-900 mb-2">Phòng Superior</h3>
                <p className="text-stone-500 text-sm mb-6 line-clamp-2">
                  Lựa chọn hoàn hảo cho khách công tác hoặc cặp đôi, với cửa sổ lớn đón ánh sáng tự nhiên.
                </p>
                <p className="text-amber-700 font-bold text-lg mb-6">Từ 1,200,000đ <span className="text-stone-400 text-sm font-normal">/ đêm</span></p>
                <button className="w-full border border-amber-700 text-amber-700 hover:bg-amber-700 hover:text-white py-3 text-sm uppercase tracking-widest font-semibold transition-colors">
                  Xem chi tiết
                </button>
              </div>
            </div>

            {/* Room 2 */}
            <div className="group bg-white rounded-sm overflow-hidden shadow-sm hover:shadow-xl transition-all duration-300 border border-stone-100">
              <div className="relative h-64 overflow-hidden">
                <img
                  src="https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&q=80"
                  alt="Deluxe City View"
                  className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700"
                />
              </div>
              <div className="p-8 text-center">
                <h3 className="font-playfair text-2xl text-stone-900 mb-2">Deluxe City View</h3>
                <p className="text-stone-500 text-sm mb-6 line-clamp-2">
                  Tầm nhìn toàn cảnh thành phố nhộn nhịp, không gian rộng rãi trang bị bồn tắm hiện đại.
                </p>
                <p className="text-amber-700 font-bold text-lg mb-6">Từ 1,800,000đ <span className="text-stone-400 text-sm font-normal">/ đêm</span></p>
                <button className="w-full border border-amber-700 text-amber-700 hover:bg-amber-700 hover:text-white py-3 text-sm uppercase tracking-widest font-semibold transition-colors">
                  Xem chi tiết
                </button>
              </div>
            </div>

            {/* Room 3 */}
            <div className="group bg-white rounded-sm overflow-hidden shadow-sm hover:shadow-xl transition-all duration-300 border border-stone-100">
              <div className="relative h-64 overflow-hidden">
                <img
                  src="https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&q=80"
                  alt="Lumina Suite"
                  className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700"
                />
              </div>
              <div className="p-8 text-center">
                <h3 className="font-playfair text-2xl text-stone-900 mb-2">Lumina Suite</h3>
                <p className="text-stone-500 text-sm mb-6 line-clamp-2">
                  Hạng phòng cao cấp nhất với phòng khách riêng biệt, minibar miễn phí và ban công ngắm cảnh.
                </p>
                <p className="text-amber-700 font-bold text-lg mb-6">Từ 3,500,000đ <span className="text-stone-400 text-sm font-normal">/ đêm</span></p>
                <button className="w-full border border-amber-700 text-amber-700 hover:bg-amber-700 hover:text-white py-3 text-sm uppercase tracking-widest font-semibold transition-colors">
                  Xem chi tiết
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Services Section */}
      <section id="services" className="py-24 px-6 bg-white border-t border-stone-100">
        <div className="max-w-6xl mx-auto flex flex-col md:flex-row items-center gap-16">
          <div className="flex-1 space-y-8">
            <h2 className="font-playfair text-4xl text-stone-900 leading-tight">
              Khám Phá Dịch Vụ <br /> Đẳng Cấp Tại Lumina
            </h2>
            <p className="text-stone-600 leading-relaxed">
              Chúng tôi không chỉ cung cấp một nơi để ngủ, mà là một trải nghiệm sống. Từ nhà hàng Fine Dining với thực đơn Á-Âu đến Spa trị liệu sức khỏe toàn diện, mọi thứ đều sẵn sàng để chiều lòng bạn.
            </p>
            <ul className="space-y-4">
              <li className="flex items-center gap-4 text-stone-800 font-medium">
                <div className="w-10 h-10 rounded-full bg-amber-100 text-amber-700 flex items-center justify-center">✓</div>
                Nhà hàng & Bar Rooftop
              </li>
              <li className="flex items-center gap-4 text-stone-800 font-medium">
                <div className="w-10 h-10 rounded-full bg-amber-100 text-amber-700 flex items-center justify-center">✓</div>
                Spa & Massage Trị liệu
              </li>
              <li className="flex items-center gap-4 text-stone-800 font-medium">
                <div className="w-10 h-10 rounded-full bg-amber-100 text-amber-700 flex items-center justify-center">✓</div>
                Hồ bơi vô cực ngắm cảnh
              </li>
            </ul>
          </div>
          <div className="flex-1 relative">
            <img
              src="https://images.unsplash.com/photo-1544161515-4ab6ce6db874?auto=format&fit=crop&q=80"
              alt="Spa Service"
              className="rounded-sm shadow-2xl w-full h-auto object-cover"
            />
            <div className="absolute -bottom-8 -left-8 w-48 h-48 bg-amber-700/10 rounded-full blur-3xl -z-10"></div>
          </div>
        </div>
      </section>
    </div>
  );
}
