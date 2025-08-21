-- ========================================
-- WUANTECH E-COMMERCE DATABASE SCHEMA
-- Cửa hàng đồ công nghệ WuanTech Store
-- Database: SQL Server
-- ========================================

-- Tạo database
CREATE DATABASE WuanTechDB;
GO

USE WuanTechDB;
GO

-- ========================================
-- 1. BẢNG USERS - Quản lý người dùng
-- ========================================
CREATE TABLE Users (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Username nvarchar(50) NOT NULL UNIQUE,
    Email nvarchar(100) NOT NULL UNIQUE,
    PasswordHash nvarchar(500) NOT NULL,
    FullName nvarchar(100) NULL,
    PhoneNumber nvarchar(20) NULL,
    Address nvarchar(500) NULL,
    Avatar nvarchar(500) NULL,
    Role nvarchar(20) NOT NULL DEFAULT 'Customer',
    IsActive bit NOT NULL DEFAULT 1,
    LastLogin datetime2 NULL,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Indexes
    INDEX IX_Users_Email (Email),
    INDEX IX_Users_Username (Username),
    INDEX IX_Users_Role (Role),
    INDEX IX_Users_IsActive (IsActive),
    
    -- Constraints
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'Staff', 'Customer')),
    CONSTRAINT CK_Users_Email_Format CHECK (Email LIKE '%@%.%')
);

-- ========================================
-- 2. BẢNG CUSTOMERS - Thông tin khách hàng
-- ========================================
CREATE TABLE Customers (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL UNIQUE,
    CustomerCode nvarchar(20) NOT NULL UNIQUE,
    DateOfBirth date NULL,
    Gender nvarchar(10) NULL,
    Points int NOT NULL DEFAULT 0,
    MembershipLevel nvarchar(20) NOT NULL DEFAULT 'Bronze',
    TotalPurchased decimal(18,2) NOT NULL DEFAULT 0,
    
    -- Foreign Key
    CONSTRAINT FK_Customers_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_Customers_UserId (UserId),
    INDEX IX_Customers_CustomerCode (CustomerCode),
    INDEX IX_Customers_MembershipLevel (MembershipLevel),
    
    -- Constraints
    CONSTRAINT CK_Customers_Gender CHECK (Gender IN ('Male', 'Female', 'Other')),
    CONSTRAINT CK_Customers_MembershipLevel CHECK (MembershipLevel IN ('Bronze', 'Silver', 'Gold', 'Platinum')),
    CONSTRAINT CK_Customers_Points CHECK (Points >= 0),
    CONSTRAINT CK_Customers_TotalPurchased CHECK (TotalPurchased >= 0)
);

-- ========================================
-- 3. BẢNG CATEGORIES - Danh mục sản phẩm
-- ========================================
CREATE TABLE Categories (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Icon nvarchar(50) NULL,
    Description nvarchar(500) NULL,
    ParentCategoryId int NULL,
    IsActive bit NOT NULL DEFAULT 1,
    DisplayOrder int NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_Categories_ParentCategory FOREIGN KEY (ParentCategoryId) REFERENCES Categories(Id),
    
    -- Indexes
    INDEX IX_Categories_ParentCategoryId (ParentCategoryId),
    INDEX IX_Categories_IsActive (IsActive),
    INDEX IX_Categories_DisplayOrder (DisplayOrder),
    INDEX IX_Categories_Name (Name)
);

-- ========================================
-- 4. BẢNG BRANDS - Thương hiệu
-- ========================================
CREATE TABLE Brands (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(100) NOT NULL UNIQUE,
    Logo nvarchar(500) NULL,
    Description nvarchar(500) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Indexes
    INDEX IX_Brands_Name (Name),
    INDEX IX_Brands_IsActive (IsActive)
);

-- ========================================
-- 5. BẢNG WAREHOUSES - Kho hàng
-- ========================================
CREATE TABLE Warehouses (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(20) NOT NULL UNIQUE,
    Name nvarchar(100) NOT NULL,
    Address nvarchar(500) NULL,
    Phone nvarchar(20) NULL,
    Manager nvarchar(100) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Indexes
    INDEX IX_Warehouses_Code (Code),
    INDEX IX_Warehouses_IsActive (IsActive)
);

-- ========================================
-- 6. BẢNG PRODUCTS - Sản phẩm
-- ========================================
CREATE TABLE Products (
    Id int IDENTITY(1,1) PRIMARY KEY,
    SKU nvarchar(50) NOT NULL UNIQUE,
    Name nvarchar(200) NOT NULL,
    Description ntext NULL,
    CategoryId int NOT NULL,
    BrandId int NULL,
    Price decimal(18,2) NOT NULL,
    DiscountPrice decimal(18,2) NULL,
    Cost decimal(18,2) NULL,
    ImageUrl nvarchar(500) NULL,
    Weight decimal(10,2) NULL,
    Dimensions nvarchar(100) NULL,
    Rating decimal(3,2) NOT NULL DEFAULT 0,
    ReviewCount int NOT NULL DEFAULT 0,
    IsFeatured bit NOT NULL DEFAULT 0,
    IsNew bit NOT NULL DEFAULT 0,
    IsActive bit NOT NULL DEFAULT 1,
    ViewCount int NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    CONSTRAINT FK_Products_Brands FOREIGN KEY (BrandId) REFERENCES Brands(Id) ON DELETE SET NULL,
    
    -- Indexes
    INDEX IX_Products_SKU (SKU),
    INDEX IX_Products_CategoryId (CategoryId),
    INDEX IX_Products_BrandId (BrandId),
    INDEX IX_Products_Price_DiscountPrice (Price, DiscountPrice),
    INDEX IX_Products_IsFeatured (IsFeatured),
    INDEX IX_Products_IsNew (IsNew),
    INDEX IX_Products_IsActive (IsActive),
    INDEX IX_Products_Rating (Rating),
    INDEX IX_Products_CreatedAt (CreatedAt),
    
    -- Constraints
    CONSTRAINT CK_Products_Price CHECK (Price > 0),
    CONSTRAINT CK_Products_DiscountPrice CHECK (DiscountPrice IS NULL OR DiscountPrice >= 0),
    CONSTRAINT CK_Products_Cost CHECK (Cost IS NULL OR Cost >= 0),
    CONSTRAINT CK_Products_Rating CHECK (Rating >= 0 AND Rating <= 5),
    CONSTRAINT CK_Products_ViewCount CHECK (ViewCount >= 0),
    CONSTRAINT CK_Products_ReviewCount CHECK (ReviewCount >= 0)
);

