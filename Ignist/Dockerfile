# Stage 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8081
EXPOSE 8081

CMD ["dotnet", "Ignist.dll"]

