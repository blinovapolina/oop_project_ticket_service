FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/EventsService/EventsService.csproj src/EventsService/
RUN dotnet restore src/EventsService/EventsService.csproj

COPY src/EventsService/ src/EventsService/
RUN dotnet publish src/EventsService/EventsService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EventsService.dll"]