-- ========================================
-- 7. BẢNG PRODUCT_IMAGES - Hình ảnh sản phẩm
-- ========================================
CREATE TABLE ProductImages (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ProductId int NOT NULL,
    ImageUrl nvarchar(500) NOT NULL,
    IsMain bit NOT NULL DEFAULT 0,
    DisplayOrder int NOT NULL DEFAULT 0,
    
    -- Foreign Key
    CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_ProductImages_ProductId (ProductId),
    INDEX IX_ProductImages_ProductId_IsMain (ProductId, IsMain),
    INDEX IX_ProductImages_DisplayOrder (DisplayOrder)
);

-- ========================================
-- 8. BẢNG PRODUCT_SPECIFICATIONS - Thông số kỹ thuật
-- ========================================
CREATE TABLE ProductSpecifications (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ProductId int NOT NULL,
    SpecKey nvarchar(100) NOT NULL,
    SpecValue nvarchar(500) NOT NULL,
    DisplayOrder int NOT NULL DEFAULT 0,
    
    -- Foreign Key
    CONSTRAINT FK_ProductSpecifications_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_ProductSpecifications_ProductId (ProductId),
    INDEX IX_ProductSpecifications_DisplayOrder (DisplayOrder)
);

-- ========================================
-- 9. BẢNG INVENTORIES - Quản lý tồn kho
-- ========================================
CREATE TABLE Inventories (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ProductId int NOT NULL,
    WarehouseId int NOT NULL,
    Quantity int NOT NULL DEFAULT 0,
    MinStock int NOT NULL DEFAULT 10,
    MaxStock int NOT NULL DEFAULT 1000,
    LastUpdated datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_Inventories_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Inventories_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses(Id),
    
    -- Indexes
    INDEX IX_Inventories_ProductId_WarehouseId UNIQUE (ProductId, WarehouseId),
    INDEX IX_Inventories_Quantity (Quantity),
    INDEX IX_Inventories_LastUpdated (LastUpdated),
    
    -- Constraints
    CONSTRAINT CK_Inventories_Quantity CHECK (Quantity >= 0),
    CONSTRAINT CK_Inventories_MinStock CHECK (MinStock >= 0),
    CONSTRAINT CK_Inventories_MaxStock CHECK (MaxStock > MinStock)
);

-- ========================================
-- 10. BẢNG CART_ITEMS - Giỏ hàng
-- ========================================
CREATE TABLE CartItems (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL,
    ProductId int NOT NULL,
    Quantity int NOT NULL DEFAULT 1,
    AddedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_CartItems_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_CartItems_UserId_ProductId UNIQUE (UserId, ProductId),
    INDEX IX_CartItems_UserId (UserId),
    INDEX IX_CartItems_ProductId (ProductId),
    INDEX IX_CartItems_AddedAt (AddedAt),
    
    -- Constraints
    CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0)
);

-- ========================================
-- 11. BẢNG ORDERS - Đơn hàng
-- ========================================
CREATE TABLE Orders (
    Id int IDENTITY(1,1) PRIMARY KEY,
    OrderNumber nvarchar(50) NOT NULL UNIQUE,
    CustomerId int NOT NULL,
    OrderDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
    Status nvarchar(20) NOT NULL DEFAULT 'PENDING',
    SubTotal decimal(18,2) NOT NULL,
    ShippingFee decimal(18,2) NOT NULL DEFAULT 0,
    Discount decimal(18,2) NOT NULL DEFAULT 0,
    Tax decimal(18,2) NOT NULL DEFAULT 0,
    TotalAmount decimal(18,2) NOT NULL,
    PaymentMethod nvarchar(50) NULL,
    PaymentStatus nvarchar(20) NOT NULL DEFAULT 'PENDING',
    ShippingAddress nvarchar(500) NULL,
    ShippingPhone nvarchar(20) NULL,
    TrackingNumber nvarchar(100) NULL,
    Notes nvarchar(500) NULL,
    ShippedDate datetime2 NULL,
    DeliveredDate datetime2 NULL,
    CancelledDate datetime2 NULL,
    CancelReason nvarchar(500) NULL,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    
    -- Indexes
    INDEX IX_Orders_OrderNumber (OrderNumber),
    INDEX IX_Orders_CustomerId (CustomerId),
    INDEX IX_Orders_OrderDate (OrderDate),
    INDEX IX_Orders_Status (Status),
    INDEX IX_Orders_PaymentStatus (PaymentStatus),
    INDEX IX_Orders_TrackingNumber (TrackingNumber),
    
    -- Constraints
    CONSTRAINT CK_Orders_Status CHECK (Status IN ('PENDING', 'CONFIRMED', 'PROCESSING', 'SHIPPED', 'DELIVERED', 'CANCELLED')),
    CONSTRAINT CK_Orders_PaymentStatus CHECK (PaymentStatus IN ('PENDING', 'PAID', 'FAILED', 'REFUNDED')),
    CONSTRAINT CK_Orders_SubTotal CHECK (SubTotal >= 0),
    CONSTRAINT CK_Orders_ShippingFee CHECK (ShippingFee >= 0),
    CONSTRAINT CK_Orders_Discount CHECK (Discount >= 0),
    CONSTRAINT CK_Orders_Tax CHECK (Tax >= 0),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0)
);

-- ========================================
-- 12. BẢNG ORDER_ITEMS - Chi tiết đơn hàng
-- ========================================
CREATE TABLE OrderItems (
    Id int IDENTITY(1,1) PRIMARY KEY,
    OrderId int NOT NULL,
    ProductId int NOT NULL,
    Quantity int NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    DiscountAmount decimal(18,2) NOT NULL DEFAULT 0,
    TotalPrice decimal(18,2) NOT NULL,
    
    -- Foreign Keys
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id),
    
    -- Indexes
    INDEX IX_OrderItems_OrderId (OrderId),
    INDEX IX_OrderItems_ProductId (ProductId),
    
    -- Constraints
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_OrderItems_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT CK_OrderItems_TotalPrice CHECK (TotalPrice >= 0)
);

