import html, subprocess, os
OUT = os.path.dirname(os.path.abspath(__file__))
W = 1500
C1, C2 = 560, 1010
LX, RX = 75, 75
GAP = 100
TOP = 150
BX1, BX2 = 330, 1230

def actor(x, y, label):
    s = f'<g stroke="#1f2937" stroke-width="2.5" fill="none"><circle cx="{x}" cy="{y-42}" r="16" fill="#fff"/>'
    s += f'<line x1="{x}" y1="{y-26}" x2="{x}" y2="{y+14}"/><line x1="{x-28}" y1="{y-12}" x2="{x+28}" y2="{y-12}"/>'
    s += f'<line x1="{x}" y1="{y+14}" x2="{x-24}" y2="{y+50}"/><line x1="{x}" y1="{y+14}" x2="{x+24}" y2="{y+50}"/></g>'
    for i, ln in enumerate(label.split('\n')):
        s += f'<text x="{x}" y="{y+76+i*22}" text-anchor="middle" font-size="18" font-weight="600" fill="#111827">{html.escape(ln)}</text>'
    return s

def build(d):
    global W, BX2
    two = any(u[2]==2 for u in d['ucs'])
    W = 1500 if two else 1000
    BX2 = 1230 if two else 780
    ucs = {u[0]: u for u in d['ucs']}
    rows = max(u[3] for u in d['ucs']) + 1
    H = TOP + rows * GAP + 120
    pos = {k: ((C1 if u[2] == 1 else C2), TOP + u[3] * GAP + 40) for k, u in ucs.items()}
    ap = {}
    for a in d['actors']:
        ys = [pos[u][1] for (aa, u) in d['assoc'] if aa == a[0]]
        ap[a[0]] = (LX, sum(ys) / len(ys))
    # avoid overlapping actors on same side
    for side in 'LR':
        lst = sorted([a for a in d['actors'] if a[2] == side], key=lambda a: ap[a[0]][1])
        for i in range(1, len(lst)):
            p, c = ap[lst[i-1][0]], ap[lst[i][0]]
            if c[1] - p[1] < 190:
                ap[lst[i][0]] = (c[0], p[1] + 190)
    H = max(H, int(max(v[1] for v in ap.values())) + 160)
    s = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}" font-family="Helvetica, Arial, sans-serif">']
    s.append('<defs><marker id="ar" markerWidth="12" markerHeight="10" refX="11" refY="5" orient="auto"><path d="M0,0 L12,5 L0,10" fill="none" stroke="#374151" stroke-width="1.8"/></marker></defs>')
    s.append(f'<rect width="{W}" height="{H}" fill="#fff"/>')
    s.append(f'<text x="{W/2}" y="60" text-anchor="middle" font-size="30" font-weight="700" fill="#111827">{html.escape(d["title"])}</text>')
    s.append(f'<rect x="{BX1}" y="95" width="{BX2-BX1}" height="{H-125}" rx="14" fill="#f8fafc" stroke="#1f2937" stroke-width="2.5"/>')
    s.append(f'<text x="{(BX1+BX2)/2}" y="125" text-anchor="middle" font-size="19" font-weight="700" fill="#1f2937">{html.escape(d["system"])}</text>')
    EW, EH = 330, 64
    def edge_pt(k, tx, ty):
        cx, cy = pos[k]
        import math
        dx, dy = tx-cx, ty-cy
        a, b = EW/2, EH/2
        t = 1/math.sqrt((dx/a)**2+(dy/b)**2) if (dx or dy) else 0
        return cx+dx*t, cy+dy*t
    # associations (elbow connectors via a vertical bus per actor)
    order = sorted(d['actors'], key=lambda a: ap[a[0]][1])
    colors = ['#dc2626', '#16a34a', '#7c3aed', '#ea580c']
    for i, a in enumerate(order):
        bus = 160 + i * 38
        ax, ay = ap[a[0]]
        hy = ay - 12
        targets = [pos[u] for (aa, u) in d['assoc'] if aa == a[0]]
        ys = [t[1] for t in targets] + [hy]
        s.append(f'<line x1="{ax+28}" y1="{hy}" x2="{bus}" y2="{hy}" stroke="{colors[i]}" stroke-width="2.2"/>')
        s.append(f'<line x1="{bus}" y1="{min(ys)}" x2="{bus}" y2="{max(ys)}" stroke="{colors[i]}" stroke-width="2.2"/>')
        for (aa, u) in d['assoc']:
            if aa != a[0]: continue
            ux, uy = pos[u]
            s.append(f'<line x1="{bus}" y1="{uy}" x2="{ux-165}" y2="{uy}" stroke="{colors[i]}" stroke-width="2.2"/>')
    # relations
    labels = []
    for f, t, kind in d['rel']:
        fx, fy = pos[f]; tx, ty = pos[t]
        x1, y1 = edge_pt(f, tx, ty); x2, y2 = edge_pt(t, fx, fy)
        s.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="#374151" stroke-width="1.8" stroke-dasharray="8,6" marker-end="url(#ar)"/>')
        labels.append(((x1+x2)/2, (y1+y2)/2, f'«{kind}»'))
    cols = {'1': ('#dbeafe', '#2563eb'), '2': ('#fef3c7', '#d97706')}
    for k, (_, label, col, row) in ucs.items():
        x, y = pos[k]
        fill, stroke = cols[str(col)]
        s.append(f'<ellipse cx="{x}" cy="{y}" rx="{EW/2}" ry="{EH/2}" fill="{fill}" stroke="{stroke}" stroke-width="2.2"/>')
        lines = label.split('\n')
        for i, ln in enumerate(lines):
            yy = y + 6 - (len(lines)-1)*11 + i*22
            s.append(f'<text x="{x}" y="{yy}" text-anchor="middle" font-size="17" font-weight="600" fill="#111827">{html.escape(ln)}</text>')
    for x, y, t in labels:
        s.append(f'<rect x="{x-38:.1f}" y="{y-12:.1f}" width="76" height="22" rx="4" fill="#f8fafc"/><text x="{x:.1f}" y="{y+5:.1f}" text-anchor="middle" font-size="14" font-style="italic" fill="#374151">{t}</text>')
    for a in d['actors']:
        x, y = ap[a[0]]
        s.append(actor(x, y, a[1]))
    s.append('</svg>')
    return '\n'.join(s), H

