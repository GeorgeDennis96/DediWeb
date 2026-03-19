using DbAccessLibrary;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DediBotWeb.Common.Models;
using MudBlazor;

namespace DediBotWeb.Services
{
    public class DiscordService : IDiscordService
    {
        private readonly DiscordSocketClient _client;
        private readonly IPlayerData _dbDataAccess;
        private List<SlashCommand> SlashCommands = new List<SlashCommand>();
        private static List<GameInstanceInfo> InstanceInfos = new List<GameInstanceInfo>();
        public DiscordService(DiscordSocketClient client, IPlayerData dbDataAccess)
        {
            _client = client;
            _dbDataAccess = dbDataAccess;
            DefineSlashCommands();
        }

        public async Task Start()
        {
            _client.Log += Log_Async;

            await _client.LoginAsync(TokenType.Bot, "your_bot_token_here");
            await _client.StartAsync();

            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.InteractionCreated += ClientOnInteractionCreatedAsync;

            var _interactionService = new InteractionService(_client.Rest);
        }

        private static Task Log_Async(LogMessage log)
        {
            Console.WriteLine(log.Message);
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        #region SlashCommandMethods
        public async Task BuildSlashCommands()
        {
            if (SlashCommands.Count() == 0)
            {
                Console.WriteLine("Could not create slash commands because there were none to add.");
                return;
            }

            Task.Delay(5000).Wait(); // Wait for discord client connection to be established before creating commands..

            foreach (var slashCommand in SlashCommands)
            {
                var slashCommandInitiate = new Discord.SlashCommandBuilder()
                .WithName(slashCommand.Name)
                .WithDescription(slashCommand.Description);

                foreach (SlashCommand.AdditionOptions option in slashCommand.additionOptions)
                {
                    slashCommandInitiate.AddOption(option.Name, option.Type, option.Description, option.Required);
                }

                _client.Guilds.ToList().ForEach(guild =>
                {
                    guild.CreateApplicationCommandAsync(slashCommandInitiate.Build());
                });

            }
        }

        private void DefineSlashCommands()
        {
            // Outputs the rules of Dedi..
            SlashCommand ruleCommand = new SlashCommand("dedirules", "Display the rules of Dedi.", true);
            SlashCommands.Add(ruleCommand);

            // Register the user..
            SlashCommand registerCommand = new SlashCommand("dediregister", "Register as a player.", true);
            SlashCommands.Add(registerCommand);

            // Daily command to claim points..
            SlashCommand dailyCommand = new SlashCommand("dedidaily", "Claim your daily 10000 points.", true);
            SlashCommands.Add(dailyCommand);

            // Look up your or someone elses stats..
            SlashCommand whoCommand = new SlashCommand("dediwho", "Look up your or someone elses stats.", true);
            whoCommand.AddOption("who", "The user to view.", ApplicationCommandOptionType.User, true);
            SlashCommands.Add(whoCommand);

            // Initiate the death dice game with an opponent..
            SlashCommand deathDiceCommand = new SlashCommand("dedi", "Challenge someone to a death dice.", true);
            deathDiceCommand.AddOption("opponent", "The user you wish to challenge.", ApplicationCommandOptionType.User, true);
            deathDiceCommand.AddOption("wager", "The wager & starting amount of the death dice.", ApplicationCommandOptionType.Number, true);
            SlashCommands.Add(deathDiceCommand);
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "dediregister":
                    await HandleRegister(command);
                    break;
                case "dedidaily":
                    await HandleDailyCommand(command);
                    break;
                case "dediwho":
                    await HandleWhoCommand(command);
                    break;
                case "dedi":
                    await HandleDeathDiceCommand(command);
                    break;
                case "dedirules":
                    await HandleRulesCommand(command);
                    break;
                default:
                    await HandleUnkownCommand(command);
                    break;
            }
        }

        private async Task HandleRulesCommand(SocketSlashCommand command)
        {
            await command.RespondAsync("Two players take turns rolling the previous number; if you roll a 1, you lose, and the winner gains the wager.", ephemeral: true);
        }