-- ========================================
-- 13. BẢNG REVIEWS - Đánh giá sản phẩm
-- ========================================
CREATE TABLE Reviews (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ProductId int NOT NULL,
    UserId int NOT NULL,
    OrderId int NULL,
    Rating int NOT NULL,
    Comment ntext NULL,
    IsVerifiedPurchase bit NOT NULL DEFAULT 0,
    IsApproved bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_Reviews_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Reviews_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE SET NULL,
    
    -- Indexes
    INDEX IX_Reviews_ProductId (ProductId),
    INDEX IX_Reviews_UserId (UserId),
    INDEX IX_Reviews_Rating (Rating),
    INDEX IX_Reviews_IsApproved (IsApproved),
    INDEX IX_Reviews_CreatedAt (CreatedAt),
    INDEX IX_Reviews_ProductId_UserId UNIQUE (ProductId, UserId),
    
    -- Constraints
    CONSTRAINT CK_Reviews_Rating CHECK (Rating >= 1 AND Rating <= 5)
);

-- ========================================
-- 14. BẢNG FAVORITES - Sản phẩm yêu thích
-- ========================================
CREATE TABLE Favorites (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL,
    ProductId int NOT NULL,
    AddedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Favorites_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_Favorites_UserId_ProductId UNIQUE (UserId, ProductId),
    INDEX IX_Favorites_UserId (UserId),
    INDEX IX_Favorites_ProductId (ProductId),
    INDEX IX_Favorites_AddedAt (AddedAt)
);

-- ========================================
-- 15. BẢNG COUPONS - Mã giảm giá
-- ========================================
CREATE TABLE Coupons (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(50) NOT NULL UNIQUE,
    Description nvarchar(500) NULL,
    DiscountType nvarchar(20) NOT NULL,
    DiscountValue decimal(18,2) NOT NULL,
    MinOrderAmount decimal(18,2) NOT NULL DEFAULT 0,
    MaxDiscountAmount decimal(18,2) NULL,
    UsageLimit int NULL,
    UsedCount int NOT NULL DEFAULT 0,
    StartDate datetime2 NULL,
    EndDate datetime2 NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Indexes
    INDEX IX_Coupons_Code (Code),
    INDEX IX_Coupons_IsActive (IsActive),
    INDEX IX_Coupons_StartDate_EndDate (StartDate, EndDate),
    INDEX IX_Coupons_DiscountType (DiscountType),
    
    -- Constraints
    CONSTRAINT CK_Coupons_DiscountType CHECK (DiscountType IN ('PERCENTAGE', 'FIXED')),
    CONSTRAINT CK_Coupons_DiscountValue CHECK (DiscountValue > 0),
    CONSTRAINT CK_Coupons_MinOrderAmount CHECK (MinOrderAmount >= 0),
    CONSTRAINT CK_Coupons_MaxDiscountAmount CHECK (MaxDiscountAmount IS NULL OR MaxDiscountAmount > 0),
    CONSTRAINT CK_Coupons_UsageLimit CHECK (UsageLimit IS NULL OR UsageLimit > 0),
    CONSTRAINT CK_Coupons_UsedCount CHECK (UsedCount >= 0)
);

-- ========================================
-- 16. BẢNG TODOS - Công việc (Task Management)
-- ========================================
CREATE TABLE Todos (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL,
    Text nvarchar(500) NOT NULL,
    IsCompleted bit NOT NULL DEFAULT 0,
    Priority nvarchar(20) NOT NULL DEFAULT 'Medium',
    DueDate datetime2 NULL,
    CompletedAt datetime2 NULL,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_Todos_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_Todos_UserId (UserId),
    INDEX IX_Todos_IsCompleted (IsCompleted),
    INDEX IX_Todos_Priority (Priority),
    INDEX IX_Todos_DueDate (DueDate),
    INDEX IX_Todos_CreatedAt (CreatedAt),
    
    -- Constraints
    CONSTRAINT CK_Todos_Priority CHECK (Priority IN ('Low', 'Medium', 'High'))
);

-- ========================================
-- 17. BẢNG REFRESH_TOKENS - Token làm mới
-- ========================================
CREATE TABLE RefreshTokens (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL,
    Token nvarchar(500) NOT NULL UNIQUE,
    ExpiresAt datetime2 NOT NULL,
    IsRevoked bit NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedByIp nvarchar(50) NULL,
    RevokedAt datetime2 NULL,
    RevokedByIp nvarchar(50) NULL,
    ReplacedByToken nvarchar(500) NULL,
    
    -- Foreign Key
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_RefreshTokens_Token (Token),
    INDEX IX_RefreshTokens_UserId (UserId),
    INDEX IX_RefreshTokens_ExpiresAt (ExpiresAt),
    INDEX IX_RefreshTokens_IsRevoked (IsRevoked)
);

-- ========================================
-- 18. BẢNG PASSWORD_RESET_TOKENS - Token reset mật khẩu
-- ========================================
CREATE TABLE PasswordResetTokens (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL,
    Token nvarchar(500) NOT NULL UNIQUE,
    ExpiresAt datetime2 NOT NULL,
    IsUsed bit NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_PasswordResetTokens_Token (Token),
    INDEX IX_PasswordResetTokens_UserId (UserId),
    INDEX IX_PasswordResetTokens_ExpiresAt (ExpiresAt),
    INDEX IX_PasswordResetTokens_IsUsed (IsUsed)
);

-- ========================================
-- 19. BẢNG SEARCH_QUERIES - Lịch sử tìm kiếm
-- ========================================
CREATE TABLE SearchQueries (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Query nvarchar(500) NOT NULL,
    UserId int NULL,
    SearchDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
    ResultCount int NOT NULL DEFAULT 0,
    IpAddress nvarchar(45) NULL,
    UserAgent nvarchar(500) NULL,
    
    -- Foreign Key
    CONSTRAINT FK_SearchQueries_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
    
    -- Indexes
    INDEX IX_SearchQueries_Query (Query),
    INDEX IX_SearchQueries_SearchDate (SearchDate),
    INDEX IX_SearchQueries_UserId (UserId),
    INDEX IX_SearchQueries_IpAddress (IpAddress)
);

