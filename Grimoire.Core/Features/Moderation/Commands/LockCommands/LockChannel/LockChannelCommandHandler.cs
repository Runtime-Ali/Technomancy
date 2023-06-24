// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Grimoire.Core.Features.Moderation.Commands.LockCommands.LockChannel;

public class LockChannelCommandHandler : ICommandHandler<LockChannelCommand, BaseResponse>
{
    private readonly IGrimoireDbContext _grimoireDbContext;

    public LockChannelCommandHandler(IGrimoireDbContext grimoireDbContext)
    {
        this._grimoireDbContext = grimoireDbContext;
    }

    public async ValueTask<BaseResponse> Handle(LockChannelCommand command, CancellationToken cancellationToken)
    {
        var lockEndTime = command.DurationType.GetDateTimeOffset(command.DurationAmount);

        var result = await this._grimoireDbContext.Channels
            .Where(x => x.GuildId == command.GuildId)
            .Where(x => x.Id == command.ChannelId)
            .Select(x => new
            {
                x.Lock,
                x.Guild.ModChannelLog
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (result is null)
            throw new AnticipatedException("Could not find that channel");

        if (result.Lock is not null)
        {
            result.Lock.ModeratorId = command.ModeratorId;
            result.Lock.EndTime = lockEndTime;
            result.Lock.Reason = command.Reason;
            this._grimoireDbContext.Locks.Update(result.Lock);
        }
        else
        {
            var local = this._grimoireDbContext.Locks.Local.FirstOrDefault(x => x.ChannelId == command.ChannelId);
            if (local is not null)
                this._grimoireDbContext.Entry(local).State = EntityState.Detached;
            var lockToAdd = new Lock
            {
                ChannelId = command.ChannelId,
                GuildId = command.GuildId,
                Reason = command.Reason,
                EndTime = lockEndTime,
                ModeratorId = command.ModeratorId,
                PreviouslyAllowed = command.PreviouslyAllowed,
                PreviouslyDenied = command.PreviouslyDenied
            };
            await this._grimoireDbContext.Locks.AddAsync(lockToAdd, cancellationToken);
        }
        await this._grimoireDbContext.SaveChangesAsync(cancellationToken);
        return new BaseResponse
        {
            LogChannelId = result.ModChannelLog
        };
    }

}
