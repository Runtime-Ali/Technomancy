// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.Domain.Shared;

namespace Grimoire.Domain
{
    public class NicknameHistory : IIdentifiable<long>, IMember
    {
        public long Id { get; set; }
        public ulong UserId { get; set; }
        public virtual Member Member { get; set; } = null!;
        public string? Nickname { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public ulong GuildId { get; set; }
        public virtual Guild Guild { get; set; } = null!;
    }
}
