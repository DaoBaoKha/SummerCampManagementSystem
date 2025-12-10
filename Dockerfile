# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SummerCampManagementSystem/SummerCampManagementSystem/SummerCampManagementSystem.API.csproj ./SummerCampManagementSystem/
RUN dotnet restore ./SummerCampManagementSystem/SummerCampManagementSystem.API.csproj

COPY . .
WORKDIR /src/SummerCampManagementSystem/SummerCampManagementSystem
RUN dotnet publish -c Release -o /app/publish

# runtime stage
#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SummerCampManagementSystem.API.dll"]
