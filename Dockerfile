FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
EXPOSE 80
WORKDIR /App
COPY --from=build-env /App/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:5066
ENTRYPOINT ["dotnet", "proxy-tester.dll"]
