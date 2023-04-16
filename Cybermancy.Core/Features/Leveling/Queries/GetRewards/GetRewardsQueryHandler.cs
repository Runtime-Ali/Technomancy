// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Cybermancy.Core.Extensions;

namespace Cybermancy.Core.Features.Leveling.Queries.GetRewards
{
    public class GetRewardsQueryHandler : IRequestHandler<GetRewardsQuery, BaseResponse>
    {
        private readonly ICybermancyDbContext _cybermancyDbContext;

        public GetRewardsQueryHandler(ICybermancyDbContext cybermancyDbContext)
        {
            this._cybermancyDbContext = cybermancyDbContext;
        }

        public async ValueTask<BaseResponse> Handle(GetRewardsQuery request, CancellationToken cancellationToken)
        {
            var rewards = await this._cybermancyDbContext.Rewards
                .Where(x => x.GuildId == request.GuildId)
                .Select(x => $"Level:{x.RewardLevel} Role:{x.Mention()}")
                .ToListAsync(cancellationToken: cancellationToken);
            if (!rewards.Any())
                throw new AnticipatedException("This guild does not have any rewards.");
            return new BaseResponse
            {
                Message = string.Join('\n', rewards)
            };
        }
    }
}