-- ========================================
-- 20. TRIGGERS - Tự động cập nhật thời gian
-- ========================================

-- Trigger cho Products
CREATE TRIGGER TR_Products_UpdatedAt
ON Products
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Products 
    SET UpdatedAt = GETUTCDATE()
    WHERE Id IN (SELECT Id FROM inserted);
END;
GO

-- Trigger cho Categories
CREATE TRIGGER TR_Categories_UpdatedAt
ON Categories
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Categories 
    SET UpdatedAt = GETUTCDATE()
    WHERE Id IN (SELECT Id FROM inserted);
END;
GO

-- Trigger cho Orders
CREATE TRIGGER TR_Orders_UpdatedAt
ON Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Orders 
    SET UpdatedAt = GETUTCDATE()
    WHERE Id IN (SELECT Id FROM inserted);
END;
GO

-- Trigger cho CartItems
CREATE TRIGGER TR_CartItems_UpdatedAt
ON CartItems
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CartItems 
    SET UpdatedAt = GETUTCDATE()
    WHERE Id IN (SELECT Id FROM inserted);
END;
GO

-- ========================================
-- 21. SAMPLE DATA - Dữ liệu mẫu
-- ========================================

-- Insert sample users
INSERT INTO Users (Username, Email, PasswordHash, FullName, PhoneNumber, Role) VALUES
('admin', 'admin@wuantech.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/NF/rZ5DYkKlPDZKKO', N'Administrator', '0901234567', 'Admin'),
('staff1', 'staff@wuantech.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/NF/rZ5DYkKlPDZKKO', N'Nhân viên 1', '0901234568', 'Staff'),
('customer1', 'customer@gmail.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/NF/rZ5DYkKlPDZKKO', N'Khách hàng 1', '0901234569', 'Customer');

-- Insert sample customers
INSERT INTO Customers (UserId, CustomerCode, Points, MembershipLevel) VALUES
(3, 'KH202401001', 0, 'Bronze');

-- Insert sample brands
INSERT INTO Brands (Name, Description) VALUES
('Apple', N'Thương hiệu công nghệ hàng đầu thế giới'),
('Samsung', N'Tập đoàn công nghệ đa quốc gia Hàn Quốc'),
('ASUS', N'Thương hiệu máy tính và linh kiện hàng đầu'),
('Dell', N'Thương hiệu máy tính và server nổi tiếng'),
('HP', N'Hewlett-Packard - Thương hiệu máy tính lâu đời'),
('Logitech', N'Thương hiệu phụ kiện máy tính hàng đầu'),
('Sony', N'Tập đoàn giải trí và công nghệ Nhật Bản'),
('LG', N'Thương hiệu điện tử tiêu dùng Hàn Quốc');

-- Insert sample categories
INSERT INTO Categories (Name, Icon, Description, DisplayOrder) VALUES
(N'Điện thoại', 'smartphone', N'Điện thoại thông minh các loại', 1),
(N'Laptop', 'laptop', N'Máy tính xách tay', 2),
(N'PC & Linh kiện', 'desktop', N'Máy tính để bàn và linh kiện', 3),
(N'Phụ kiện', 'accessories', N'Phụ kiện điện tử', 4),
(N'Âm thanh', 'headphones', N'Thiết bị âm thanh', 5),
(N'Gaming', 'gamepad', N'Thiết bị gaming', 6);

-- Insert subcategories
INSERT INTO Categories (Name, Icon, Description, ParentCategoryId, DisplayOrder) VALUES
(N'iPhone', NULL, N'Điện thoại iPhone', 1, 1),
(N'Samsung Galaxy', NULL, N'Điện thoại Samsung', 1, 2),
(N'Laptop Gaming', NULL, N'Laptop chơi game', 2, 1),
(N'Laptop Văn phòng', NULL, N'Laptop văn phòng', 2, 2),
(N'CPU', NULL, N'Bộ vi xử lý', 3, 1),
(N'VGA', NULL, N'Card đồ họa', 3, 2),
(N'RAM', NULL, N'Bộ nhớ trong', 3, 3),
(N'Ổ cứng', NULL, N'Ổ cứng SSD/HDD', 3, 4);

-- Insert sample warehouses
INSERT INTO Warehouses (Code, Name, Address, Manager) VALUES
('WH001', N'Kho Hồ Chí Minh', N'123 Nguyễn Văn Cừ, Q.5, TP.HCM', N'Nguyễn Văn Kha'),
('WH002', N'Kho Hà Nội', N'456 Giải Phóng, Hai Bà Trưng, Hà Nội', N'Trần Thị Bình'),
('WH003', N'Kho Đà Nẵng', N'789 Nguyễn Văn Linh, Hải Châu, Đà Nẵng', N'Lê Văn Quới');

-- Insert sample products
INSERT INTO Products (SKU, Name, Description, CategoryId, BrandId, Price, DiscountPrice, ImageUrl, IsFeatured, IsNew) VALUES
('IP15PM256', N'iPhone 15 Pro Max 256GB', N'iPhone 15 Pro Max với chip A17 Pro mạnh mẽ', 7, 1, 34990000, 33990000, '/images/iphone-15-pro-max.jpg', 1, 1),
('SS-S24U512', N'Samsung Galaxy S24 Ultra 512GB', N'Galaxy S24 Ultra với S Pen tích hợp', 8, 2, 31990000, 30000000, '/images/galaxy-s24-ultra.jpg', 1, 1),
('AS-ROG-G16', N'ASUS ROG Strix G16', N'Laptop gaming với RTX 4060', 9, 3, 28990000, 27490000, '/images/asus-rog-g16.jpg', 1, 0),
('DL-XPS13', N'Dell XPS 13', N'Laptop văn phòng cao cấp', 10, 4, 25990000, 24000000, '/images/dell-xps-13.jpg', 0, 0),
('LG-MX-M2', N'Logitech MX Master 2S', N'Chuột không dây cao cấp', 4, 6, 1990000, 1790000, '/images/mx-master-2s.jpg', 0, 0);

