// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Grimoire.Core.Features.Logging.Commands.TrackerCommands.AddTracker
{
    public sealed record AddTrackerCommandResponse : BaseResponse
    {
        public ulong? ModerationLogId { get; init; }
    }
}