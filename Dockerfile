# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["ApiLogDemo.csproj", "./"]
RUN dotnet restore "ApiLogDemo.csproj"

# copy everything else and publish
COPY . .
RUN dotnet publish "ApiLogDemo.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Ensure the app listens on port 80 inside the container
ENV ASPNETCORE_URLS="http://+:80"

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ApiLogDemo.dll"]