-- Insert sample product images
INSERT INTO ProductImages (ProductId, ImageUrl, IsMain, DisplayOrder) VALUES
(1, '/images/iphone-15-pro-max-1.jpg', 1, 0),
(1, '/images/iphone-15-pro-max-2.jpg', 0, 1),
(1, '/images/iphone-15-pro-max-3.jpg', 0, 2),
(2, '/images/galaxy-s24-ultra-1.jpg', 1, 0),
(2, '/images/galaxy-s24-ultra-2.jpg', 0, 1),
(3, '/images/asus-rog-g16-1.jpg', 1, 0),
(3, '/images/asus-rog-g16-2.jpg', 0, 1),
(4, '/images/dell-xps-13-1.jpg', 1, 0),
(5, '/images/mx-master-2s-1.jpg', 1, 0);

-- Insert sample product specifications
INSERT INTO ProductSpecifications (ProductId, SpecKey, SpecValue, DisplayOrder) VALUES
-- iPhone 15 Pro Max specs
(1, N'Màn hình', N'6.7" Super Retina XDR OLED', 1),
(1, N'Chip xử lý', N'A17 Pro', 2),
(1, N'RAM', N'8GB', 3),
(1, N'Bộ nhớ trong', N'256GB', 4),
(1, N'Camera chính', N'48MP + 12MP + 12MP', 5),
(1, N'Pin', N'4441mAh', 6),
(1, N'Hệ điều hành', N'iOS 17', 7),

-- Galaxy S24 Ultra specs
(2, N'Màn hình', N'6.8" Dynamic AMOLED 2X', 1),
(2, N'Chip xử lý', N'Snapdragon 8 Gen 3', 2),
(2, N'RAM', N'12GB', 3),
(2, N'Bộ nhớ trong', N'512GB', 4),
(2, N'Camera chính', N'200MP + 50MP + 12MP + 10MP', 5),
(2, N'Pin', N'5000mAh', 6),
(2, N'Hệ điều hành', N'Android 14, One UI 6.1', 7),
(2, N'S Pen', N'Có', 8),

-- ASUS ROG G16 specs
(3, N'Màn hình', N'16" QHD+ 165Hz', 1),
(3, N'CPU', N'Intel Core i7-13650HX', 2),
(3, N'RAM', N'16GB DDR5', 3),
(3, N'GPU', N'NVIDIA RTX 4060 8GB', 4),
(3, N'Ổ cứng', N'512GB PCIe SSD', 5),
(3, N'Hệ điều hành', N'Windows 11 Home', 6),
(3, N'Bàn phím', N'RGB Per-key', 7),

-- Dell XPS 13 specs
(4, N'Màn hình', N'13.4" FHD+ InfinityEdge', 1),
(4, N'CPU', N'Intel Core i5-1340P', 2),
(4, N'RAM', N'8GB LPDDR5', 3),
(4, N'Ổ cứng', N'256GB PCIe SSD', 4),
(4, N'Trọng lượng', N'1.23kg', 5),
(4, N'Hệ điều hành', N'Windows 11 Home', 6),

-- Logitech MX Master 2S specs
(5, N'Kết nối', N'Bluetooth, USB Unifying', 1),
(5, N'Pin', N'Lên đến 70 ngày', 2),
(5, N'DPI', N'200-4000 DPI', 3),
(5, N'Nút bấm', N'7 nút', 4),
(5, N'Tương thích', N'Windows, macOS, Linux', 5);

-- Insert sample inventories
INSERT INTO Inventories (ProductId, WarehouseId, Quantity, MinStock, MaxStock) VALUES
-- iPhone 15 Pro Max inventory
(1, 1, 50, 10, 200),
(1, 2, 30, 10, 200),
(1, 3, 20, 5, 100),

-- Galaxy S24 Ultra inventory
(2, 1, 45, 10, 200),
(2, 2, 35, 10, 200),

-- ASUS ROG G16 inventory
(3, 1, 25, 5, 100),
(3, 2, 15, 5, 100),

-- Dell XPS 13 inventory
(4, 1, 30, 5, 150),
(4, 2, 20, 5, 150),

-- Logitech MX Master 2S inventory
(5, 1, 100, 20, 500),
(5, 2, 80, 20, 500),
(5, 3, 50, 10, 300);

-- Insert sample coupons
INSERT INTO Coupons (Code, Description, DiscountType, DiscountValue, MinOrderAmount, MaxDiscountAmount, UsageLimit, StartDate, EndDate) VALUES
('WELCOME10', N'Giảm 10% cho khách hàng mới', 'PERCENTAGE', 10, 1000000, 500000, 100, GETUTCDATE(), DATEADD(MONTH, 3, GETUTCDATE())),
('NEWBIE500', N'Giảm 500K cho đơn hàng đầu tiên', 'FIXED', 500000, 5000000, NULL, 50, GETUTCDATE(), DATEADD(MONTH, 1, GETUTCDATE())),
('SUMMER2024', N'Khuyến mãi hè 2024', 'PERCENTAGE', 15, 2000000, 1000000, 200, GETUTCDATE(), DATEADD(MONTH, 2, GETUTCDATE())),
('FREESHIP', N'Miễn phí vận chuyển', 'FIXED', 30000, 500000, NULL, 1000, GETUTCDATE(), DATEADD(MONTH, 6, GETUTCDATE()));

-- ========================================
-- 22. STORED PROCEDURES - Thủ tục lưu trữ
-- ========================================

