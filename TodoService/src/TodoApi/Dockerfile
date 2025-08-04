# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY TodoApi.csproj ./
RUN dotnet restore ./TodoApi.csproj

# copy everything else and publish
COPY . ./
RUN dotnet publish TodoApi.csproj -c Release -o /out /p:UseAppHost=false

# -------- Runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /out ./
ENTRYPOINT ["dotnet","TodoApi.dll"]
