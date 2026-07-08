FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
# Since context is ./src/backend, this will find SyrianStudyBot/SyrianStudyBot.csproj
COPY ["SyrianStudyBot/SyrianStudyBot.csproj", "SyrianStudyBot/"]
RUN dotnet restore "SyrianStudyBot/SyrianStudyBot.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "SyrianStudyBot/SyrianStudyBot.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SyrianStudyBot/SyrianStudyBot.csproj" -c Release -o /app/publish

# Use the official ASP.NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SyrianStudyBot.dll"]
