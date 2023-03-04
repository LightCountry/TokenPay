#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TokenPay/TokenPay.csproj", "TokenPay/"]
RUN dotnet restore "TokenPay/TokenPay.csproj"
COPY . .
WORKDIR "/src/TokenPay"
RUN dotnet build "TokenPay.csproj" -c Release -o /app/build -property:WarningLevel=0

FROM build AS publish
RUN dotnet publish "TokenPay.csproj" -c Release -o /src/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "TokenPay.dll"]