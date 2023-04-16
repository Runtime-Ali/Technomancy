// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Cybermancy.Core.Features.Logging.Commands.SetLogSettings
{
    public sealed record SetLoggingSettingsCommand : ICommand
    {
        public ulong GuildId { get; init; }
        public LoggingSetting LogSetting { get; init; }
        public ulong? ChannelId { get; init; }
    }
    public enum LoggingSetting
    {
        JoinLog,
        LeaveLog,
        DeleteLog,
        BulkDeleteLog,
        EditLog,
        UsernameLog,
        NicknameLog,
        AvatarLog
    }
}
