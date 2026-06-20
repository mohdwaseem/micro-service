FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching on restore
COPY MicroServiceDemo.sln .
COPY src/SharedKernel/SharedKernel.csproj src/SharedKernel/
COPY src/UserService/UserService.csproj src/UserService/
RUN dotnet restore src/UserService/UserService.csproj

# Copy source and publish
COPY src/SharedKernel/ src/SharedKernel/
COPY src/UserService/ src/UserService/
RUN dotnet publish src/UserService/UserService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UserService.dll"]
