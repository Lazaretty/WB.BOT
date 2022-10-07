FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WB.Telegram/WB.Telegram.csproj", "WB.Telegram/"]
RUN dotnet restore "WB.Telegram/WB.Telegram.csproj"
COPY . .
WORKDIR "/src/WB.Telegram"
RUN dotnet build "WB.Telegram.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WB.Telegram.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WB.Telegram.dll"]
