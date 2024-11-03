// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Grimoire.Features.Moderation.Lock.Commands;

public sealed class UnlockChannel
{
    [SlashRequireGuild]
    [SlashRequireModuleEnabled(Module.Moderation)]
    [SlashRequireUserPermissions(DiscordPermissions.ManageMessages)]
    [SlashRequireBotPermissions(DiscordPermissions.ManageChannels)]
    internal sealed class Command(IMediator mediator) : ApplicationCommandModule
    {
        private readonly IMediator _mediator = mediator;

        [SlashCommand("Unlock", "Prevents users from being able to speak in the channel")]
        public async Task UnlockChannelAsync(
            InteractionContext ctx,
            [Option("Channel", "The Channel to unlock. Current channel if not specified.")]
            DiscordChannel? channel = null)
        {
            await ctx.DeferAsync();
            channel ??= ctx.Channel;
            var response =
                await this._mediator.Send(new Request { ChannelId = channel.Id, GuildId = ctx.Guild.Id });

            if (!channel.IsThread)
            {
                var permissions = ctx.Guild.Channels[channel.Id].PermissionOverwrites
                    .First(x => x.Id == ctx.Guild.EveryoneRole.Id);
                await channel.AddOverwriteAsync(ctx.Guild.EveryoneRole,
                    permissions.Allowed.RevertLockPermissions(response.PreviouslyAllowed)
                    , permissions.Denied.RevertLockPermissions(response.PreviouslyDenied));
            }

            await ctx.EditReplyAsync(message: $"{channel.Mention} has been unlocked");

            await ctx.SendLogAsync(response, GrimoireColor.Purple,
                message: $"{channel.Mention} has been unlocked by {ctx.User.Mention}");
        }
    }


    public sealed record Request : IRequest<Response>
    {
        public required ulong ChannelId { get; init; }
        public required ulong GuildId { get; init; }
    }

    public sealed class Handler(GrimoireDbContext grimoireDbContext)
        : IRequestHandler<Request, Response>
    {
        private readonly GrimoireDbContext _grimoireDbContext = grimoireDbContext;

        public async Task<Response> Handle(Request command,
            CancellationToken cancellationToken)
        {
            var result = await this._grimoireDbContext.Locks
                .Where(x => x.ChannelId == command.ChannelId && x.GuildId == command.GuildId)
                .Select(x => new { Lock = x, ModerationLogId = x.Guild.ModChannelLog })
                .FirstOrDefaultAsync(cancellationToken);
            if (result?.Lock is null)
                throw new AnticipatedException("Could not find a lock entry for that channel.");

            this._grimoireDbContext.Locks.Remove(result.Lock);
            await this._grimoireDbContext.SaveChangesAsync(cancellationToken);

            return new Response
            {
                LogChannelId = result.ModerationLogId,
                PreviouslyAllowed = result.Lock.PreviouslyAllowed,
                PreviouslyDenied = result.Lock.PreviouslyDenied
            };
        }
    }

    public sealed record Response : BaseResponse
    {
        public long PreviouslyAllowed { get; init; }
        public long PreviouslyDenied { get; init; }
    }
}
