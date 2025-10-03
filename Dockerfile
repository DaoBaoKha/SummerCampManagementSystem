#build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

#copy csproj and restore (folder api project)
COPY SummerCampManagementSystem.API/*.csproj ./SummerCampManagementSystem.API/
RUN dotnet restore ./SummerCampManagementSystem.API/SummerCampManagementSystem.API.csproj

#copy source code
COPY . .
WORKDIR /src/SummerCampManagementSystem.API
RUN dotnet publish -c Release -o /app/publish

#runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SummerCampManagementSystem.API.dll"]
