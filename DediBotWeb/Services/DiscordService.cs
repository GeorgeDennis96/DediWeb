using DbAccessLibrary;
using DediBotWeb.Common.Models;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Net;
using System.Reflection;

namespace DediBotWeb.Services
{
    public class DiscordService : IDiscordService
    {
        private readonly DiscordSocketClient _client;
        private readonly IPlayerRepo _dbDataAccess;
        private List<SlashCommand> SlashCommands = new List<SlashCommand>();
        private static List<GameInstanceInfo> InstanceInfos = new List<GameInstanceInfo>();
        private const int DailyRewardAmount = 10000;
        private const int WeekendDailyRewardMultiplier = 2;

        public DiscordService(DiscordSocketClient client, IPlayerRepo dbDataAccess)
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

            InteractionService _interactionService = new InteractionService(_client.Rest);

            Discord.Rest.RestApplication appInfo = await _client.GetApplicationInfoAsync();
            Console.WriteLine($"APP ID: {appInfo.Id}");

            await this.BuildSlashCommands();
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

            // Delete to refresh the commands..
            foreach (SocketGuild? guild in _client.Guilds)
            {
                await guild.DeleteApplicationCommandsAsync();
            }

            foreach (SlashCommand slashCommand in SlashCommands)
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

            SlashCommand tradeCommand = new SlashCommand("deditrade", "Trade points with another user.", true);
            tradeCommand.AddOption("tradepartner", "The user you wish to trade with.", ApplicationCommandOptionType.User, true);
            tradeCommand.AddOption("amount", "The amount of points you wish to trade.", ApplicationCommandOptionType.Number, true);
            SlashCommands.Add(tradeCommand);

            SlashCommand betCommand = new SlashCommand("dedibet", "Bet on a game.", true);
            betCommand.AddOption("id", "The game ID.", ApplicationCommandOptionType.String, true);
            betCommand.AddOption("amount", "the amount you would like to bet.", ApplicationCommandOptionType.Integer, true);
            SlashCommands.Add(betCommand);

