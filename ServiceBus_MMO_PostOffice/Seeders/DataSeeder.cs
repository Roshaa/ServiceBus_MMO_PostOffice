using ServiceBus_MMO_PostOffice.Models;

namespace ServiceBus_MMO_PostOffice.Data
{
    public class DataSeeder
    {
        public void Seed(ApplicationDbContext db)
        {
            if (db.Guild.Any())
            {
                return;
            }

            var guilds = new[]
            {
                new Guild { Name = "Knights of Azure" },
                new Guild { Name = "Crimson Ravens" },
                new Guild { Name = "Shadow Syndicate" },
            };

            db.Guild.AddRange(guilds);
            db.SaveChanges();

            var players = new[]
            {
                // Knights of Azure
                new Player { NickName = "AzureKnight",  GuildId = guilds[0].Id },
                new Player { NickName = "BlueMage",      GuildId = guilds[0].Id },
                new Player { NickName = "AzurePaladin",  GuildId = guilds[0].Id },
                new Player { NickName = "SkySentinel",   GuildId = guilds[0].Id },
                new Player { NickName = "CeruleanBlade", GuildId = guilds[0].Id },
                new Player { NickName = "FrostLancer",   GuildId = guilds[0].Id },
                new Player { NickName = "StormCaller",   GuildId = guilds[0].Id },
                new Player { NickName = "LightBringer",  GuildId = guilds[0].Id },
                new Player { NickName = "AegisKnight",   GuildId = guilds[0].Id },

                // Crimson Ravens
                new Player { NickName = "RavenQueen",    GuildId = guilds[1].Id },
                new Player { NickName = "CrimsonWing",   GuildId = guilds[1].Id },
                new Player { NickName = "ScarletArcher", GuildId = guilds[1].Id },
                new Player { NickName = "RavenWarden",   GuildId = guilds[1].Id },
                new Player { NickName = "BloodTalon",    GuildId = guilds[1].Id },
                new Player { NickName = "VermilionHex",  GuildId = guilds[1].Id },
                new Player { NickName = "GarnetRogue",   GuildId = guilds[1].Id },
                new Player { NickName = "RubyShade",     GuildId = guilds[1].Id },

                // Shadow Syndicate
                new Player { NickName = "NightShade",    GuildId = guilds[2].Id },
                new Player { NickName = "ShadeDancer",   GuildId = guilds[2].Id },
                new Player { NickName = "UmbralAssassin",GuildId = guilds[2].Id },
                new Player { NickName = "NightWhisper",  GuildId = guilds[2].Id },
                new Player { NickName = "VoidStrider",   GuildId = guilds[2].Id },
                new Player { NickName = "EbonBlade",     GuildId = guilds[2].Id },
                new Player { NickName = "Duskwalker",    GuildId = guilds[2].Id },
                new Player { NickName = "ObsidianMask",  GuildId = guilds[2].Id },
            };


            db.Player.AddRange(players);
            db.SaveChanges();
        }
    }
}