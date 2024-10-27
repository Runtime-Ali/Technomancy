// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Grimoire.Features.Leveling.Settings;

[SlashRequireGuild]
[SlashRequireUserGuildPermissions(DiscordPermissions.ManageGuild)]
[SlashRequireModuleEnabled(Module.Leveling)]
[SlashCommandGroup("Ignore", "View or edit who is ignored for xp gain.")]
public sealed partial class IgnoreCommandGroup(IMediator mediator) : ApplicationCommandModule
{
    private readonly IMediator _mediator = mediator;

    [SlashCommand("Add", "Ignores a user, channel, or role for xp gains")]
    public Task IgnoreAsync(InteractionContext ctx,
        [Option("items", "The users, channels or roles to ignore")]
        string value) =>
        this.UpdateIgnoreState(ctx, value, true);

    [SlashCommand("Remove", "Removes a user, channel, or role from the ignored xp list.")]
    public Task WatchAsync(InteractionContext ctx,
        [Option("Item", "The user, channel or role to Observe")]
        string value) =>
        this.UpdateIgnoreState(ctx, value, false);

    private async Task UpdateIgnoreState(InteractionContext ctx, string value, bool shouldIgnore)
    {
        await ctx.DeferAsync();
        var matchedIds = await ctx.ParseStringIntoIdsAndGroupByTypeAsync(value)
            .ToDictionaryAsync(
                x => x.Key,
                v => v);
        if (matchedIds.Count == 0 || (matchedIds.ContainsKey("Invalid") && matchedIds.Keys.Count == 1))
        {
            await ctx.EditReplyAsync(GrimoireColor.Yellow, "Could not parse any ids from the submitted values.");
            return;
        }

        IUpdateIgnoreForXpGain command = shouldIgnore
            ? new AddIgnoreForXpGain.Command { GuildId = ctx.Guild.Id }
            : new RemoveIgnoreForXpGain.Command { GuildId = ctx.Guild.Id };

        if (matchedIds.TryGetValue("User", out var userIds))
            command.Users = await userIds
                .SelectAwait(async x => await BuildUserDto(ctx.Client, x, ctx.Guild.Id))
                .OfType<UserDto>()
                .ToArrayAsync();

        if (matchedIds.TryGetValue("Role", out var roleIds))
            command.Roles = await roleIds
                .Select(x =>
                    new RoleDto { Id = ulong.Parse(x), GuildId = ctx.Guild.Id }).ToArrayAsync();

        if (matchedIds.TryGetValue("Channel", out var channelIds))
            command.Channels = await channelIds
                .Select(x =>
                    new ChannelDto { Id = ulong.Parse(x), GuildId = ctx.Guild.Id }).ToArrayAsync();
        if (matchedIds.TryGetValue("Invalid", out var invalidIds))
            command.InvalidIds = await invalidIds.ToArrayAsync();
        var response = await this._mediator.Send(command);


        await ctx.EditReplyAsync(GrimoireColor.Green,
            string.IsNullOrWhiteSpace(response.Message)
                ? "All items in list provided were not ignored"
                : response.Message);
        await ctx.SendLogAsync(response, GrimoireColor.DarkPurple);
    }

    private static async ValueTask<UserDto?> BuildUserDto(DiscordClient client, string idString, ulong guildId)
    {
        var id = ulong.Parse(idString);
        if (client.Guilds[guildId].Members.TryGetValue(id, out var member))
            return new UserDto
            {
                Id = member.Id,
                Nickname = member.Nickname,
                Username = member.GetUsernameWithDiscriminator(),
                AvatarUrl = member.GetGuildAvatarUrl(ImageFormat.Auto)
            };
        try
        {
            var user = await client.GetUserAsync(id);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (user is not null)
                return new UserDto
                {
                    Id = user.Id,
                    Username = user.GetUsernameWithDiscriminator(),
                    AvatarUrl = user.GetAvatarUrl(ImageFormat.Auto)
                };
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }
}
