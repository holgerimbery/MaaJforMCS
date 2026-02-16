// Test Direct Line connection
#r "bin/Debug/net9.0/CopilotStudioTestRunner.Core.dll"

using System;
using System.Threading.Tasks;
using CopilotStudioTestRunner.Core.DirectLine;
using Serilog;

// Enable detailed logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var secret = "dummysecret"; // Replace with actual secret for real test
var botId = "dummybotid";
var timeout = 30;
var useWebChannelSecret = true;

Log.Information("Starting connection test...");
Log.Information("Using web channel secret: {UseWebChannelSecret}", useWebChannelSecret);

try
{
    var client = new DirectLineClient(secret, botId, timeout, useWebChannelSecret);
    
    Log.Information("Step 1/3: Starting conversation...");
    var conversationId = await client.StartConversationAsync();
    Log.Information("Got conversation ID: {ConversationId}", conversationId);
    
    Log.Information("Step 2/3: Sending test message 'Hello'...");
    await client.SendActivityAsync(conversationId, "Hello");
    Log.Information("Message sent");
    
    Log.Information("Step 3/3: Retrieving activities...");
    var activities = await client.GetActivitiesAsync(conversationId);
    Log.Information("Retrieved {Count} activities", activities.Count);
    
    foreach (var activity in activities)
    {
        Log.Information("  - Type: {Type}, From: {From}, Text: {Text}", 
            activity.Type, activity.From?.Id, activity.Text);
    }
    
    var botMessages = activities.Where(a => a.Type == "message" && a.From?.Id != "user").ToList();
    Log.Information("Found {Count} bot messages", botMessages.Count);
    
    if (botMessages.Count > 0)
    {
        Log.Information("SUCCESS! Agent responded: {Response}", botMessages.First().Text);
    }
    else
    {
        Log.Error("FAILED! Agent did not respond");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Connection test failed");
}
