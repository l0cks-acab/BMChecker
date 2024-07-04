using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("BMChecker", "herbs.acab", "1.3.6")]
    [Description("Checks for alt accounts and bans using the BattleMetrics API when a player joins and alerts via webhook.")]

    public class BMChecker : CovalencePlugin
    {
        [PluginReference]
        private Plugin Clans;

        private string apiKey;
        private string webhookUrl;

        private Configuration config;

        private class Configuration
        {
            [JsonProperty("API Key (This is obtained from: https://www.battlemetrics.com/developers/token)")]
            public string ApiKey { get; set; } = "your_api_key_here";

            [JsonProperty("Webhook URL")]
            public string WebhookUrl { get; set; } = "your_webhook_url_here";
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void Init()
        {
            // Load the API key and webhook URL from the config
            apiKey = config.ApiKey;
            webhookUrl = config.WebhookUrl;

            if (string.IsNullOrEmpty(apiKey) || apiKey == "your_api_key_here")
            {
                PrintWarning("API Key is not set. Please configure your API key in the plugin configuration.");
            }

            if (string.IsNullOrEmpty(webhookUrl) || webhookUrl == "your_webhook_url_here")
            {
                PrintWarning("Webhook URL is not set. Please configure your webhook URL in the plugin configuration.");
            }
        }

        private void OnUserConnected(IPlayer player)
        {
            string playerId = player.Id;
            string playerName = player.Name;
            string connectionTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (string.IsNullOrEmpty(apiKey) || apiKey == "your_api_key_here")
            {
                Puts("API Key is not set. Skipping BattleMetrics check.");
                return;
            }

            // Call the BattleMetrics API to check for bans and alt accounts
            CheckPlayer(playerId, playerName, connectionTime);
        }

        private void CheckPlayer(string playerId, string playerName, string connectionTime)
        {
            string url = $"https://api.battlemetrics.com/players/{playerId}";

            webrequest.Enqueue(url, null, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    Puts($"Failed to get data from BattleMetrics API for player {playerName} ({playerId}): {code}");
                    return;
                }

                try
                {
                    var playerData = JObject.Parse(response);
                    // Check for bans and alt accounts here
                    bool isBanned = CheckForBans(playerData, out string banDetails);
                    bool hasAltAccounts = CheckForAltAccounts(playerData, out string altAccountDetails);

                    if (isBanned || hasAltAccounts)
                    {
                        Puts($"{playerName} ({playerId}) has been detected with issues.");
                        SendWebhookAlert(playerName, playerId, connectionTime, isBanned, banDetails, hasAltAccounts, altAccountDetails);
                    }
                }
                catch (Exception ex)
                {
                    Puts($"Error parsing BattleMetrics API response for player {playerName} ({playerId}): {ex.Message}");
                }
            }, this, RequestMethod.GET, new Dictionary<string, string> { { "Authorization", $"Bearer {apiKey}" } });
        }

        private bool CheckForBans(JObject playerData, out string banDetails)
        {
            banDetails = string.Empty;
            var bans = playerData["data"]?["relationships"]?["bans"]?["data"];
            if (bans != null && bans.HasValues)
            {
                foreach (var ban in bans)
                {
                    string banId = ban["id"]?.ToString();
                    banDetails += $"\n[Ban](https://www.battlemetrics.com/bans/{banId})";
                }
                return true;
            }

            return false;
        }

        private bool CheckForAltAccounts(JObject playerData, out string altAccountDetails)
        {
            altAccountDetails = string.Empty;
            var altAccounts = playerData["data"]?["relationships"]?["altAccounts"]?["data"];
            if (altAccounts != null && altAccounts.HasValues)
            {
                foreach (var alt in altAccounts)
                {
                    string altId = alt["id"]?.ToString();
                    altAccountDetails += $"\n[Alt Account](https://www.battlemetrics.com/players/{altId})";
                }
                return true;
            }

            return false;
        }

        private void SendWebhookAlert(string playerName, string playerId, string connectionTime, bool isBanned, string banDetails, bool hasAltAccounts, string altAccountDetails)
        {
            if (string.IsNullOrEmpty(webhookUrl) || webhookUrl == "your_webhook_url_here")
            {
                Puts("Webhook URL is not set. Skipping webhook alert.");
                return;
            }

            var payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "BattleMetrics Alert",
                        description = $"Player **{playerName}** (SteamID: {playerId}) connected at {connectionTime}.",
                        fields = new List<object>()
                        {
                            new { name = "Bans", value = isBanned ? banDetails : "No bans found", inline = false },
                            new { name = "Alt Accounts", value = hasAltAccounts ? altAccountDetails : "No alt accounts found", inline = false }
                        },
                        color = isBanned || hasAltAccounts ? 16711680 : 65280, // Red if issues found, green otherwise
                        footer = new
                        {
                            text = "developed by herbs.acab"
                        }
                    }
                }
            };

            webrequest.Enqueue(webhookUrl, JsonConvert.SerializeObject(payload), (code, response) =>
            {
                if (code != 200)
                {
                    Puts($"Failed to send webhook alert: {code} {response}");
                }
            }, this, RequestMethod.POST, new Dictionary<string, string> { { "Content-Type", "application/json" } });
        }
    }
}