-- Procedure: Lấy thống kê dashboard
CREATE PROCEDURE SP_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TotalRevenue DECIMAL(18,2) = 0;
    DECLARE @TotalOrders INT = 0;
    DECLARE @TotalProducts INT = 0;
    DECLARE @TotalCustomers INT = 0;
    DECLARE @PendingOrders INT = 0;
    DECLARE @LowStockProducts INT = 0;
    
    -- Tính tổng doanh thu (chỉ đơn hàng đã giao)
    SELECT @TotalRevenue = ISNULL(SUM(TotalAmount), 0)
    FROM Orders 
    WHERE Status = 'DELIVERED';
    
    -- Tổng số đơn hàng
    SELECT @TotalOrders = COUNT(*)
    FROM Orders;
    
    -- Tổng số sản phẩm active
    SELECT @TotalProducts = COUNT(*)
    FROM Products 
    WHERE IsActive = 1;
    
    -- Tổng số khách hàng
    SELECT @TotalCustomers = COUNT(*)
    FROM Users 
    WHERE Role = 'Customer';
    
    -- Đơn hàng chờ xử lý
    SELECT @PendingOrders = COUNT(*)
    FROM Orders 
    WHERE Status IN ('PENDING', 'CONFIRMED');
    
    -- Sản phẩm sắp hết hàng
    SELECT @LowStockProducts = COUNT(DISTINCT ProductId)
    FROM Inventories 
    WHERE Quantity <= MinStock;
    
    -- Trả về kết quả
    SELECT 
        @TotalRevenue AS TotalRevenue,
        @TotalOrders AS TotalOrders,
        @TotalProducts AS TotalProducts,
        @TotalCustomers AS TotalCustomers,
        @PendingOrders AS PendingOrders,
        @LowStockProducts AS LowStockProducts;
END;
GO

-- Procedure: Cập nhật rating sản phẩm
CREATE PROCEDURE SP_UpdateProductRating
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AvgRating DECIMAL(3,2);
    DECLARE @ReviewCount INT;
    
    SELECT 
        @AvgRating = ISNULL(AVG(CAST(Rating AS DECIMAL(3,2))), 0),
        @ReviewCount = COUNT(*)
    FROM Reviews 
    WHERE ProductId = @ProductId AND IsApproved = 1;
    
    UPDATE Products 
    SET 
        Rating = @AvgRating,
        ReviewCount = @ReviewCount,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @ProductId;
END;
GO

-- Procedure: Tính tổng giỏ hàng
CREATE PROCEDURE SP_CalculateCartTotal
    @UserId INT,
    @SubTotal DECIMAL(18,2) OUTPUT,
    @ShippingFee DECIMAL(18,2) OUTPUT,
    @Tax DECIMAL(18,2) OUTPUT,
    @Total DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Tính subtotal
    SELECT @SubTotal = ISNULL(SUM(
        ci.Quantity * ISNULL(p.DiscountPrice, p.Price)
    ), 0)
    FROM CartItems ci
    INNER JOIN Products p ON ci.ProductId = p.Id
    WHERE ci.UserId = @UserId AND p.IsActive = 1;
    
    -- Tính phí vận chuyển (miễn phí nếu > 500K)
    SET @ShippingFee = CASE 
        WHEN @SubTotal >= 500000 THEN 0 
        ELSE 30000 
    END;
    
    -- Tính thuế (10%)
    SET @Tax = @SubTotal * 0.1;
    
    -- Tính tổng
    SET @Total = @SubTotal + @ShippingFee + @Tax;
END;
GO

-- Procedure: Kiểm tra tồn kho
CREATE PROCEDURE SP_CheckProductStock
    @ProductId INT,
    @Quantity INT,
    @IsAvailable BIT OUTPUT,
    @AvailableStock INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT @AvailableStock = ISNULL(SUM(Quantity), 0)
    FROM Inventories 
    WHERE ProductId = @ProductId;
    
    SET @IsAvailable = CASE 
        WHEN @AvailableStock >= @Quantity THEN 1 
        ELSE 0 
    END;
END;
GO

-- Procedure: Lấy sản phẩm bán chạy
CREATE PROCEDURE SP_GetTopSellingProducts
    @TopCount INT = 10,
    @DateFrom DATETIME2 = NULL,
    @DateTo DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @DateFrom IS NULL SET @DateFrom = DATEADD(MONTH, -3, GETUTCDATE());
    IF @DateTo IS NULL SET @DateTo = GETUTCDATE();
    
    SELECT TOP (@TopCount)
        p.Id AS ProductId,
        p.SKU,
        p.Name AS ProductName,
        p.Price,
        p.DiscountPrice,
        p.ImageUrl,
        b.Name AS BrandName,
        c.Name AS CategoryName,
        SUM(oi.Quantity) AS TotalSold,
        SUM(oi.TotalPrice) AS TotalRevenue,
        AVG(oi.UnitPrice) AS AvgPrice
    FROM Products p
    INNER JOIN OrderItems oi ON p.Id = oi.ProductId
    INNER JOIN Orders o ON oi.OrderId = o.Id
    LEFT JOIN Brands b ON p.BrandId = b.Id
    LEFT JOIN Categories c ON p.CategoryId = c.Id
    WHERE o.Status = 'DELIVERED' 
        AND o.OrderDate BETWEEN @DateFrom AND @DateTo
        AND p.IsActive = 1
    GROUP BY p.Id, p.SKU, p.Name, p.Price, p.DiscountPrice, p.ImageUrl, b.Name, c.Name
    ORDER BY TotalSold DESC, TotalRevenue DESC;
END;
GO

-- Procedure: Lấy khách hàng VIP
CREATE PROCEDURE SP_GetTopCustomers
    @TopCount INT = 10,
    @DateFrom DATETIME2 = NULL,
    @DateTo DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @DateFrom IS NULL SET @DateFrom = DATEADD(YEAR, -1, GETUTCDATE());
    IF @DateTo IS NULL SET @DateTo = GETUTCDATE();
    
    SELECT TOP (@TopCount)
        c.Id AS CustomerId,
        c.CustomerCode,
        u.FullName,
        u.Email,
        u.PhoneNumber,
        c.MembershipLevel,
        c.Points,
        c.TotalPurchased,
        COUNT(o.Id) AS TotalOrders,
        SUM(o.TotalAmount) AS PeriodRevenue,
        MAX(o.OrderDate) AS LastOrderDate
    FROM Customers c
    INNER JOIN Users u ON c.UserId = u.Id
    LEFT JOIN Orders o ON c.Id = o.CustomerId 
        AND o.Status = 'DELIVERED'
        AND o.OrderDate BETWEEN @DateFrom AND @DateTo
    WHERE u.IsActive = 1
    GROUP BY c.Id, c.CustomerCode, u.FullName, u.Email, u.PhoneNumber, 
             c.MembershipLevel, c.Points, c.TotalPurchased
    ORDER BY c.TotalPurchased DESC, COUNT(o.Id) DESC;
END;
GO

