// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimoire.Core.Configuration;

[ExcludeFromCodeCoverage]
public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .IsRequired();
        builder.HasOne(e => e.Lock)
            .WithOne(e => e.Channel)
            .IsRequired(false);
#pragma warning disable CS0618 // Type or member is obsolete
        builder.Property(e => e.IsXpIgnored)
            .HasDefaultValue(value: false);
        builder.HasIndex(e => e.IsXpIgnored)
            .HasFilter("\"IsXpIgnored\" = TRUE");
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
