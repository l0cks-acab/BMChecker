# BMChecker

BMChecker is a plugin developed for Rust servers that checks for alt accounts and bans using the BattleMetrics API when a player joins, originally made for Rusty Reef. It alerts admins via a configurable webhook with detailed information in an embed.

## Features

- Checks players against the BattleMetrics API upon connection.
- Detects and reports bans and alt accounts.
- Sends detailed alerts via a configurable webhook.
- Customizable configuration including API key and webhook URL.

## Configuration

The configuration file `BMChecker.json` will be generated automatically. You need to provide your BattleMetrics API key and the webhook URL.

Example configuration:
```json
{
  "API Key (This is obtained from: https://www.battlemetrics.com/developers/token)": "your_api_key_here",
  "Webhook URL": "your_webhook_url_here"
}
```
