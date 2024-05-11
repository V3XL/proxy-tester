FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App
EXPOSE 5000/tcp
# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS runtime
WORKDIR /App
COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "proxy-tester.dll"]

