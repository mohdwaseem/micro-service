FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MicroServiceDemo.sln .
COPY src/SharedKernel/SharedKernel.csproj src/SharedKernel/
COPY src/OrderService/OrderService.csproj src/OrderService/
RUN dotnet restore src/OrderService/OrderService.csproj

COPY src/SharedKernel/ src/SharedKernel/
COPY src/OrderService/ src/OrderService/
RUN dotnet publish src/OrderService/OrderService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderService.dll"]