        private async Task HandleRegister(SocketSlashCommand command)
        {
            if (await _dbDataAccess.InsertNewPlayer(new PlayerModel() { DiscordId = command.User.Id, Balance = 25000, Username = command.User.Username }) == 0)
                await command.RespondAsync("You are already registered.", ephemeral: true);
            else
                await command.RespondAsync("You have registered.", ephemeral: true);
        }

        private async Task HandleDailyCommand(SocketSlashCommand command)
        {
            var whoDbModel = await _dbDataAccess.GetPlayerByDiscordId(command.User.Id);
            if (whoDbModel is null)
            {
                await command.RespondAsync("You are not registered please use the /dediregister command.");
                return;
            }

            if (whoDbModel.DailyClaimedAt.Date == DateTime.UtcNow.Date)
            {
                await command.RespondAsync("You have already claimed your daily points today, come back tomorrow!", ephemeral: true);
                return;
            }
            else
            {
                whoDbModel.Balance += 10000;
                whoDbModel.DailyClaimedAt = DateTime.UtcNow;
                await _dbDataAccess.UpdatePlayer(whoDbModel);
                await command.RespondAsync($"You have claimed your daily points, your balance is now {whoDbModel.Balance}", ephemeral: true);
            }
        }
        private async Task HandleWhoCommand(SocketSlashCommand command)
        {
            SocketUser who = (SocketUser)command.Data.Options.Where(x => x.Name == "who").FirstOrDefault().Value;

            var whoDbModel = await _dbDataAccess.GetPlayerByDiscordId(who.Id);

            if (whoDbModel is null)
            {
                await command.RespondAsync("That user is not registered.");
                return;
            }

            var builder = new ComponentBuilderV2();
            builder
                .WithTextDisplay($"Stats for <@{who.Username}>")
                .WithTextDisplay($"Wins: {whoDbModel.Wins}")
                .WithTextDisplay($"Losses: {whoDbModel.Losses}")
                .WithTextDisplay($"Total Games Played: {whoDbModel.TotalGamesPlayed}")
                .WithTextDisplay($"Win Rate: {whoDbModel.WinRate}%")
                .WithTextDisplay($"Balance: {whoDbModel.Balance}");

            await command.RespondAsync(components: builder.Build());
        }

        private async Task HandleUnkownCommand(SocketSlashCommand command)
        {
            await command.RespondAsync("I'm not sure what you mean by that, please try again.");
        }

        private async Task HandleDeathDiceCommand(SocketSlashCommand command)
        {
            SocketUser? initialUser = command.User;
            SocketUser? challenegedUser = (SocketUser)command.Data.Options.Where(x => x.Name == "opponent").FirstOrDefault().Value;

            if (initialUser is null || challenegedUser is null)
            {
                await command.RespondAsync("Something internal happened.. aborting.", ephemeral: true);
                return;
            }

            if (challenegedUser == initialUser)
            {
                await command.RespondAsync("You can't challenge yourself..", ephemeral: true);
                return;
            }

            double number = (double)command.Data.Options.Where(x => x.Name == "wager").FirstOrDefault().Value;
            int numb = (int)number;

            if (numb == 0 || numb == 1)
            {
                await command.RespondAsync("You can't wager between 1 and 2..", ephemeral: true);
                return;
            }

            var instanceInfo = AddInstanceInfo(numb, initialUser, challenegedUser);

            var dbInitialUser = await _dbDataAccess.GetPlayerByDiscordId(initialUser.Id);
            var dbChallengedUser = await _dbDataAccess.GetPlayerByDiscordId(challenegedUser.Id);

            if (dbInitialUser is null || dbChallengedUser is null)
            {
                await command.RespondAsync("One or both users are not registered.", ephemeral: true);
                return;
            }

            if (!(dbInitialUser.Balance >= numb) || !(dbChallengedUser.Balance >= numb))
            {
                await command.RespondAsync("One of the players does not have enough points to make that wager.", ephemeral: true);
                return;
            }

            var builder = new ComponentBuilderV2();
            var AcceptChallengeButton = new ButtonBuilder("Accept", customId: instanceInfo.AcceptButtonID);
            var declineChallengeButton = new ButtonBuilder("Decline", customId: instanceInfo.DeclineButtonID);

            builder
            .WithTextDisplay($"{initialUser.Mention} has challenged {challenegedUser.Mention} to a death dice!")
            .WithTextDisplay($"Starting Number: {instanceInfo.InitialNumber}")
            .WithActionRow([
                AcceptChallengeButton, declineChallengeButton
                ]);

            await command.RespondAsync(components: builder.Build());
        }
        #endregion

