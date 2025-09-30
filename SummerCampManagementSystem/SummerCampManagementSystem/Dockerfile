# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore
COPY *.csproj ./
RUN dotnet restore SummerCampManagementSystem.API.csproj

# copy source code
COPY . .
RUN dotnet publish SummerCampManagementSystem.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SummerCampManagementSystem.API.dll"]
