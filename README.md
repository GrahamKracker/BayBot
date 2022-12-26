# BayBot
 A personal project to create an all in one bot for my common discord needs.

# Features
 Current features include:
 * Polls
 * Log channel for commands
 * Counting channel
 * Reaction roles
 * Echoed messages
 * More to come...

# How it works
 * Baybot uses Microsoft's .NET Maui library to run on an Android phone as a makeshift server to host a discord bot.
 * To make things more complicated, the actual bot handling code is in it's own library, where it is sent to the Android phone from a computer using adb as a dll, which is then loaded on the fly using reflection so that it can be reloaded while the bot is still running.
 * A core library is shared between the Android app and the library so that the library can store data while being reloaded, and have access to sending logs in the app.
 * The discord library I used was Discord .NET which ended up being really easy to use. Currently the bot makes use of slash commands and embeds to do most of its functions.

# Links
 * .NET Maui: [Github](https://github.com/dotnet/maui), [Documentation](https://learn.microsoft.com/en-us/dotnet/maui/?view=net-maui-7.0)
 * Discord .NET: [Github](https://github.com/discord-net/Discord.Net), [Documentation](https://discordnet.dev/)
