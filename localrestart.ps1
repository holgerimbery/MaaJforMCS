Get-Process dotnet | Stop-Process -Force
cd "CopilotStudioTestRunner.WebUI"
dotnet run --launch-profile https