            SlashCommand rankingCommand = new SlashCommand("dedirankings", "Display the top 10 ranking players.", true);
            SlashCommands.Add(rankingCommand);

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
                case "deditrade":
                    await HandleTradeCommand(command);
                    break;
                case "dedirankings":
                    await HandleRankingCommand(command);
                    break;
                case "dedibet":
                    await HandleBettingCommand(command);
                    break;
                default:
                    await HandleUnkownCommand(command);
                    break;
            }
        }

        private async Task HandleBettingCommand(SocketSlashCommand command)
        {
            string gameId = (string)command.Data.Options.Where(x => x.Name == "id").FirstOrDefault().Value;
            GameInstanceInfo gameInfo = InstanceInfos.Where(i => i.ID.ToString() == gameId).FirstOrDefault();

            double amount = (double)command.Data.Options.Where(x => x.Name == "amount").FirstOrDefault().Value;
            int bet = (int)amount;


        }

        private async Task HandleRankingCommand(SocketSlashCommand command)
        {
            var builder = new ComponentBuilderV2();

            var topPlayers = await _dbDataAccess.GetTopPlayersAsync(10);

            for (int i = 0; i < topPlayers.Count; i++)
            {
                int rankNum = i + 1;
                builder.WithTextDisplay($"Rank #{rankNum} - {topPlayers[i].Username} - {topPlayers[i].WinRate}% - {topPlayers[i].Balance} points.");
            }
            await command.RespondAsync(components: builder.Build());
        }

        private async Task HandleTradeCommand(SocketSlashCommand command)
        {
            SocketUser tradePartner = (SocketUser)command.Data.Options.Where(x => x.Name == "tradepartner").FirstOrDefault().Value;
            double number = (double)command.Data.Options.Where(x => x.Name == "amount").FirstOrDefault().Value;
            int amount = (int)number;

            if (amount < 0)
            {
                await command.RespondAsync("You can't trade a negative number..", ephemeral: true);
                return;
            }

            if (amount > 0)
            {
                PlayerModel? whoDbModel = await _dbDataAccess.GetPlayerByDiscordIdAsync(command.User.Id);
                PlayerModel? tradePartnerDbModel = await _dbDataAccess.GetPlayerByDiscordIdAsync(tradePartner.Id);

                if (whoDbModel is null || tradePartnerDbModel is null)
                {
                    await command.RespondAsync("One or both users are not registered.", ephemeral: true);
                    return;
                }
                if (whoDbModel.Balance < amount)
                {
                    await command.RespondAsync("You do not have enough points to make that trade.", ephemeral: true);
                    return;
                }

                whoDbModel.Balance -= amount;
                tradePartnerDbModel.Balance += amount;

                await _dbDataAccess.UpdatePlayerAsync(whoDbModel);
                await _dbDataAccess.UpdatePlayerAsync(tradePartnerDbModel);

                Console.WriteLine($"{command.User.Mention} traded {amount} points to {tradePartner.Username}.");
                await command.RespondAsync($"{command.User.Mention} traded {amount} points to {tradePartner.Username}.");
            }
            else
            {
                await command.RespondAsync("You can only trade a positive amount of points.", ephemeral: true);
                return;
            }
        }

        private async Task HandleRulesCommand(SocketSlashCommand command)
        {
            await command.RespondAsync("Two players take turns rolling the previous number; if you roll a 1, you lose, and the winner gains the wager.", ephemeral: true);
        }

        private async Task HandleRegister(SocketSlashCommand command)
        {
            await _dbDataAccess.AddPlayerAsync(new PlayerModel() { DiscordId = command.User.Id, Balance = 25000, Username = command.User.Username });

            //if (await _dbDataAccess.AddPlayerAsync(new PlayerModel() { DiscordId = command.User.Id, Balance = 25000, Username = command.User.Username }) == 0)
            //    await command.RespondAsync("You are already registered.", ephemeral: true);
            //else
            await command.RespondAsync("You have registered.", ephemeral: true);
        }

        private async Task HandleDailyCommand(SocketSlashCommand command)
        {
            PlayerModel? whoDbModel = await _dbDataAccess.GetPlayerByDiscordIdAsync(command.User.Id);
            if (whoDbModel is null)
            {
                await command.RespondAsync("You are not registered please use the /dediregister command.");
                return;
            }

            if (whoDbModel.DailyClaimedAt.Date == DateTime.Today)
            {
                await command.RespondAsync("You have already claimed your daily points today, come back tomorrow!", ephemeral: true);
                return;
            }
            else
            {
                int awardAmount = DailyRewardAmount;
                if ((DateTime.Now.DayOfWeek == DayOfWeek.Saturday) || (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)) awardAmount *= WeekendDailyRewardMultiplier;

                whoDbModel.Balance += awardAmount;
                whoDbModel.DailyClaimedAt = DateTime.Today;
                await _dbDataAccess.UpdatePlayerAsync(whoDbModel);
                Console.WriteLine($"{command.User.Username} claimed their daily points.");
                await command.RespondAsync($"You have claimed your daily points, your balance is now {whoDbModel.Balance}", ephemeral: true);
            }
        }
        private async Task HandleWhoCommand(SocketSlashCommand command)
        {
            SocketUser who = (SocketUser)command.Data.Options.Where(x => x.Name == "who").FirstOrDefault().Value;
            PlayerModel? whoDbModel = await _dbDataAccess.GetPlayerByDiscordIdAsync(who.Id);

            if (whoDbModel is null)
            {
                await command.RespondAsync("That user is not registered.");
                return;
            }

            var builder = new ComponentBuilderV2();
            builder
                .WithTextDisplay($"Stats for {who.Username}")
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
            SocketUser? initialUser;
            SocketUser? challenegedUser;

            initialUser = command.User;
            challenegedUser = (SocketUser)command.Data.Options.Where(x => x.Name == "opponent").FirstOrDefault().Value;

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
            int wager = (int)number;

            if (wager < 0)
            {
                await command.RespondAsync("You can't wager a negative number..", ephemeral: true);
                return;
            }

            if (wager == 0 || wager == 1)
            {
                await command.RespondAsync("You can't wager between 1 and 2..", ephemeral: true);
                return;
            }

            GameInstanceInfo instanceInfo = AddInstanceInfo(wager, initialUser, challenegedUser);
            PlayerModel? dbInitialUser = await _dbDataAccess.GetPlayerByDiscordIdAsync(initialUser.Id);
            PlayerModel? dbChallengedUser = await _dbDataAccess.GetPlayerByDiscordIdAsync(challenegedUser.Id);

            if (dbInitialUser is null || dbChallengedUser is null)
            {
                await command.RespondAsync("One or both users are not registered.", ephemeral: true);
                return;
            }

            if (!(dbInitialUser.Balance >= wager) || !(dbChallengedUser.Balance >= wager))
            {
                await command.RespondAsync("One of the players does not have enough points to make that wager.", ephemeral: true);
                return;
            }

            var builder = new ComponentBuilderV2();
            var AcceptChallengeButton = new ButtonBuilder("Accept", customId: instanceInfo.AcceptButtonID);
            var declineChallengeButton = new ButtonBuilder("Decline", customId: instanceInfo.DeclineButtonID);

            builder
            .WithTextDisplay($"Game ID: {instanceInfo.ID}")
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

                    string customId = component.Data.CustomId;
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

                        PlayerModel challengerModel = _dbDataAccess.GetPlayerByDiscordIdAsync(instanceInfo.InitialChallenger.Id).Result;
                        PlayerModel challengedUserModel = _dbDataAccess.GetPlayerByDiscordIdAsync(instanceInfo.ChallengedUser.Id).Result;

                        List<PlayerModel> players = new List<PlayerModel>() { challengerModel, challengedUserModel };

                        instanceInfo.SetState(GameInstanceInfo.GameState.InProgress);

                        GatherPot(players, instanceInfo.InitialNumber, instanceInfo);

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

                        ulong rollAttemptUser = arg.User.Id;
                        ulong whoRolledLast = instanceInfo.RollHistory.Last().WhoRolled;
                        ulong initialChallenger = instanceInfo.InitialChallenger.Id;
                        ulong challengedUser = instanceInfo.ChallengedUser.Id;

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
                    await component.UpdateAsync(x =>
                    {
                        x.Components = BuildComponentUnsafe(instanceInfo).Build();
                    });
                    break;
                case SocketModal modal:
                    // Interaction came from a modal
                    break;
                default:
                    return;
            }
        }

        public ComponentBuilderV2 BuildComponentUnsafe(GameInstanceInfo gameInstanceInfo)
        {
            var builder = new ComponentBuilderV2();
            builder.WithTextDisplay($"Starting Number: {gameInstanceInfo.InitialNumber}");

            SocketUser rolledUser = gameInstanceInfo.InitialChallenger;
            SocketUser whoRollsNext = gameInstanceInfo.WhoRollsNext;
            if (whoRollsNext == gameInstanceInfo.InitialChallenger) rolledUser = gameInstanceInfo.ChallengedUser;

            PlayerModel winnerDbInfo = null;
            PlayerModel loserDbInfo = null;

            if (gameInstanceInfo.RollHistory.Where(x => x.RolledNumber == 1).Count() == 1) // Lose condition..
            {
                foreach (Roll roll in gameInstanceInfo.RollHistory)
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
                        gameInstanceInfo.SetState(GameInstanceInfo.GameState.Completed);

                        builder.WithTextDisplay($"{mention} rolled a 1.");

                        ulong loser = roll.WhoRolled;
                        ulong winner = gameInstanceInfo.InitialChallenger.Id == loser ? gameInstanceInfo.ChallengedUser.Id : gameInstanceInfo.InitialChallenger.Id;

                        winnerDbInfo = _dbDataAccess.GetPlayerByDiscordIdAsync(winner).Result;
                        loserDbInfo = _dbDataAccess.GetPlayerByDiscordIdAsync(loser).Result;

                        winnerDbInfo = UpdatePlayerStats(winnerDbInfo, gameInstanceInfo, didWin: true);
                        loserDbInfo = UpdatePlayerStats(loserDbInfo, gameInstanceInfo, didWin: false);

                        _dbDataAccess.UpdatePlayerAsync(winnerDbInfo);
                        _dbDataAccess.UpdatePlayerAsync(loserDbInfo);
                    }
                    else
                    {
                        builder.WithTextDisplay($"{mention} rolled a {roll.RolledNumber}.");
                    }
                }

                builder
                .WithTextDisplay($"{rolledUser.Mention} lost.")
                .WithTextDisplay($"{rolledUser.Mention} now has {loserDbInfo.Balance} points.")
                .WithTextDisplay($"{whoRollsNext.Mention} now has {winnerDbInfo.Balance} points.");

                InstanceInfos.Remove(gameInstanceInfo);
            }
            else // Lose condition not met.. continue the game..
            {
                builder
                .WithTextDisplay($"{rolledUser.Mention} rolled a {gameInstanceInfo.RollHistory.LastOrDefault().RolledNumber}")
                .WithTextDisplay($"{whoRollsNext.Mention}'s turn.")
                .WithActionRow([
                    new ButtonBuilder("Roll", customId: $"roll-{gameInstanceInfo.ID}")
                ]);
            }
            return builder;
        }
        #endregion

        #region HelperMethods

        private void GatherPot(List<PlayerModel> players, int wager, GameInstanceInfo gameInfo)
        {
            foreach (PlayerModel player in players)
            {
                player.Balance -= wager;
                gameInfo.AddPotentialWinnings(wager);
                _dbDataAccess.UpdatePlayerAsync(player);
            }
        }

        private PlayerModel UpdatePlayerStats(PlayerModel player, GameInstanceInfo gameInfo, bool didWin)
        {
            if (didWin)
            {
                player.Wins++;
                player.Balance += gameInfo.PotentialWinnings;
            }
            else
            {
                player.Losses++;
            }
            return player;
        }

        public GameInstanceInfo AddInstanceInfo(int startingNumber, SocketUser initialChallenger, SocketUser challengedUser)
        {
            var newInstanceInfo = new GameInstanceInfo(startingNumber, initialChallenger, challengedUser);
            InstanceInfos.Add(newInstanceInfo);
            return newInstanceInfo;
        }

        public async Task SetStatus(UserStatus status)
        {
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
