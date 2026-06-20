FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MicroServiceDemo.sln .
COPY src/SharedKernel/SharedKernel.csproj src/SharedKernel/
COPY src/Frontend/Frontend.csproj src/Frontend/
RUN dotnet restore src/Frontend/Frontend.csproj

COPY src/SharedKernel/ src/SharedKernel/
COPY src/Frontend/ src/Frontend/
RUN dotnet publish src/Frontend/Frontend.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Frontend.dll"]
