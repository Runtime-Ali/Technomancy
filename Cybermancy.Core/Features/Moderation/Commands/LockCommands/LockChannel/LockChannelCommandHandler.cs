// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Cybermancy.Core.Features.Moderation.Commands.LockCommands.LockChannel
{
    public class LockChannelCommandHandler : ICommandHandler<LockChannelCommand, BaseResponse>
    {
        private readonly ICybermancyDbContext _cybermancyDbContext;

        public LockChannelCommandHandler(ICybermancyDbContext cybermancyDbContext)
        {
            this._cybermancyDbContext = cybermancyDbContext;
        }

        public async ValueTask<BaseResponse> Handle(LockChannelCommand command, CancellationToken cancellationToken)
        {
            var lockEndTime = command.DurationType.GetDateTimeOffset(command.DurationAmount);

            var result = await this._cybermancyDbContext.Channels
                .Where(x => x.GuildId == command.GuildId)
                .Include(x => x.Lock)
                .Include(x => x.Guild.ModChannelLog)
                .FirstAsync(x => x.Id == command.ChannelId, cancellationToken);
            if (result is null)
                throw new AnticipatedException("Could not find that channel");

            if(result.Lock is not null)
            {
                result.Lock.ModeratorId = command.ModeratorId;
                result.Lock.EndTime = lockEndTime;
                result.Lock.Reason = command.Reason;
                this._cybermancyDbContext.Channels.Update(result);
            }
            else
            {
                await this._cybermancyDbContext.Locks.AddAsync(new Domain.Lock
                {
                    ChannelId = command.ChannelId,
                    GuildId = command.GuildId,
                    Reason = command.Reason,
                    EndTime = lockEndTime,
                    ModeratorId = command.GuildId,
                    PreviouslyAllowed = command.PreviouslyAllowed,
                    PreviouslyDenied = command.PreviouslyDenied
                }, cancellationToken);
            }
            return new BaseResponse
            {
                LogChannelId = result.Guild.ModChannelLog
            };
        }

    }
}
