FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/BookingService/BookingService.csproj src/BookingService/
RUN dotnet restore src/BookingService/BookingService.csproj

COPY src/BookingService/ src/BookingService/
RUN dotnet publish src/BookingService/BookingService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BookingService.dll"]
