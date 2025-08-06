# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj vào thư mục con
COPY emnhanvien/EmNhanVien.csproj ./emnhanvien/
RUN dotnet restore ./emnhanvien/EmNhanVien.csproj

# Copy toàn bộ source code
COPY . .

# Build project
WORKDIR /app/emnhanvien
RUN dotnet publish -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out ./

# Port config for Render
ENV ASPNETCORE_URLS=http://+:$PORT

# Chạy đúng file DLL đã build
ENTRYPOINT ["dotnet", "EmNhanVien.dll"]