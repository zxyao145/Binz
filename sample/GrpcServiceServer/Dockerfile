#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 9527
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["sample/GrpcServiceServer/GrpcServiceServer.csproj", "sample/GrpcServiceServer/"]
RUN dotnet restore "sample/GrpcServiceServer/GrpcServiceServer.csproj"
COPY . .
WORKDIR "/src/sample/GrpcServiceServer"
RUN dotnet build "GrpcServiceServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GrpcServiceServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GrpcServiceServer.dll"]