        #region IteractionMethods
        private async Task ClientOnInteractionCreatedAsync(SocketInteraction arg)
        {
            switch (arg)
            {
                case SocketMessageComponent component:

                    var customId = component.Data.CustomId;
                    Console.WriteLine($"{arg.User.Id} clicked button..");
                    Console.WriteLine($"Custom ID: {customId}");
                    GameInstanceInfo instanceInfo = null;
                    ComponentBuilderV2 newComponentContainer;

                    // Handle the challenge component..
                    if (customId.StartsWith("accept-"))
                    {
                        instanceInfo = InstanceInfos.Where(i => i.AcceptButtonID == customId).FirstOrDefault();

                        if (component.User.Id != instanceInfo.ChallengedUser.Id)
                        {
                            await component.RespondAsync("You are not playing.", ephemeral: true);
                            return;
                        }

                        instanceInfo.AddRollHistory(instanceInfo.InitialNumber, component.User.Id);
                    }

                    if (customId.StartsWith("decline-"))
                    {
                        instanceInfo = InstanceInfos.Where(i => i.DeclineButtonID == customId).FirstOrDefault();

                        if (component.User.Id != instanceInfo.ChallengedUser.Id)
                        {
                            await component.RespondAsync("You are not playing.", ephemeral: true);
                            return;
                        }

                        var builder = new ComponentBuilderV2();
                        builder.WithTextDisplay($"{instanceInfo.ChallengedUser.Mention} declined the duel.");
                        InstanceInfos.Remove(instanceInfo);

                        await component.UpdateAsync(x =>
                        {
                            x.Components = builder.Build();
                        });
                        return;
                    }

                    // Handle the rolling component..
                    if (customId.StartsWith("roll-"))
                    {
                        instanceInfo = InstanceInfos.Where(i => $"roll-{i.ID.ToString()}" == customId).FirstOrDefault();

                        var rollAttemptUser = arg.User.Id;
                        var whoRolledLast = instanceInfo.RollHistory.Last().WhoRolled;

                        var initialChallenger = instanceInfo.InitialChallenger.Id;
                        var challengedUser = instanceInfo.ChallengedUser.Id;

                        if (rollAttemptUser != initialChallenger && rollAttemptUser != challengedUser)
                        {
                            await component.RespondAsync("You're not that guy pal, you're not that guy.", ephemeral: true);
                            return;
                        }

                        if (rollAttemptUser == whoRolledLast)
                        {
                            await component.RespondAsync("It is not your turn.", ephemeral: true);
                            return;
                        }

                        if (rollAttemptUser != whoRolledLast)
                        {
                            instanceInfo.AddRollHistory(instanceInfo.RollHistory.Last().RolledNumber, component.User.Id);
                        }
                    }

                    // Update the component..
                    newComponentContainer = BuildComponentUnsafe(instanceInfo, arg);
                    await component.UpdateAsync(x =>
                    {
                        x.Components = newComponentContainer.Build();
                    });
                    break;
                case SocketModal modal:
                    // Interaction came from a modal

                    break;
                default:
                    return;
            }
        }

