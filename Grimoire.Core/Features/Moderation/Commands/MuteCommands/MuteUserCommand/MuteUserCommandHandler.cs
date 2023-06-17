// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.Core.DatabaseQueryHelpers;

namespace Grimoire.Core.Features.Moderation.Commands.MuteCommands.MuteUserCommand
{
    public class MuteUserCommandHandler : ICommandHandler<MuteUserCommand, MuteUserCommandResponse>
    {
        private readonly IGrimoireDbContext _grimoireDbContext;

        public MuteUserCommandHandler(IGrimoireDbContext grimoireDbContext)
        {
            this._grimoireDbContext = grimoireDbContext;
        }

        public async ValueTask<MuteUserCommandResponse> Handle(MuteUserCommand command, CancellationToken cancellationToken)
        {
            var response = await this._grimoireDbContext.Members
                .WhereMemberHasId(command.UserId, command.GuildId)
                .Select(x => new
                {
                    x.ActiveMute,
                    x.Guild.ModerationSettings.MuteRole,
                    x.Guild.ModChannelLog
                }).FirstOrDefaultAsync(cancellationToken);
            if (response is null) throw new AnticipatedException("Could not find User.");
            if (response.MuteRole is null) throw new AnticipatedException("A mute role is not configured.");
            if (response.ActiveMute is not null) this._grimoireDbContext.Mutes.Remove(response.ActiveMute);
            var lockEndTime = command.DurationType.GetDateTimeOffset(command.DurationAmount);
            var sin = new Sin
            {
                UserId = command.UserId,
                GuildId = command.GuildId,
                ModeratorId = command.ModeratorId,
                Reason = command.Reason,
                SinType = SinType.Mute,
                Mute = new Mute
                {
                    GuildId = command.GuildId,
                    UserId = command.UserId,
                    EndTime = lockEndTime
                }
            };
            await this._grimoireDbContext.Sins.AddAsync(sin, cancellationToken);
            await this._grimoireDbContext.SaveChangesAsync(cancellationToken);
            return new MuteUserCommandResponse
            {
                MuteRole = response.MuteRole.Value,
                LogChannelId = response.ModChannelLog,
            };
        }
    }
}