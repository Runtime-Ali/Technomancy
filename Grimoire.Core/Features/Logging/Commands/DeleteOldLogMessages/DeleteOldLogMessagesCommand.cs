// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Grimoire.Core.Features.Logging.Commands.DeleteOldLogMessages
{
    public sealed record DeleteOldLogMessagesCommand : ICommand
    {
        public ulong[] DeletedOldLogMessageIds { get; init; } = Array.Empty<ulong>();
    }
}