#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["sample/GrpcServiceClient/GrpcServiceClient.csproj", "sample/GrpcServiceClient/"]
COPY ["src/Binz.Client/Binz.Client.csproj", "src/Binz.Client/"]
COPY ["src/Binz.Core/Binz.Core.csproj", "src/Binz.Core/"]
COPY ["src/Binz.Registry.Consul/Binz.Registry.Consul.csproj", "src/Binz.Registry.Consul/"]
COPY ["src/Binz.Registry.Etcd/Binz.Registry.Etcd.csproj", "src/Binz.Registry.Etcd/"]
RUN dotnet restore "sample/GrpcServiceClient/GrpcServiceClient.csproj"
COPY . .
WORKDIR "/src/sample/GrpcServiceClient"
RUN dotnet build "GrpcServiceClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GrpcServiceClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "GrpcServiceClient.dll"]