-- Procedure: Báo cáo doanh thu theo tháng
CREATE PROCEDURE SP_GetMonthlyRevenue
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH MonthRange AS (
        SELECT 1 AS Month UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 
        UNION SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 
        UNION SELECT 9 UNION SELECT 10 UNION SELECT 11 UNION SELECT 12
    )
    SELECT 
        mr.Month,
        @Year AS Year,
        DATENAME(MONTH, DATEFROMPARTS(@Year, mr.Month, 1)) AS MonthName,
        ISNULL(SUM(o.TotalAmount), 0) AS Revenue,
        COUNT(o.Id) AS OrderCount,
        ISNULL(AVG(o.TotalAmount), 0) AS AvgOrderValue
    FROM MonthRange mr
    LEFT JOIN Orders o ON YEAR(o.OrderDate) = @Year 
        AND MONTH(o.OrderDate) = mr.Month
        AND o.Status = 'DELIVERED'
    GROUP BY mr.Month
    ORDER BY mr.Month;
END;
GO

-- ========================================
-- 23. VIEWS - Khung nhìn
-- ========================================

-- View: Thông tin sản phẩm chi tiết
CREATE VIEW VW_ProductDetails AS
SELECT 
    p.Id,
    p.SKU,
    p.Name,
    p.Description,
    p.Price,
    p.DiscountPrice,
    ISNULL(p.DiscountPrice, p.Price) AS FinalPrice,
    CASE 
        WHEN p.DiscountPrice IS NOT NULL AND p.DiscountPrice < p.Price 
        THEN ROUND((p.Price - p.DiscountPrice) / p.Price * 100, 2)
        ELSE 0 
    END AS DiscountPercentage,
    p.ImageUrl,
    p.Weight,
    p.Dimensions,
    p.Rating,
    p.ReviewCount,
    p.IsFeatured,
    p.IsNew,
    p.IsActive,
    p.ViewCount,
    p.CreatedAt,
    p.UpdatedAt,
    c.Name AS CategoryName,
    c.Icon AS CategoryIcon,
    b.Name AS BrandName,
    b.Logo AS BrandLogo,
    ISNULL(SUM(i.Quantity), 0) AS TotalStock,
    CASE 
        WHEN ISNULL(SUM(i.Quantity), 0) > 0 THEN 1 
        ELSE 0 
    END AS InStock
FROM Products p
LEFT JOIN Categories c ON p.CategoryId = c.Id
LEFT JOIN Brands b ON p.BrandId = b.Id
LEFT JOIN Inventories i ON p.Id = i.ProductId
GROUP BY 
    p.Id, p.SKU, p.Name, p.Description, p.Price, p.DiscountPrice, 
    p.ImageUrl, p.Weight, p.Dimensions, p.Rating, p.ReviewCount,
    p.IsFeatured, p.IsNew, p.IsActive, p.ViewCount, p.CreatedAt, p.UpdatedAt,
    c.Name, c.Icon, b.Name, b.Logo;
GO

-- View: Thông tin đơn hàng chi tiết
CREATE VIEW VW_OrderDetails AS
SELECT 
    o.Id,
    o.OrderNumber,
    o.OrderDate,
    o.Status,
    o.PaymentStatus,
    o.PaymentMethod,
    o.SubTotal,
    o.ShippingFee,
    o.Discount,
    o.Tax,
    o.TotalAmount,
    o.ShippingAddress,
    o.TrackingNumber,
    o.ShippedDate,
    o.DeliveredDate,
    c.CustomerCode,
    u.FullName AS CustomerName,
    u.Email AS CustomerEmail,
    u.PhoneNumber AS CustomerPhone,
    COUNT(oi.Id) AS ItemCount,
    SUM(oi.Quantity) AS TotalQuantity
FROM Orders o
INNER JOIN Customers c ON o.CustomerId = c.Id
INNER JOIN Users u ON c.UserId = u.Id
LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
GROUP BY 
    o.Id, o.OrderNumber, o.OrderDate, o.Status, o.PaymentStatus, o.PaymentMethod,
    o.SubTotal, o.ShippingFee, o.Discount, o.Tax, o.TotalAmount, 
    o.ShippingAddress, o.TrackingNumber, o.ShippedDate, o.DeliveredDate,
    c.CustomerCode, u.FullName, u.Email, u.PhoneNumber;
GO

-- View: Thống kê tồn kho
CREATE VIEW VW_InventoryStatus AS
SELECT 
    p.Id AS ProductId,
    p.SKU,
    p.Name AS ProductName,
    p.Price,
    c.Name AS CategoryName,
    b.Name AS BrandName,
    w.Code AS WarehouseCode,
    w.Name AS WarehouseName,
    i.Quantity,
    i.MinStock,
    i.MaxStock,
    i.LastUpdated,
    CASE 
        WHEN i.Quantity = 0 THEN N'Hết hàng'
        WHEN i.Quantity <= i.MinStock THEN N'Sắp hết'
        WHEN i.Quantity >= i.MaxStock THEN N'Thừa kho'
        ELSE N'Bình thường'
    END AS StockStatus,
    CASE 
        WHEN i.Quantity = 0 THEN 'OUT_OF_STOCK'
        WHEN i.Quantity <= i.MinStock THEN 'LOW_STOCK'
        WHEN i.Quantity >= i.MaxStock THEN 'OVER_STOCK'
        ELSE 'NORMAL'
    END AS StockStatusCode
FROM Inventories i
INNER JOIN Products p ON i.ProductId = p.Id
INNER JOIN Warehouses w ON i.WarehouseId = w.Id
LEFT JOIN Categories c ON p.CategoryId = c.Id
LEFT JOIN Brands b ON p.BrandId = b.Id
WHERE p.IsActive = 1 AND w.IsActive = 1;
GO

-- View: Top sản phẩm bán chạy
CREATE VIEW VW_TopSellingProducts AS
SELECT 
    p.Id AS ProductId,
    p.SKU,
    p.Name AS ProductName,
    p.Price,
    p.DiscountPrice,
    p.ImageUrl,
    c.Name AS CategoryName,
    b.Name AS BrandName,
    SUM(oi.Quantity) AS TotalSold,
    SUM(oi.TotalPrice) AS TotalRevenue,
    COUNT(DISTINCT oi.OrderId) AS OrderCount,
    AVG(oi.UnitPrice) AS AvgSellPrice
