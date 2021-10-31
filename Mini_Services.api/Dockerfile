FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY ["Mini_Services.Api.csproj", "Mini_Services.api/"]
RUN dotnet restore "Mini_Services.api/Mini_Services.Api.csproj"
COPY . .
RUN dotnet publish "Mini_Services.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mini_Services.Api.dll"]
