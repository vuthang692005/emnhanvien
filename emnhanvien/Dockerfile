
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY emnhanvien/emnhanvien.csproj ./emnhanvien/
RUN dotnet restore ./emnhanvien/emnhanvien.csproj

COPY . .

WORKDIR /app/emnhanvien
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:$PORT

ENTRYPOINT ["dotnet", "emnhanvien.dll"]