using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
using Discord.Rest;
using Discord.WebSocket;
using Discord;

namespace howell.gameServers
{
    public static class DiscordInteractionFunction
    {
        public static Discord.Rest.DiscordRestClient discordClient = new() { };
        public static Discord.WebSocket.DiscordSocketClient discordsockClient = new() { };

        [FunctionName("DiscordInteractionFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // convert body from json
            string body = await req.ReadAsStringAsync();
            log.LogInformation(body);

            // debug output for interaction verification
            string sig = req.Headers["X-Signature-Ed25519"];
            log.LogInformation($"sig - {sig}");
            string sigTimestamp = req.Headers["X-Signature-Timestamp"];
            log.LogInformation($"sig timestamp - {sigTimestamp}");
            log.LogInformation($"rawBody - {body}");
            RestInteraction interaction;

            if (discordClient.LoginState != LoginState.LoggedIn)
            {
                log.LogInformation("Logging into discord...");
                await discordClient.LoginAsync(TokenType.Bot, CONSTANTS.DISCORD_BOT_TOKEN, true);
            }

            try
            {
                interaction = await discordClient.ParseHttpInteractionAsync(CONSTANTS.DISCORD_PUBLIC_KEY, sig, sigTimestamp, body);
            }
            catch (BadSignatureException e)
            {
                log.LogInformation(e.Message);
                return new UnauthorizedResult();
            }

            log.LogInformation("interaction type: " + interaction.Type.ToString());

            if (interaction.Type == Discord.InteractionType.Ping)
            {
                log.LogInformation("ping");
                return new OkObjectResult(new { type = 1 });
            }
            else if (interaction.Type == Discord.InteractionType.ApplicationCommand)
            {
                log.LogInformation("application command");
                log.LogInformation("deferring...");
                SayHello(interaction, log);
                return new OkObjectResult(new { type = 5, content = "pending" });
            }
            else
            {
                return new OkResult();
            }

            //return resp;

            /*string signature = req.Headers["X-Signature-Ed25519"];
            string timestamp = req.Headers["X-Signature-Timestamp"];
            string body = await req.ReadAsStringAsync();

            if (!_client.IsValidHttpInteraction(CONSTANTS.DISCORD_PUBLIC_KEY, signature, timestamp, body))
            {
                return new UnauthorizedResult();
            }

            // create the rest slash command from the interaction req
            RestInteraction parsed = JsonSerializer.Deserialize<RestInteraction>(body);

            if (parsed.Type == InteractionType.Ping)
            {
                return new OkObjectResult(new { type = 1 });
            }

            RestSlashCommand slashCommand = JsonSerializer.Deserialize<RestSlashCommand>(body);

            SayHello(slashCommand, log);

            // return a deferred response
            return new OkObjectResult(new { type = 5 });

            //command = new RestSlashCommand

            // Use the interaction data to handle the command
            // ...

            return new OkResult();*/
        }

        [FunctionName("SayHello")]
        public static async Task SayHello(RestInteraction interaction, ILogger log)
        {
            log.LogInformation($"Saying hello to someone.");
            interaction.FollowupAsync($"Hello {interaction.User.Username}");

            return;
        }

        /*
                [FunctionName("DiscordInteractionInitializerFunction")]
                public static async Task DiscordInteractionInitializerFunction(
                    [TimerTrigger("0 5 * * * *")] TimerInfo timer,
                    ILogger log)
                {
                    if (_client == null)
                    {
                        _client = new DiscordSocketClient(new DiscordSocketConfig
                        {
                            LogLevel = LogSeverity.Info,
                            AlwaysDownloadUsers = true,
                            MessageCacheSize = 1000
                        });

                        await _client.LoginAsync(TokenType.Bot, "BOT_TOKEN");
                        await _client.StartAsync();
                        await _client.SetSlashCommandsAsync(null);
                    }
                }*/

        public static class CONSTANTS
        {
            public readonly static string DISCORD_PUBLIC_KEY = System.Environment.GetEnvironmentVariable("DISCORD_PUBLIC_KEY");
            public readonly static string DISCORD_APP_ID = System.Environment.GetEnvironmentVariable("DISCORD_APP_ID");
            public readonly static string DISCORD_SECRET = System.Environment.GetEnvironmentVariable("DISCORD_SECRET");
            public readonly static string DISCORD_BOT_TOKEN = System.Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
            public readonly static string AZURE_SUB_ID = System.Environment.GetEnvironmentVariable("AZURE_SUB_ID");
            public readonly static string KUBECONFIG_BASE64 = System.Environment.GetEnvironmentVariable("KUBECONFIG");
        }
    }
}
