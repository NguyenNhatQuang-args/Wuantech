# WuanTech E-Commerce API

Một API hoàn chỉnh cho hệ thống thương mại điện tử được xây dựng với ASP.NET Core, Entity Framework Core và SQL Server.

## 🚀 Tính năng chính

### 🔐 Xác thực & Phân quyền
- JWT Authentication với Refresh Token
- Role-based Authorization (Admin, Staff, Customer)
- Password Reset & Email Verification
- Secure API endpoints

### 🛍️ Quản lý sản phẩm
- CRUD operations cho sản phẩm
- Phân loại theo danh mục & thương hiệu
- Hình ảnh và thông số kỹ thuật
- Hệ thống đánh giá & review
- Tìm kiếm & lọc sản phẩm

### 🛒 Mua hàng
- Giỏ hàng với session persistence
- Quy trình đặt hàng hoàn chỉnh
- Quản lý trạng thái đơn hàng
- Tính toán phí vận chuyển & thuế
- Theo dõi đơn hàng

### 📦 Quản lý kho
- Multi-warehouse inventory
- Stock transactions tracking
- Low stock alerts
- Purchase orders
- Stock transfer between warehouses

### 💳 Thanh toán
- Multiple payment gateways support
- Order confirmation emails
- Payment status tracking
- Refund management

### 📊 Báo cáo & Analytics
- Sales reports
- Inventory reports
- Customer analytics
- Dashboard statistics

### 🔧 Tính năng khác
- Todo management
- Favorites/Wishlist
- Coupon system
- Email notifications
- API versioning
- Health checks
- Comprehensive logging

## 🏗️ Kiến trúc

```
WuanTech.API/
├── Controllers/          # API Controllers
├── Data/                # Database Context
├── DTOs/                # Data Transfer Objects
├── Services/            # Business Logic Services
│   ├── Interfaces/      # Service Interfaces
│   └── Implementations/ # Service Implementations
├── Models/              # Entity Models
├── Middleware/          # Custom Middleware
├── Extensions/          # Extension Methods
├── Helpers/             # Utility Helpers
├── Constants/           # Application Constants
└── Validation/          # Custom Validators
```

## 🛠️ Công nghệ sử dụng

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server với Entity Framework Core
- **Authentication**: JWT Bearer Token
- **API Documentation**: Swagger/OpenAPI
- **Validation**: Data Annotations + FluentValidation
- **Email**: SMTP with HTML templates
- **Caching**: In-Memory Cache
- **Testing**: xUnit (planned)

## 📋 Yêu cầu hệ thống

- .NET 8.0 SDK
- SQL Server 2019+ hoặc SQL Server Express
- Visual Studio 2022 hoặc VS Code
- Git

## ⚡ Cài đặt nhanh

### 1. Clone repository
```bash
git clone https://github.com/your-username/wuantech-api.git
cd wuantech-api
```

### 2. Restore packages
```bash
dotnet restore
```

### 3. Cấu hình Database
Cập nhật connection string trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=WuanTechDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 4. Tạo và seed database
```bash
# Chạy script SQL để tạo database và dữ liệu mẫu
# Import file WuanTechDatabase.sql vào SQL Server Management Studio
```

### 5. Chạy ứng dụng
```bash
dotnet run
```

API sẽ chạy tại:
- HTTP: `http://localhost:7102`
- HTTPS: `https://localhost:7103`
- Swagger UI: `https://localhost:7103` (Development mode)

## 🔧 Cấu hình chi tiết

### appsettings.json

```json
{
  "Jwt": {
    "Key": "your-super-secret-key-here",
    "Issuer": "WuanTechAPI",
    "Audience": "WuanTechClient",
    "ExpirationInMinutes": 60
  },
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password"
  }
}
```

