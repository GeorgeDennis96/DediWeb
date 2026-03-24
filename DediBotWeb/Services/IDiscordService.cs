using Discord;

namespace DediBotWeb.Services
{
    public interface IDiscordService
    {
        public Task Start();
        public Task SetStatus(UserStatus status);
        public UserStatus GetStatus();
        public Task BuildSlashCommands();
    }
}
