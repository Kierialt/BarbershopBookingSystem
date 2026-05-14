# Backend API (ASP.NET Core 8) — Web Service na Render
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/backend.csproj ./backend/
RUN dotnet restore ./backend/backend.csproj

COPY backend/ ./backend/
WORKDIR /src/backend
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render ustawia zmienną PORT; domyślnie 8080 lokalnie
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

CMD ["sh", "-c", "exec dotnet backend.dll --urls \"http://0.0.0.0:${PORT:-8080}\""]