### Environment Variables (Production)
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
ConnectionStrings__DefaultConnection="your-production-connection-string"
Jwt__Key="your-production-jwt-key"
```

## 📚 API Endpoints

### Authentication
```
POST /api/auth/register      # Đăng ký
POST /api/auth/login         # Đăng nhập
POST /api/auth/refresh-token # Refresh token
POST /api/auth/logout        # Đăng xuất
POST /api/auth/forgot-password # Quên mật khẩu
POST /api/auth/reset-password  # Reset mật khẩu
GET  /api/auth/me           # Thông tin user hiện tại
```

### Products
```
GET    /api/products                    # Danh sách sản phẩm
GET    /api/products/{id}              # Chi tiết sản phẩm
GET    /api/products/featured          # Sản phẩm nổi bật
GET    /api/products/new               # Sản phẩm mới
GET    /api/products/bestsellers       # Sản phẩm bán chạy
GET    /api/products/{id}/related      # Sản phẩm liên quan
GET    /api/products/search            # Tìm kiếm sản phẩm
POST   /api/products                   # Tạo sản phẩm (Admin)
PUT    /api/products/{id}              # Cập nhật sản phẩm (Admin)
DELETE /api/products/{id}              # Xóa sản phẩm (Admin)
POST   /api/products/{id}/reviews      # Thêm đánh giá
```

### Categories
```
GET    /api/categories        # Danh sách danh mục
GET    /api/categories/{id}   # Chi tiết danh mục
GET    /api/categories/menu   # Menu danh mục
POST   /api/categories        # Tạo danh mục (Admin)
PUT    /api/categories/{id}   # Cập nhật danh mục (Admin)
DELETE /api/categories/{id}   # Xóa danh mục (Admin)
```

### Cart
```
GET    /api/cart              # Xem giỏ hàng
POST   /api/cart/add          # Thêm vào giỏ hàng
PUT    /api/cart/items/{id}   # Cập nhật số lượng
DELETE /api/cart/items/{id}   # Xóa khỏi giỏ hàng
DELETE /api/cart/clear        # Xóa toàn bộ giỏ hàng
```

### Orders
```
GET    /api/orders                 # Danh sách đơn hàng
GET    /api/orders/{id}           # Chi tiết đơn hàng
POST   /api/orders                # Tạo đơn hàng
DELETE /api/orders/{id}/cancel    # Hủy đơn hàng
GET    /api/orders/{id}/tracking  # Theo dõi đơn hàng
```

### User Management
```
GET  /api/user/profile          # Thông tin profile
PUT  /api/user/profile          # Cập nhật profile
POST /api/user/change-password  # Đổi mật khẩu
```

### Favorites
```
GET    /api/favorites           # Danh sách yêu thích
POST   /api/favorites/{id}      # Thêm vào yêu thích
DELETE /api/favorites/{id}      # Xóa khỏi yêu thích
GET    /api/favorites/{id}/check # Kiểm tra đã yêu thích
```

### Todos
```
GET    /api/todos               # Danh sách todo
POST   /api/todos               # Tạo todo
PUT    /api/todos/{id}          # Cập nhật todo
DELETE /api/todos/{id}          # Xóa todo
PATCH  /api/todos/{id}/toggle   # Toggle hoàn thành
GET    /api/todos/stats         # Thống kê todo
```

## 🔒 Authentication

API sử dụng JWT Bearer Token. Để truy cập các endpoint được bảo vệ:

1. Đăng nhập để nhận token:
```json
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "password123"
}
```

2. Sử dụng token trong header:
```
Authorization: Bearer <your-jwt-token>
```

## 👥 Roles & Permissions

### Admin
- Quản lý tất cả sản phẩm, danh mục
- Xem báo cáo và thống kê
- Quản lý user và đơn hàng
- Quản lý kho và inventory

### Staff
- Quản lý sản phẩm (CRUD)
- Xử lý đơn hàng
- Quản lý inventory

### Customer
- Duyệt và mua sản phẩm
- Quản lý giỏ hàng và đơn hàng
- Đánh giá sản phẩm
- Quản lý profile và todos

## 📧 Email Configuration

Để gửi email, cấu hình SMTP trong `appsettings.json`:

```json
{
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-specific-password",
    "FromName": "WuanTech Store"
  }
}
```

Với Gmail, cần tạo App Password thay vì dùng mật khẩu thường.

## 🛡️ Security Features

- JWT với secure secret key
- Password hashing với BCrypt
- Input validation và sanitization
- CORS configuration
- Rate limiting ready
- SQL injection protection via EF Core
- XSS protection headers

## 📊 Database Schema

Database bao gồm các bảng chính:

- **Products**: Sản phẩm
- **Categories**: Danh mục
- **Brands**: Thương hiệu
- **Users**: Người dùng
- **Orders**: Đơn hàng
- **OrderItems**: Chi tiết đơn hàng
- **CartItems**: Giỏ hàng
- **Reviews**: Đánh giá
- **Inventory**: Tồn kho
- **Warehouses**: Kho hàng
- **StockTransactions**: Giao dịch kho

## 🚀 Deployment

### IIS Deployment
1. Publish project:
```bash
dotnet publish -c Release -o ./publish
```

2. Copy files to IIS directory
3. Configure connection string in web.config
4. Set up SSL certificate

### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WuanTech.API.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WuanTech.API.dll"]
```

## 🧪 Testing

```bash
# Unit tests
dotnet test

# Integration tests
dotnet test --filter Category=Integration

# Load testing với k6
k6 run load-test.js
```

## 📝 Contributing

1. Fork repository
2. Tạo feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Tạo Pull Request

## 🐛 Troubleshooting

### Connection String Issues
- Kiểm tra SQL Server đang chạy
- Đảm bảo user có quyền truy cập database
- Kiểm tra firewall settings

### JWT Issues
- Kiểm tra JWT key trong appsettings.json
- Đảm bảo system time đồng bộ
- Verify token expiration settings

### Email Issues
- Kiểm tra SMTP credentials
- Enable "Less secure app access" hoặc dùng App Password
- Kiểm tra firewall cho port 587

## 📞 Support

- Email: support@wuantech.com
- Documentation: [API Docs](https://api.wuantech.com/docs)
- Issues: [GitHub Issues](https://github.com/your-repo/issues)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- ASP.NET Core team
- Entity Framework Core team
- Community contributors
- Open source libraries used in this project

---

Made with ❤️ by WuanTech Team