AD, LT, KH = ('admin','Quản lý\n(Admin)','L'), ('lt','Lễ tân\n(Receptionist)','L'), ('kh','Khách hàng\n(Guest)','R')
SYS = 'Hệ thống Quản lý Khách sạn'
D = []
D.append(dict(file='01_UC_TongQuat', title='Sơ đồ Use Case tổng quát', system=SYS,
 actors=[AD, LT, KH],
 ucs=[('a','Đăng nhập / Đổi mật khẩu',1,0),('b','Quản lý danh mục\n(phòng, dịch vụ, kho)',1,1),('c','Quản lý nhân viên & phân quyền',1,2),
      ('d','Quản lý đặt phòng',1,3),('e','Check-in / Check-out / Đổi phòng',1,4),('f','Ghi nhận dịch vụ & buồng phòng',1,5),
      ('g','Thanh toán & Lập hóa đơn',1,6),('h','Quản lý ca thu ngân',1,7),('i','Báo cáo & Nhật ký thao tác',1,8),
      ('j','Tra cứu phòng & Đặt phòng online',1,9),('k','Quản lý tài khoản & tích điểm',1,10)],
 assoc=[('admin','a'),('admin','b'),('admin','c'),('admin','d'),('admin','e'),('admin','g'),('admin','i'),
        ('lt','a'),('lt','d'),('lt','e'),('lt','f'),('lt','g'),('lt','h'),
        ('kh','j'),('kh','k')],
 rel=[]))
D.append(dict(file='02_UC_DatPhong', title='Use Case: Quản lý đặt phòng', system=SYS, actors=[LT,AD,KH],
 ucs=[('a','Tra cứu phòng trống',1,0),('b','Tạo đơn đặt phòng',1,1),('c','Ghi nhận tiền cọc',1,2),('d','Sửa đơn (đổi ngày / phòng / gia hạn)',1,3),
      ('e','Hủy đơn đặt phòng',1,4),('f','Đánh dấu No-show',1,5),('g','Xem / lọc danh sách đơn',1,6),
      ('h','Chọn / tạo hồ sơ khách',2,1),('i','Kiểm tra xung đột phòng',2,3),('j','Tính phí hủy & hoàn cọc',2,4),('k','In phiếu xác nhận đặt phòng',2,0)],
 assoc=[('lt','a'),('lt','b'),('lt','c'),('lt','d'),('lt','e'),('lt','f'),('lt','g'),('admin','e'),('admin','g'),('kh','a'),('kh','b')],
 rel=[('b','h','include'),('b','a','include'),('d','i','include'),('e','j','include'),('k','b','extend')]))
D.append(dict(file='03_UC_LeTan', title='Use Case: Nghiệp vụ lễ tân (Front Desk)', system=SYS, actors=[LT,AD],
 ucs=[('a','Xem bảng điều khiển lễ tân',1,0),('b','Check-in khách có đặt trước',1,1),('c','Thêm khách vào lượt lưu trú',1,2),('d','Đổi phòng giữa kỳ',1,3),
      ('e','Gia hạn lưu trú',1,4),('f','Check-out',1,5),('g','Xem chi tiết lượt lưu trú',1,6),
      ('h','Nhập danh sách khách ở cùng',2,1),('i','Tạo Stay + Folio',2,2),('j','Kiểm phòng & minibar',2,5),('k','Phụ thu / Miễn chênh lệch giá',2,3),('l','Chốt Folio & chuyển thanh toán',2,6)],
 assoc=[('lt','a'),('lt','b'),('lt','c'),('lt','d'),('lt','e'),('lt','f'),('lt','g'),('admin','d'),('admin','g')],
 rel=[('b','h','include'),('b','i','include'),('f','j','include'),('f','l','include'),('k','d','extend'),('k','c','extend')]))
