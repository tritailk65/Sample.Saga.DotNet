
## 1. Khởi tạo Solution

```bash
dotnet new sln -n MySolutionName
dotnet sln add Path/To/Project.csproj

```

## 2. Các loại Project phổ biến

### Web API (Backend)

```bash
dotnet new webapi -n MyProject.API

dotnet new webapi -n MyProject.API --use-controllers

```

### Class Library (Thư viện dùng chung)

```bash
dotnet new classlib -n MyProject.Core

```

### Console Application

```bash
dotnet new console -n MyProject.Utility

```

## 3. Các loại Project Test 

### xUnit

```bash
dotnet new xunit -n MyProject.Tests

```

### NUnit

```bash
dotnet new nunit -n MyProject.Tests.NUnit

```

### MSTest

```bash
dotnet new mstest -n MyProject.Tests.MSTest

```

## 5. Quy trình gợi ý (Workflow)

1. Tạo Solution: `dotnet new sln -n CleanArchitecture`
2. Tạo các Project:
* `dotnet new webapi -n CleanArchitecture.API`
* `dotnet new classlib -n CleanArchitecture.Core`
* `dotnet new xunit -n CleanArchitecture.UnitTests`


3. Link chúng vào Solution:
* `dotnet sln add CleanArchitecture.API/CleanArchitecture.API.csproj`
* `dotnet sln add CleanArchitecture.Core/CleanArchitecture.Core.csproj`
* `dotnet sln add CleanArchitecture.UnitTests/CleanArchitecture.UnitTests.csproj`


4. Thêm reference cho các project (ví dụ: API dùng Core):
* `dotnet add CleanArchitecture.API/CleanArchitecture.API.csproj reference CleanArchitecture.Core/CleanArchitecture.Core.csproj`


## Quản lý thư mục
# 1. Tạo thư mục vật lý trước (nếu chưa có)
mkdir src
mkdir tests

# 2. Tạo project bên trong các thư mục đó
dotnet new webapi -n MyProject.API -o src/MyProject.API
dotnet new xunit -n MyProject.Tests -o tests/MyProject.Tests

# 3. Add vào solution (tự động phân loại dựa trên đường dẫn)
dotnet sln add src/MyProject.API/MyProject.API.csproj
dotnet sln add tests/MyProject.Tests/MyProject.Tests.csproj

## Add migraiont by dot net ef
.NET CLI
dotnet ef migrations add InitialCreate --output-dir Your/Directory


dotnet ef migrations add --startup-project ../Booking.API --context BookingContext InitialCreate