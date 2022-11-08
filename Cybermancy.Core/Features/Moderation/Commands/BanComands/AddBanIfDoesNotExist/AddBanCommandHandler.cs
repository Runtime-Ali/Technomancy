// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Cybermancy.Core.Contracts.Persistance;
using Cybermancy.Core.DatabaseQueryHelpers;
using Cybermancy.Core.Responses;
using Cybermancy.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Cybermancy.Core.Features.Moderation.Commands.BanComands.AddBanIfDoesNotExist
{
    public class AddBanCommandHandler : ICommandHandler<AddBanCommand, AddBanCommandResponse>
    {
        private readonly ICybermancyDbContext _cybermancyDbContext;

        public AddBanCommandHandler(ICybermancyDbContext cybermancyDbContext)
        {
            this._cybermancyDbContext = cybermancyDbContext;
        }

        public async ValueTask<AddBanCommandResponse> Handle(AddBanCommand command, CancellationToken cancellationToken)
        {
            var sin = await this._cybermancyDbContext.Sins.AddAsync(new Sin
            {
                GuildId = command.GuildId,
                UserId = command.UserId,
                Reason = command.Reason,
                SinType = SinType.Ban,
                ModeratorId = command.ModeratorId
            }, cancellationToken);
            await this._cybermancyDbContext.SaveChangesAsync(cancellationToken);

            var loggingChannel = await this._cybermancyDbContext.Guilds
                .WhereIdIs(command.GuildId)
                .Select(x => x.ModChannelLog)
                .FirstOrDefaultAsync(cancellationToken);
            return new AddBanCommandResponse { Success = true, SinId = sin.Entity.Id, LogChannelId = loggingChannel };
        }
    }
}