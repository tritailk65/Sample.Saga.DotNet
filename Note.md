### Create Solution

```bash
dotnet new sln -n MySolutionName
dotnet sln add Path/To/Project.csproj

```

### Web API (Backend)

```bash
dotnet new webapi -n MyProject.API

dotnet new webapi -n MyProject.API --use-controllers

```

### Class Library

```bash
dotnet new classlib -n MyProject.Core

```

### Console Application

```bash
dotnet new console -n MyProject.Utility

```

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

### Add migraiont by dot net ef
.NET CLI
```shell
dotnet ef migrations add InitialCreate --output-dir Your/Directory
dotnet ef migrations add --startup-project ../Booking.API --context BookingContext InitialCreate
```