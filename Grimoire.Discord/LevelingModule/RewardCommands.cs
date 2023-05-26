// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.Core.Features.Leveling.Commands.ManageRewardsCommands.AddReward;
using Grimoire.Core.Features.Leveling.Commands.ManageRewardsCommands.RemoveReward;
using Grimoire.Core.Features.Leveling.Queries.GetRewards;

namespace Grimoire.Discord.LevelingModule
{
    [SlashCommandGroup("Rewards", "Commands for updating and viewing the server rewards")]
    [SlashRequireGuild]
    [SlashRequireModuleEnabled(Module.Leveling)]
    [SlashRequireUserGuildPermissions(Permissions.ManageGuild)]
    public class RewardCommands : ApplicationCommandModule
    {
        private readonly IMediator _mediator;

        public RewardCommands(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [SlashCommand("Add", "Adds or updates rewards for the server.")]
        public async Task AddAsync(InteractionContext ctx,
            [Option("Role", "The role to be added as a reward")] DiscordRole role,
            [Minimum(0)]
            [Maximum(int.MaxValue)]
            [Option("Level", "The level the reward is awarded at.")] long level)
        {
            var response = await this._mediator.Send(
                new AddRewardCommand
                {
                    RoleId = role.Id,
                    GuildId = ctx.Guild.Id,
                    RewardLevel = (int)level,
                });
            await ctx.ReplyAsync(GrimoireColor.DarkPurple, message: response.Message, ephemeral: false);
            await ctx.SendLogAsync(response, GrimoireColor.DarkPurple);
        }

        [SlashCommand("Remove", "Removes a reward from the server.")]
        public async Task RemoveAsync(InteractionContext ctx,
            [Option("Role", "The role to be awarded")] DiscordRole role)
        {
            var response = await this._mediator.Send(
                new RemoveRewardCommand
                {
                    RoleId = role.Id
                });

            await ctx.ReplyAsync(GrimoireColor.DarkPurple, message: response.Message, ephemeral: false);
            await ctx.SendLogAsync(response, GrimoireColor.DarkPurple);
        }

        [SlashCommand("View", "Displays all rewards on this server.")]
        public async Task ViewAsync(InteractionContext ctx)
        {
            var response = await this._mediator.Send(new GetRewardsQuery{ GuildId = ctx.Guild.Id});
            await ctx.ReplyAsync(GrimoireColor.DarkPurple,
                title: "Rewards",
                message: response.Message,
                ephemeral: false);
        }
    }
}
