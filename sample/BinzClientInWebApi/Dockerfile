#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# vs hava some bug, and I hava to generate the image by manual

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

EXPOSE 80


COPY bin/Debug/net6.0 /app
WORKDIR /app

ENTRYPOINT ["dotnet", "BinzClientInWebApi.dll"]