D.append(dict(file='04_UC_DichVu_BuongPhong', title='Use Case: Dịch vụ & Buồng phòng', system=SYS, actors=[LT,AD,KH],
 ucs=[('a','Ghi nhận dịch vụ vào folio',1,0),('b','Hủy dòng dịch vụ ghi nhầm',1,1),('c','Xem bảng trạng thái buồng phòng',1,2),('d','Cập nhật trạng thái dọn phòng',1,3),
      ('e','Kiểm minibar khi check-out',1,4),('f','Báo hỏng / yêu cầu bảo trì',1,5),('g','Xử lý yêu cầu phục vụ của khách',1,6),
      ('h','Tự động trừ tồn kho',2,0),('i','Hoàn tồn kho',2,1),('j','Chuyển phòng Dirty → Available',2,3),('k','Chuyển phòng sang Maintenance',2,5)],
 assoc=[('lt','a'),('lt','b'),('lt','c'),('lt','d'),('lt','e'),('lt','f'),('lt','g'),('admin','f'),('admin','c'),('kh','g')],
 rel=[('a','h','include'),('b','i','include'),('d','j','include'),('f','k','include'),('e','a','include')]))
D.append(dict(file='05_UC_ThuNgan_ThanhToan', title='Use Case: Thu ngân & Thanh toán', system=SYS, actors=[LT,AD],
 ucs=[('a','Xem Folio / số dư cần thu',1,0),('b','Áp dụng giảm giá',1,1),('c','Thanh toán đa phương thức',1,2),('d','Đối trừ cọc / hoàn tiền thừa',1,3),
      ('e','Chốt & in hóa đơn',1,4),('f','Hủy hóa đơn (Void)',1,5),('g','Thu phí hủy / no-show',1,6),('h','Mở ca / Đóng ca',1,7),('i','Báo cáo cuối ca',1,8),
      ('j','Duyệt giảm giá vượt hạn mức',2,1),('k','Kiểm tra ca đang mở',2,4),('l','Ghi nhật ký thao tác',2,5),('m','Tính chênh lệch quỹ',2,8)],
 assoc=[('lt','a'),('lt','b'),('lt','c'),('lt','d'),('lt','e'),('lt','g'),('lt','h'),('lt','i'),('admin','f'),('admin','j'),('admin','e'),('admin','i')],
 rel=[('j','b','extend'),('e','k','include'),('f','l','include'),('i','m','include'),('h','m','include')]))
D.append(dict(file='06_UC_BaoCao_QuanTri', title='Use Case: Quản trị & Báo cáo (Admin)', system=SYS, actors=[AD],
 ucs=[('a','Quản lý loại phòng & phòng',1,0),('b','Quản lý dịch vụ & tồn kho',1,1),('c','Quản lý nhân viên & tài khoản',1,2),('d','Cấu hình hệ thống\n(VAT, giờ, phụ thu, chính sách)',1,3),
      ('e','Xem Dashboard tổng quan',1,4),('f','Báo cáo doanh thu',1,5),('g','Báo cáo công suất / ADR / RevPAR',1,6),('h','Xem nhật ký thao tác (Audit log)',1,7),
      ('i','Xuất Excel / CSV',2,5),('j','Khóa / mở khóa tài khoản',2,2),('k','Cảnh báo tồn kho dưới định mức',2,1)],
 assoc=[('admin','a'),('admin','b'),('admin','c'),('admin','d'),('admin','e'),('admin','f'),('admin','g'),('admin','h')],
 rel=[('f','i','extend'),('g','i','extend'),('c','j','include'),('b','k','include')]))
D.append(dict(file='07_UC_CongKhachHang', title='Use Case: Cổng khách hàng (Client Portal)', system='Cổng đặt phòng trực tuyến', actors=[KH,LT],
 ucs=[('a','Đăng ký / Đăng nhập tài khoản',1,0),('b','Xem loại phòng & thư viện ảnh',1,1),('c','Tra cứu phòng trống',1,2),('d','Đặt phòng trực tuyến',1,3),
      ('e','Xem / hủy đơn đặt phòng',1,4),('f','Xem lịch sử lưu trú',1,5),('g','Xem điểm tích lũy & khuyến mãi',1,6),
      ('h','Áp dụng mã khuyến mãi',2,3),('i','Xác nhận đơn (lễ tân)',2,4),('j','Tính phí hủy',2,5)],
 assoc=[('kh','a'),('kh','b'),('kh','c'),('kh','d'),('kh','e'),('kh','f'),('kh','g'),('lt','i')],
 rel=[('d','c','include'),('h','d','extend'),('e','j','include')]))

chrome = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
for d in D:
    svg, H = build(d)
    p = os.path.join(OUT, d['file'] + '.svg'); open(p, 'w').write(svg)
    html_p = os.path.join(OUT, d['file'] + '.html')
    open(html_p, 'w').write(f'<html><body style="margin:0">{svg}</body></html>')
    subprocess.run([chrome, '--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=2',
                    f'--window-size={W},{H}', f'--screenshot={os.path.join(OUT, d["file"]+".png")}', 'file://' + html_p],
                   capture_output=True)
    os.remove(html_p)
print('done')
