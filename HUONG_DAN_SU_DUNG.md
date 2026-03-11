# HƯ�sNG DẪN SỬ DỤNG H�? THỐNG QUẢN LÝ KH�"A H�OC CNTT

## �Ys? KH�zI Đ�~NG H�? THỐNG

### Yêu cầu h�? th�'ng:
- .NET 6.0 hoặc cao hơn
- SQL Server hoặc SQL Server Express
- Visual Studio 2022 hoặc VS Code

### Cách chạy ứng dụng:
```bash
cd asp.net\Website_Ban_Khoa_Hoc_CNTT
dotnet run --urls "http://localhost:5000"
```
dotnet build Website_Ban_Khoa_Hoc_CNTT.sln

Truy cập: **http://localhost:5000**

---

## �Y'� T�?I KHOẢN MẶC Đ�SNH

### Tài khoản Admin:
- **Email:** admin@example.com
- **Password:** Admin123!
- **Quyền:** Toàn quyền quản tr�< h�? th�'ng

### Tài khoản User thử nghi�?m:
- **Email:** user@example.com  
- **Password:** User123!
- **Quyền:** Người dùng thường

### Tạo tài khoản m�>i:
1. Truy cập trang �'�fng ký: `/Account/Register`
2. Điền thông tin �'ầy �'ủ
3. Xác nhận email (nếu �'ược cấu hình)

---

## �Y�� GIAO DI�?N NGƯ�oI D�TNG

### Trang chủ:
- Hi�fn th�< danh sách khóa học n�.i bật
- Tìm kiếm khóa học theo từ khóa
- Lọc theo danh mục, mức �'�T, giá

### Trang khóa học:
- Xem chi tiết khóa học
- Đ�fng ký học
- Xem n�Ti dung bài học (sau khi �'�fng ký)
- Theo dõi tiến �'�T học tập

### Trang cá nhân:
- Quản lý thông tin cá nhân
- Xem khóa học �'ã �'�fng ký
- Theo dõi tiến �'�T học tập
- Xem chứng ch�? �'ã �'ạt �'ược

---

## �sT️ H�? THỐNG QUẢN TR�S ADMIN

### Đ�fng nhập Admin:
1. Truy cập: `/Admin` hoặc `/Account/Login`
2. Đ�fng nhập bằng tài khoản admin
3. Tự �'�Tng chuy�fn hư�>ng �'ến Dashboard

### Dashboard Admin:
- Th�'ng kê t�.ng quan h�? th�'ng
- Bi�fu �'�" người dùng, khóa học, doanh thu
- Hoạt �'�Tng gần �'ây

---

## �Y"s QUẢN LÝ KH�"A H�OC

### Danh sách khóa học:
- **Đường dẫn:** `/Admin/Courses`
- **Chức n�fng:**
  - Xem danh sách tất cả khóa học
  - Tìm kiếm theo tên, mô tả
  - Lọc theo danh mục, trạng thái, giá
  - Sắp xếp theo ngày tạo, cập nhật

### Tạo khóa học m�>i:
- **Đường dẫn:** `/Admin/Courses/Create`
- **Thông tin cần nhập:**
  - Tên khóa học
  - Mô tả ngắn và chi tiết
  - Danh mục
  - Mức �'�T (Beginner, Intermediate, Advanced)
  - Giá và giá khuyến mãi
  - Hình ảnh �'ại di�?n
  - Video gi�>i thi�?u

### Ch�?nh sửa khóa học:
- **Đường dẫn:** `/Admin/Courses/Edit/{id}`
- **Chức n�fng:**
  - Cập nhật thông tin khóa học
  - Thay �'�.i trạng thái (Draft, Published, Archived)
  - Quản lý n�Ti dung bài học

### Xóa khóa học:
- **Đường dẫn:** `/Admin/Courses/Delete/{id}`
- **Lưu ý:** Ki�fm tra ràng bu�Tc v�>i enrollments trư�>c khi xóa

---

## �Y"- QUẢN LÝ B�?I H�OC

### Danh sách bài học:
- **Đường dẫn:** `/Admin/Lessons`
- **Chức n�fng:**
  - Xem tất cả bài học theo khóa học
  - Tìm kiếm theo tên bài học
  - Lọc theo loại n�Ti dung (Video, Text, Quiz)

### Tạo bài học m�>i:
- **Đường dẫn:** `/Admin/Lessons/Create`
- **Thông tin cần nhập:**
  - Tên bài học
  - Mô tả
  - Khóa học thu�Tc về
  - Chương (Chapter)
  - Loại n�Ti dung
  - Thứ tự hi�fn th�<
  - N�Ti dung chi tiết

### Quản lý n�Ti dung:
- **Video:** Upload file hoặc nhúng YouTube/Vimeo
- **Text:** Soạn thảo n�Ti dung v�>i rich text editor
- **Quiz:** Tạo câu hỏi trắc nghi�?m

---

## �Y'� QUẢN LÝ NGƯ�oI D�TNG

### Danh sách người dùng:
- **Đường dẫn:** `/Admin/Users`
- **Chức n�fng:**
  - Xem tất cả người dùng
  - Tìm kiếm theo email, tên
  - Lọc theo vai trò, trạng thái
  - Phân quyền người dùng

### Quản lý vai trò:
- **Admin:** Toàn quyền quản tr�<
- **User:** Người dùng thường
- **Instructor:** Giảng viên (nếu có)

### Thao tác v�>i người dùng:
- Xem chi tiết thông tin
- Ch�?nh sửa thông tin
- Khóa/m�Y khóa tài khoản
- Phân quyền vai trò

---

## �Y"S QUẢN LÝ Đ�,NG KÝ H�OC

### Danh sách �'�fng ký:
- **Đường dẫn:** `/Admin/Enrollments`
- **Chức n�fng:**
  - Xem tất cả �'�fng ký học
  - Tìm kiếm theo người dùng, khóa học
  - Lọc theo trạng thái, ngày �'�fng ký
  - Theo dõi tiến �'�T học tập

### Trạng thái �'�fng ký:
- **Pending:** Chờ xác nhận
- **Active:** Đang học
- **Completed:** Hoàn thành
- **Cancelled:** Đã hủy

### Thao tác quản lý:
- Xác nhận �'�fng ký
- Hủy �'�fng ký
- Cập nhật tiến �'�T
- Xuất báo cáo

---

## �Y"^ QUẢN LÝ TIẾN �'�T H�OC TẬP

### Danh sách tiến �'�T:
- **Đường dẫn:** `/Admin/LessonProgresses`
- **Chức n�fng:**
  - Xem tiến �'�T học tập của tất cả học viên
  - Tìm kiếm theo User ID, trạng thái
  - Lọc theo bài học, mức �'�T hoàn thành
  - Cập nhật tiến �'�T trực tiếp

### Cập nhật tiến �'�T:
- **Cách 1:** Click nút edit trong danh sách
- **Cách 2:** Sử dụng modal popup cập nhật nhanh
- **Cách 3:** Bulk update cho nhiều tiến �'�T cùng lúc

### Th�'ng kê tiến �'�T:
- **Đường dẫn:** `/Admin/LessonProgresses/Statistics`
- **N�Ti dung:**
  - T�.ng quan tiến �'�T học tập
  - Bi�fu �'�" hoàn thành theo thời gian
  - Top bài học �'ược học nhiều nhất
  - Th�'ng kê theo ngày/tuần/tháng

---

## �Y'� QUẢN LÝ B�ONH LUẬN

### Danh sách bình luận:
- **Đường dẫn:** `/Admin/Comments`
- **Chức n�fng:**
  - Xem tất cả bình luận
  - Tìm kiếm theo n�Ti dung, người dùng
  - Lọc theo khóa học, trạng thái
  - Duy�?t/ẩn bình luận

### Moderation:
- Duy�?t bình luận m�>i
- Ẩn bình luận không phù hợp
- Trả lời bình luận học viên
- Xóa bình luận spam

---

## �YZ� QUẢN LÝ KHUYẾN M�fI

### Danh sách khuyến mãi:
- **Đường dẫn:** `/Admin/Promotions`
- **Chức n�fng:**
  - Tạo mã giảm giá
  - Thiết lập % hoặc s�' tiền giảm
  - Đặt thời hạn sử dụng
  - Gi�>i hạn s�' lần sử dụng

### Loại khuyến mãi:
- **Percentage:** Giảm theo phần tr�fm
- **Fixed Amount:** Giảm s�' tiền c�' �'�<nh
- **Free Shipping:** Mi�.n phí (nếu có)

---

## �Y�? QUẢN LÝ CHỨNG CH�^

### Danh sách chứng ch�?:
- **Đường dẫn:** `/Admin/Certificates`
- **Chức n�fng:**
  - Xem chứng ch�? �'ã cấp
  - Tìm kiếm theo người dùng, khóa học
  - Tạo chứng ch�? m�>i
  - In/xuất chứng ch�? PDF

### Tự �'�Tng cấp chứng ch�?:
- Khi học viên hoàn thành 100% khóa học
- Đạt �'i�fm t�'i thi�fu (nếu có quiz)
- Thời gian học �'ủ yêu cầu

---

## �Y"� C�?I ĐẶT H�? THỐNG

### Cấu hình database:
- File: `appsettings.json`
- Connection string SQL Server
- Chạy migrations: `dotnet ef database update`

### Cấu hình email:
- SMTP settings cho gửi email xác nhận
- Email templates cho thông báo

### Cấu hình file upload:
- Thư mục lưu trữ: `wwwroot/uploads`
- Gi�>i hạn kích thư�>c file
- Đ�<nh dạng file cho phép

---

## �Ys� XỬ LÝ SỰ CỐ

### L�-i thường gặp:
1. **Không �'�fng nhập �'ược:**
   - Ki�fm tra email/password
   - Xóa cache browser
   - Ki�fm tra database connection

2. **Không upload �'ược file:**
   - Ki�fm tra quyền thư mục
   - Ki�fm tra kích thư�>c file
   - Ki�fm tra �'�<nh dạng file

3. **L�-i database:**
   - Chạy lại migrations
   - Ki�fm tra connection string
   - Backup và restore database

### Liên h�? h�- trợ:
- Email: support@example.com
- Hotline: 1900-xxxx
- Documentation: /docs

---

## �Y"� GHI CH�s QUAN TR�ONG

1. **Backup �'�<nh kỳ:** Sao lưu database và files thường xuyên
2. **Bảo mật:** Thay �'�.i password mặc �'�<nh ngay lập tức
3. **Cập nhật:** Ki�fm tra và cập nhật h�? th�'ng �'�<nh kỳ
4. **Monitor:** Theo dõi logs và performance

**Phiên bản:** 1.0.0  
**Cập nhật lần cu�'i:** 2024-12-19