        public ComponentBuilderV2 BuildComponentUnsafe(GameInstanceInfo gameInstanceInfo, SocketInteraction arg)
        {
            var builder = new ComponentBuilderV2();
            builder.WithTextDisplay($"Starting Number: {gameInstanceInfo.InitialNumber}");

            SocketUser rolledUser = gameInstanceInfo.InitialChallenger;
            SocketUser whoRollsNext = gameInstanceInfo.WhoRollsNext;
            if (whoRollsNext == gameInstanceInfo.InitialChallenger)
            {
                rolledUser = gameInstanceInfo.ChallengedUser;
            }

            var doesContainLoseCondition = gameInstanceInfo.RollHistory.Where(x => x.RolledNumber == 1);
            if (doesContainLoseCondition.Count() == 1) // Lose condition..
            {
                foreach (var roll in gameInstanceInfo.RollHistory)
                {
                    var mention = string.Empty;
                    if (roll.WhoRolled == gameInstanceInfo.InitialChallenger.Id)
                        mention = gameInstanceInfo.InitialChallenger.Mention;
                    else if (roll.WhoRolled == gameInstanceInfo.ChallengedUser.Id)
                        mention = gameInstanceInfo.ChallengedUser.Mention;
                    else
                        mention = $"<@{roll.WhoRolled}>";

                    if (roll.RolledNumber == 1)
                    {
                        builder.WithTextDisplay($"{mention} rolled a 1.");

                        var loser = roll.WhoRolled;
                        var winner = gameInstanceInfo.InitialChallenger.Id == loser ? gameInstanceInfo.ChallengedUser.Id : gameInstanceInfo.InitialChallenger.Id;

                        var winnerDbInfo = _dbDataAccess.GetPlayerByDiscordId(winner).Result;
                        var loserDbInfo = _dbDataAccess.GetPlayerByDiscordId(loser).Result;

                        winnerDbInfo = UpdatePlayerStats(winnerDbInfo, gameInstanceInfo.InitialNumber, didWin: true);
                        loserDbInfo = UpdatePlayerStats(loserDbInfo, gameInstanceInfo.InitialNumber, didWin: false);

                        _dbDataAccess.UpdatePlayer(winnerDbInfo);
                        _dbDataAccess.UpdatePlayer(loserDbInfo);
                    }
                    else
                    {
                        builder.WithTextDisplay($"{mention} rolled a {roll.RolledNumber}.");
                    }
                }

                builder
                .WithTextDisplay($"{rolledUser.Mention} lost.");
                InstanceInfos.Remove(gameInstanceInfo);
            }
            else
            {
                var RollButton = new ButtonBuilder("Roll", customId: $"roll-{gameInstanceInfo.ID}");
                builder
                .WithTextDisplay($"{rolledUser.Mention} rolled a {gameInstanceInfo.RollHistory.LastOrDefault().RolledNumber}")
                .WithTextDisplay($"{whoRollsNext.Mention} turn.")
                .WithActionRow([
                    RollButton
                ]);
            }
            return builder;
        }
        #endregion

        #region HelperMethods

        private PlayerModel UpdatePlayerStats(PlayerModel player, int initialWager, bool didWin)
        {
            if (didWin)
            {
                player.Wins += 1;
                player.TotalGamesPlayed += 1;
                player.Balance += initialWager;
            }
            else
            {
                player.Losses += 1;
                player.TotalGamesPlayed += 1;
                player.Balance -= initialWager;
            }
            return player;
        }

        public GameInstanceInfo AddInstanceInfo(int startingNumber, SocketUser initialChallenger, SocketUser challengedUser)
        {
            InstanceInfos.Add(new GameInstanceInfo(startingNumber, initialChallenger, challengedUser));
            return InstanceInfos.Where(i => i.InitialChallenger == initialChallenger && i.ChallengedUser == challengedUser).FirstOrDefault();
        }

        public async Task SetBotStatus(UserStatus status)
        {
            Console.WriteLine($"Set status to: {status}.");
            await _client.SetStatusAsync(status);

            Console.WriteLine($"Status: {_client.Status}");

        }
        public UserStatus GetStatus()
        {
            return _client.Status;
        }
        #endregion
    }
}
