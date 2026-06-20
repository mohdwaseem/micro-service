FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MicroServiceDemo.sln .
COPY src/SharedKernel/SharedKernel.csproj src/SharedKernel/
COPY src/ProductService/ProductService.csproj src/ProductService/
RUN dotnet restore src/ProductService/ProductService.csproj

COPY src/SharedKernel/ src/SharedKernel/
COPY src/ProductService/ src/ProductService/
RUN dotnet publish src/ProductService/ProductService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductService.dll"]
