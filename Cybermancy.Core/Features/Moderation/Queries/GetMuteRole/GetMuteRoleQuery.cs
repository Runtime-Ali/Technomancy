// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Cybermancy.Core.Features.Moderation.Queries.GetMuteRole
{
    public class GetMuteRoleQuery : IRequest<GetMuteRoleQueryResponse>
    {
        public ulong GuildId { get; init; }
    }
}
