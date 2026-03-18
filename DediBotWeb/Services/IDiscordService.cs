using Discord;

namespace DediBotWeb.Services
{
    public interface IDiscordService
    {
        public Task Start();
        public Task SetBotStatus(UserStatus status);
        public UserStatus GetStatus();
        public Task BuildSlashCommands();
    }
}
