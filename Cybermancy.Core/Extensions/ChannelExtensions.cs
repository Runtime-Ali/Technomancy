// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Cybermancy.Core.Extensions
{
    public static class ChannelExtensions
    {
        public static string Mention(ulong id) => $"<#{id}>";
    }
}