FROM Products p
INNER JOIN OrderItems oi ON p.Id = oi.ProductId
INNER JOIN Orders o ON oi.OrderId = o.Id
LEFT JOIN Categories c ON p.CategoryId = c.Id
LEFT JOIN Brands b ON p.BrandId = b.Id
WHERE o.Status = 'DELIVERED' 
    AND p.IsActive = 1
    AND o.OrderDate >= DATEADD(MONTH, -3, GETUTCDATE())
GROUP BY 
    p.Id, p.SKU, p.Name, p.Price, p.DiscountPrice, p.ImageUrl,
    c.Name, b.Name;
GO

-- ========================================
-- 24. FUNCTIONS - Hàm tiện ích
-- ========================================

-- Function: Tính discount percentage
CREATE FUNCTION FN_CalculateDiscountPercentage(
    @OriginalPrice DECIMAL(18,2),
    @DiscountPrice DECIMAL(18,2)
)
RETURNS DECIMAL(5,2)
AS
BEGIN
    DECLARE @Result DECIMAL(5,2);
    
    IF @DiscountPrice IS NULL OR @DiscountPrice >= @OriginalPrice OR @OriginalPrice = 0
        SET @Result = 0;
    ELSE
        SET @Result = ROUND((@OriginalPrice - @DiscountPrice) / @OriginalPrice * 100, 2);
    
    RETURN @Result;
END;
GO

-- Function: Lấy tổng tồn kho của sản phẩm
CREATE FUNCTION FN_GetProductTotalStock(@ProductId INT)
RETURNS INT
AS
BEGIN
    DECLARE @TotalStock INT;
    
    SELECT @TotalStock = ISNULL(SUM(Quantity), 0)
    FROM Inventories 
    WHERE ProductId = @ProductId;
    
    RETURN @TotalStock;
END;
GO

-- Function: Kiểm tra khách hàng VIP
CREATE FUNCTION FN_IsVIPCustomer(@CustomerId INT)
RETURNS BIT
AS
BEGIN
    DECLARE @IsVIP BIT = 0;
    DECLARE @TotalPurchased DECIMAL(18,2);
    
    SELECT @TotalPurchased = TotalPurchased
    FROM Customers 
    WHERE Id = @CustomerId;
    
    IF @TotalPurchased >= 50000000  -- 50 triệu VND
        SET @IsVIP = 1;
    
    RETURN @IsVIP;
END;
GO

-- Function: Tính membership level
CREATE FUNCTION FN_GetMembershipLevel(@TotalPurchased DECIMAL(18,2))
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @Level NVARCHAR(20);
    
    SET @Level = CASE 
        WHEN @TotalPurchased >= 50000000 THEN 'Platinum'
        WHEN @TotalPurchased >= 20000000 THEN 'Gold'
        WHEN @TotalPurchased >= 5000000 THEN 'Silver'
        ELSE 'Bronze'
    END;
    
    RETURN @Level;
END;
GO

-- ========================================
-- 25. INDEXES OPTIMIZATION - Tối ưu hóa
-- ========================================

-- Covering indexes cho performance
CREATE NONCLUSTERED INDEX IX_Products_Category_Brand_Active 
ON Products (CategoryId, BrandId, IsActive) 
INCLUDE (Id, Name, Price, DiscountPrice, ImageUrl, Rating, ReviewCount);

CREATE NONCLUSTERED INDEX IX_Orders_Customer_Status_Date 
ON Orders (CustomerId, Status) 
INCLUDE (Id, OrderNumber, OrderDate, TotalAmount, PaymentStatus);

CREATE NONCLUSTERED INDEX IX_OrderItems_Product_Order 
ON OrderItems (ProductId, OrderId) 
INCLUDE (Quantity, UnitPrice, TotalPrice);

CREATE NONCLUSTERED INDEX IX_Reviews_Product_Approved 
ON Reviews (ProductId, IsApproved) 
INCLUDE (Rating, Comment, CreatedAt, UserId);

CREATE NONCLUSTERED INDEX IX_CartItems_User_Product 
ON CartItems (UserId, ProductId) 
INCLUDE (Quantity, AddedAt, UpdatedAt);

-- ========================================
-- 26. SECURITY & PERMISSIONS
-- ========================================

-- Tạo roles cho phân quyền
-- (Chạy với quyền sysadmin)
/*
CREATE ROLE db_wuantech_admin;
CREATE ROLE db_wuantech_staff;
CREATE ROLE db_wuantech_readonly;

-- Phân quyền cho admin (full access)
ALTER ROLE db_owner ADD MEMBER db_wuantech_admin;

-- Phân quyền cho staff (read/write nhưng không delete)
GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO db_wuantech_staff;

-- Phân quyền cho readonly (chỉ đọc)
GRANT SELECT ON SCHEMA::dbo TO db_wuantech_readonly;
*/

-- ========================================
-- 27. COMPLETION MESSAGE
-- ========================================

PRINT '=== WUANTECH DATABASE CREATED SUCCESSFULLY ===';
PRINT 'Database: WuanTechDB';
PRINT 'Tables: 19 tables created';
PRINT 'Sample Data: Inserted successfully';
PRINT 'Stored Procedures: 6 procedures created';
PRINT 'Views: 4 views created';
PRINT 'Functions: 4 functions created';
PRINT 'Indexes: Optimized for performance';
PRINT '';
PRINT 'Ready for WuanTech E-Commerce API!';
PRINT '==========================================';

-- Test query để kiểm tra
SELECT 
    'Products' AS TableName, COUNT(*) AS RecordCount FROM Products
UNION ALL
SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL
SELECT 'Brands', COUNT(*) FROM Brands
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Warehouses', COUNT(*) FROM Warehouses
UNION ALL
SELECT 'Inventories', COUNT(*) FROM Inventories
UNION ALL
SELECT 'Coupons', COUNT(*) FROM Coupons;

-- ========================================
-- END OF SCRIPT
-- ========================================



SELECT *FROM Products;

SELECT *FROM ProductSpecifications;

SELECT 'Current Products' AS Status, COUNT(*) AS Count FROM Products WHERE IsActive = 1;

