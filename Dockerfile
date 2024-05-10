# Build
FROM mcr.microsoft.com/dotnet/sdk:7.0
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# copy and build everything else
COPY . ./
RUN dotnet publish -c Release -o out
ENV ASPNETCORE_URLS=http://*:80
ENTRYPOINT ["dotnet", "out/proxy-tester.dll"]
