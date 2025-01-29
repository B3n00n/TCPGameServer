using System.Collections.Concurrent;
using GameServer.Core.Network;
using GameServer.Handlers;
using GameServer.Infrastructure.Repositories;

namespace GameServer.Core.Chat
{
    public class CommandHandler
    {
        private readonly AccountRepository _accountRepository;

        private readonly Dictionary<string, IChatCommand> _commands;
        private readonly ConcurrentDictionary<string, GameClient> _clients;
        private readonly Dictionary<int, string> _rankHelpMessages;

        public CommandHandler(ConcurrentDictionary<string, GameClient> clients, AccountRepository accountRepository)
        {
            _clients = clients;
            _commands = new Dictionary<string, IChatCommand>(StringComparer.OrdinalIgnoreCase);
            _rankHelpMessages = new Dictionary<int, string>();

            _accountRepository = accountRepository;

            RegisterCommands();
            GenerateHelpMessages();
        }

        private void RegisterCommands()
        {
            RegisterCommand(new OnlineCommand(_clients));
            RegisterCommand(new BroadcastCommand());
            RegisterCommand(new KickCommand(_clients));
            RegisterCommand(new BanCommand(_clients, _accountRepository));
            RegisterCommand(new UnbanCommand(_clients, _accountRepository));
            RegisterCommand(new MuteCommand(_clients, _accountRepository));
            RegisterCommand(new UnmuteCommand(_clients, _accountRepository));

            RegisterCommand(new HelpCommand(_rankHelpMessages));
        }

        private void RegisterCommand(IChatCommand command)
        {
            foreach (var trigger in command.Triggers)
            {
                _commands[trigger.ToLower()] = command;
            }
        }

        public async Task HandleCommand(GameClient sender, string[] commandParts, ChatPacketHandler chatPacketHandler)
        {
            string commandTrigger = commandParts[0].ToLower();

            if (_commands.TryGetValue(commandTrigger, out var command))
            {
                if (sender.Data.Rank >= command.RequiredRank)
                {
                    try
                    {
                        await command.ExecuteAsync(sender, commandParts.Skip(1).ToArray(), chatPacketHandler);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Command execution error: {ex}");
                        await chatPacketHandler.SendGameMessage(sender, "An error occurred while executing the command.");
                    }
                }
                else
                {
                    await chatPacketHandler.SendGameMessage(sender, "You don't have permission to use this command.");
                }
            }
            else
            {
                await chatPacketHandler.SendGameMessage(sender, $"Unknown command: {commandTrigger}");
            }
        }

        // Generate help messages for each rank
        private void GenerateHelpMessages()
        {
            for (int rank = 0; rank <= 6; rank++)
            {
                var commandList = _commands.Values
                    .Distinct()
                    .Where(cmd => rank >= cmd.RequiredRank)
                    .OrderBy(cmd => cmd.Triggers.First())
                    .Select(cmd => $"/{cmd.Triggers.First()} - {cmd.Description}");

                _rankHelpMessages[rank] = "Available commands:\n" + string.Join("\n", commandList);
            }
        }
    }
}