FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy everything from your existing WebApi/publish/ (includes secret, nginx, configs)
COPY WebApi/publish/ .

# Expose port for ECS + ALB
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
