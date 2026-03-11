# Website Bán Khóa Học CNTT

M�Tt website bán khóa học công ngh�? thông tin �'ược xây dựng bằng ASP.NET Core 8.0 v�>i giao di�?n hi�?n �'ại, trẻ trung và �'ẹp mắt.

## Tính n�fng chính

### Giao di�?n người dùng (Shop)
- **Trang chủ**: Hi�fn th�< khóa học n�.i bật, m�>i nhất và th�'ng kê
- **Danh sách khóa học**: Xem tất cả khóa học v�>i tính n�fng tìm kiếm và lọc
- **Chi tiết khóa học**: Xem thông tin chi tiết, n�Ti dung và �'ánh giá
- **Đ�fng ký/Đ�fng nhập**: Quản lý tài khoản người dùng
- **Responsive Design**: Tương thích v�>i mọi thiết b�<

### Giao di�?n quản tr�< (Admin)
- **Dashboard**: Th�'ng kê t�.ng quan về khóa học, học viên, doanh thu
- **Quản lý khóa học**: Thêm, sửa, xóa khóa học và n�Ti dung
- **Quản lý người dùng**: Phân quyền và quản lý tài khoản
- **Báo cáo**: Th�'ng kê chi tiết về hoạt �'�Tng website

## Công ngh�? sử dụng

- **Backend**: ASP.NET Core 8.0, Entity Framework Core
- **Frontend**: Bootstrap 5, Font Awesome, AOS Animation
- **Database**: SQL Server (sử dụng database có sẵn)
- **Authentication**: ASP.NET Core Identity
- **UI Libraries**: CDN (Bootstrap, Font Awesome, Google Fonts)

## Cấu trúc Database

Website sử dụng database có sẵn v�>i các bảng:
- `Categories`: Danh mục khóa học
- `Courses`: Thông tin khóa học
- `Chapters`: Chương học
- `Enrollments`: Đ�fng ký khóa học
- `LessonProgresses`: Tiến �'�T học tập
- `Comments`: Bình luận
- `Certificates`: Chứng ch�?
- `Discounts`: Mã giảm giá
- `Finances`: Quản lý tài chính

## Cài �'ặt và chạy

### Yêu cầu h�? th�'ng
- .NET 8.0 SDK
- SQL Server
- Visual Studio 2022 hoặc VS Code

### Hư�>ng dẫn cài �'ặt

1. **Clone project**:
   ```bash
   git clone [repository-url]
   cd Website_Ban_Khoa_Hoc_CNTT
   ```

2. **Cấu hình database**:
   - M�Y file `appsettings.json`
   - Cập nhật connection string �'�f kết n�'i �'ến SQL Server của bạn:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=ECourse;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
     }
   }
   ```

3. **Chạy ứng dụng**:
   ```bash
   # Cách 1: Sử dụng script
   run.bat
   
   # Cách 2: Sử dụng dotnet CLI
   dotnet restore
   dotnet build
   dotnet run
   ```

4. **Truy cập website**:
   - **Trang chủ**: http://localhost:5000 hoặc https://localhost:5001
   - **Admin**: https://localhost:5001/Admin

### Tài khoản mặc �'�<nh

Sau khi chạy lần �'ầu, h�? th�'ng sẽ tự �'�Tng tạo các tài khoản:

- **Admin**: 
  - Email: admin@elearning.com
  - Password: Admin@123

- **Instructor**:
  - Email: instructor@elearning.com  
  - Password: Instructor@123

- **Student**:
  - Email: student@elearning.com
  - Password: Student@123

## Cấu trúc thư mục

```
Website_Ban_Khoa_Hoc_CNTT/
�"o�"?�"? Areas/
�",   �""�"?�"? Admin/              # Khu vực quản tr�<
�"o�"?�"? Controllers/            # Controllers cho shop
�"o�"?�"? Data/                   # DbContext và cấu hình database
�"o�"?�"? Models/                 # Entity models
�"o�"?�"? Services/               # Business logic services
�"o�"?�"? Views/                  # Razor views
�",   �"o�"?�"? Home/              # Views cho trang chủ
�",   �"o�"?�"? Shared/            # Layout và partial views
�",   �""�"?�"? ...
�"o�"?�"? wwwroot/               # Static files
�"o�"?�"? appsettings.json       # Cấu hình ứng dụng
�"o�"?�"? Program.cs             # Entry point
�""�"?�"? README.md              # Tài li�?u này
```

## Tính n�fng �'ang phát tri�fn

- H�? th�'ng thanh toán trực tuyến
- Quản lý bài học và video
- H�? th�'ng quiz và ki�fm tra
- Chat support trực tuyến
- Mobile app

## Liên h�?

Nếu có thắc mắc hoặc cần h�- trợ, vui lòng liên h�?:
- Email: support@elearning-cntt.com
- Website: https://elearning-cntt.com

## License

Dự án này �'ược phát tri�fn cho mục �'ích học tập và thương mại.
