using Discord;

namespace DediBotWeb.Common.Models
{
    public class SlashCommand
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public List<AdditionOptions> additionOptions = new List<AdditionOptions>();

        public SlashCommand(string name, string description, bool required) 
        {
            this.Name = name;
            this.Description = description;
        }

        public void AddOption(string name, string description, ApplicationCommandOptionType type, bool required)
        {
            var option = new AdditionOptions(name, description, type, required);
            additionOptions.Add(option);
        }

        public class AdditionOptions
        {
            public string Name { get; private set; }
            public string Description { get; private set; }
            public ApplicationCommandOptionType Type { get; private set; }
            public bool Required { get; private set; }
            public AdditionOptions(string name, string description, ApplicationCommandOptionType type, bool required)
            {
                this.Name = name;
                this.Description = description;
                this.Type = type;
                this.Required = required;
            }
        }
    }
}
