# �Y"� Hư�>ng dẫn cấu hình Gmail �'�f gửi email quên mật khẩu

## �Y"� Bư�>c 1: Tạo App Password cho Gmail

### 1.1. Bật xác thực 2 bư�>c (2FA)
1. Đ�fng nhập vào Gmail
2. Vào **Google Account** �?' **Security**
3. Tìm **2-Step Verification** và bật nó lên
4. Làm theo hư�>ng dẫn �'�f thiết lập

### 1.2. Tạo App Password
1. Sau khi bật 2FA, vào **Security** �?' **App passwords**
2. Chọn **Mail** và **Windows Computer** (hoặc Other)
3. Nhập tên: "ELearning Website"
4. Click **Generate**
5. **Copy** mật khẩu 16 ký tự �'ược tạo ra

## �Y"� Bư�>c 2: Cấu hình appsettings.json

M�Y file `appsettings.json` và thay �'�.i:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",        // �?� Thay bằng email của bạn
    "SenderName": "ELearning CNTT Website",
    "Username": "your-email@gmail.com",           // �?� Thay bằng email của bạn
    "Password": "your-16-digit-app-password"      // �?� Thay bằng App Password vừa tạo
  }
}
```

## �Y"� Bư�>c 3: Test chức n�fng

1. Chạy website: `dotnet run`
2. Vào trang �'�fng nhập: `http://localhost:5000/Identity/Account/Login`
3. Click **"Quên mật khẩu?"**
4. Nhập email và click **"Gửi liên kết �'ặt lại mật khẩu"**
5. Ki�fm tra email �'�f nhận liên kết reset password

## �Y"� Bư�>c 4: Tạo trang ResetPassword (nếu chưa có)

Nếu cần, tôi sẽ tạo thêm trang ResetPassword �'�f hoàn thi�?n chức n�fng.

## �s�️ Lưu ý bảo mật

1. **KH�"NG** commit App Password lên Git
2. Sử dụng **Environment Variables** cho production:
   ```bash
   set EmailSettings__Password=your-app-password
   ```
3. Hoặc sử dụng **Azure Key Vault** cho production

## �YZ� Kết quả

Sau khi cấu hình xong:
- �o. User có th�f reset password qua email
- �o. Email �'ược gửi v�>i template �'ẹp
- �o. Liên kết reset có thời hạn 24h
- �o. Bảo mật cao v�>i App Password

## �Y?~ Troubleshooting

### L�-i "Authentication failed"
- Ki�fm tra App Password có �'úng không
- Đảm bảo �'ã bật 2FA

### L�-i "SMTP timeout"
- Ki�fm tra kết n�'i internet
- Thử port 465 v�>i SSL thay vì 587

### Email không nhận �'ược
- Ki�fm tra thư mục Spam/Junk
- Ki�fm tra email có t�"n tại không
