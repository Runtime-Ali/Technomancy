// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Cybermancy.Core.Responses;
using Mediator;

namespace Cybermancy.Core.Features.Moderation.Commands.MuteCommands.SetMuteRole
{
    public sealed record SetMuteRoleCommand : ICommand<BaseResponse>
    {
        public ulong Role { get; init; }
        public ulong GuildId { get; init; }
    }
}