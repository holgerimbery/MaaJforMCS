# Stage 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["CopilotStudioTestRunner.WebUI/CopilotStudioTestRunner.WebUI.csproj", "CopilotStudioTestRunner.WebUI/"]
COPY ["CopilotStudioTestRunner.Core/CopilotStudioTestRunner.Core.csproj", "CopilotStudioTestRunner.Core/"]
COPY ["CopilotStudioTestRunner.Domain/CopilotStudioTestRunner.Domain.csproj", "CopilotStudioTestRunner.Domain/"]
COPY ["CopilotStudioTestRunner.Data/CopilotStudioTestRunner.Data.csproj", "CopilotStudioTestRunner.Data/"]
RUN dotnet restore "CopilotStudioTestRunner.WebUI/CopilotStudioTestRunner.WebUI.csproj"

COPY . .
WORKDIR "/src/CopilotStudioTestRunner.WebUI"
RUN dotnet publish -c Release -o /app/publish

# Stage 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .
COPY VERSION ./VERSION
RUN mkdir -p /app/data/index /app/data/uploads /app/logs \
    && chown -R appuser:appgroup /app/data /app/logs

USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS="http://+:8080"
ENTRYPOINT ["dotnet", "CopilotStudioTestRunner.WebUI.dll"]
