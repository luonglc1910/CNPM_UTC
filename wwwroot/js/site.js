// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Chuẩn hóa khung hiển thị cho các màn Admin cũ. Các màn đã có hero
// chuyên biệt (Home, Rooms, Guests, ops-page) được giữ nguyên.
document.addEventListener('DOMContentLoaded', function () {
    var main = document.getElementById('main');
    if (!main || main.querySelector(':scope > .dashboard-page, :scope > .rooms-page, :scope > .ops-page')) return;

    var controller = main.dataset.adminController || '';
    var action = main.dataset.adminAction || '';
    var title = document.querySelector('.admin-title')?.textContent?.trim() || document.title.split(' - ')[0];

    var sections = {
        Employees: ['Nhân sự & phân quyền', 'Quản lý hồ sơ, vai trò và trạng thái làm việc của nhân viên.', 'person-badge'],
        HotelServices: ['Dịch vụ khách sạn', 'Quản lý danh mục dịch vụ, giá bán và khả năng cung ứng.', 'cup-hot'],
        Inventory: ['Kho & vật tư', 'Theo dõi tồn kho, nhập hàng và các điều chỉnh cần kiểm soát.', 'boxes'],
        Billing: ['Thu ngân', 'Kiểm soát folio, thanh toán, hóa đơn và công nợ khách lưu trú.', 'receipt'],
        Reports: ['Báo cáo quản trị', 'Theo dõi doanh thu, công suất, dịch vụ, nhân sự và nhật ký hệ thống.', 'graph-up-arrow'],
        Shifts: ['Ca làm việc', 'Quản lý quỹ đầu ca, giao dịch trong ca và bàn giao cuối ca.', 'cash-stack'],
        Settings: ['Thiết lập hệ thống', 'Cấu hình thông tin khách sạn, nghiệp vụ, thanh toán và tích hợp.', 'sliders'],
        Promotions: ['Marketing & nội dung', 'Quản lý chương trình ưu đãi và mức giảm giá đang công bố.', 'tags'],
        Testimonials: ['Marketing & nội dung', 'Quản lý đánh giá khách hàng hiển thị trên các kênh nội dung.', 'chat-quote'],
        Gallery: ['Marketing & nội dung', 'Quản lý hình ảnh khách sạn và thứ tự hiển thị trên trang giới thiệu.', 'images'],
        Reservations: ['Đặt phòng', 'Tra cứu, kiểm soát trạng thái và xử lý vòng đời đơn đặt phòng.', 'journal-bookmark'],
        FrontDesk: ['Lễ tân', 'Xử lý nhận phòng, lưu trú, đổi phòng, gia hạn và trả phòng.', 'bell'],
        Housekeeping: ['Buồng phòng', 'Theo dõi dọn phòng, bảo trì và yêu cầu phục vụ.', 'brush'],
        Guests: ['Khách hàng', 'Quản lý hồ sơ, lịch sử lưu trú và khai báo tạm trú.', 'people'],
        Account: ['Tài khoản', 'Bảo vệ tài khoản và thông tin đăng nhập của bạn.', 'shield-lock']
    };
    var meta = sections[controller] || ['Vận hành khách sạn', 'Quản lý thông tin và thao tác nghiệp vụ trong một giao diện thống nhất.', 'grid'];

    var oldNodes = Array.from(main.childNodes);
    var safeTitle = document.createElement('span');
    safeTitle.textContent = title;
    var page = document.createElement('div');
    page.className = 'ops-page admin-auto-page';
    var hero = document.createElement('section');
    hero.className = 'ops-hero admin-auto-hero';
    hero.innerHTML = '<div><span class="ops-hero-kicker">' + meta[0] + '</span><h2><i class="bi bi-' + meta[2] + ' me-2"></i>' + safeTitle.innerHTML + '</h2><p>' + meta[1] + '</p></div>';
    var body = document.createElement('div');
    body.className = 'admin-auto-body';
    oldNodes.forEach(function (node) { body.appendChild(node); });
    page.append(hero, body);
    main.appendChild(page);
    main.dataset.enhancedAction = action;
});
