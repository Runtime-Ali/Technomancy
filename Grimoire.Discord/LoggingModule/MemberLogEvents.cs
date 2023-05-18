// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Grimoire.Core.Features.Logging.Commands.UpdateAvatar;
using Grimoire.Core.Features.Logging.Commands.UpdateNickname;
using Grimoire.Core.Features.Logging.Commands.UpdateUsername;
using Grimoire.Core.Features.Logging.Queries.GetUserLogSettings;
using Grimoire.Discord.Notifications;
using Grimoire.Domain;

namespace Grimoire.Discord.LoggingModule
{
    [DiscordGuildMemberAddedEventSubscriber]
    [DiscordGuildMemberUpdatedEventSubscriber]
    [DiscordGuildMemberRemovedEventSubscriber]
    internal class MemberLogEvents :
        IDiscordGuildMemberAddedEventSubscriber,
        IDiscordGuildMemberUpdatedEventSubscriber,
        IDiscordGuildMemberRemovedEventSubscriber
    {
        private readonly IMediator _mediator;
        private readonly IInviteService _inviteService;
        private readonly IDiscordImageEmbedService _imageEmbedService;

        public MemberLogEvents(IMediator mediator, IInviteService inviteService, IDiscordImageEmbedService imageEmbedService)
        {
            this._mediator = mediator;
            this._inviteService = inviteService;
            this._imageEmbedService = imageEmbedService;
        }

        public async Task DiscordOnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            var settings = await this._mediator.Send(new GetUserLogSettingsQuery{ GuildId = args.Guild.Id });
            if (!settings.IsLoggingEnabled) return;
            if (settings.JoinChannelLog is null) return;
            var logChannel = args.Guild.Channels.GetValueOrDefault(settings.JoinChannelLog.Value);
            if (logChannel is null) return;

            var accountAge = DateTime.UtcNow - args.Member.CreationTimestamp;
            var invites = await args.Guild.GetInvitesAsync();
            var inviteUsed = this._inviteService.CalculateInviteUsed(new GuildInviteDto
            {
                GuildId = args.Guild.Id,
                Invites = new ConcurrentDictionary<string, Invite>(
                    invites.Select(x =>
                        new Invite
                        {
                            Code = x.Code,
                            Inviter = x.Inviter.GetUsernameWithDiscriminator(),
                            Url = x.ToString(),
                            Uses = x.Uses,
                            MaxUses = x.MaxUses
                        }).ToDictionary(x => x.Code))
            });
            var inviteUsedText = "";
            if (inviteUsed is not null)
                inviteUsedText = $"**Invite used:** {inviteUsed.Url} ({inviteUsed.Uses + 1} uses)\n**Created By:** {inviteUsed.Inviter}";
            else if (!string.IsNullOrWhiteSpace(args.Guild.VanityUrlCode))
                inviteUsedText = $"**Invite used:** Vanity Invite";
            else
                inviteUsedText = $"**Invite used:** Unknown Invite";

            var embed = new DiscordEmbedBuilder()
                .WithTitle("User Joined")
                .WithDescription($"**Name:** {args.Member.Mention}\n" +
                    $"**Created on:** {args.Member.CreationTimestamp:MMM dd, yyyy}\n" +
                    $"**Account age:** {accountAge.Days} days old\n" +
                    inviteUsedText)
                .WithColor(accountAge < TimeSpan.FromDays(7) ? GrimoireColor.Yellow : GrimoireColor.Green)
                .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                .WithThumbnail(args.Member.GetGuildAvatarUrl(ImageFormat.Auto))
                .WithFooter($"Total Members: {args.Guild.MemberCount}")
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (accountAge < TimeSpan.FromDays(7))
                embed.AddField("New Account", $"Created {accountAge.CustomTimeSpanString()}");
            await logChannel.SendMessageAsync(embed);
        }

        public async Task DiscordOnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs args)
        {
            var settings = await this._mediator.Send(new GetUserLogSettingsQuery{ GuildId = args.Guild.Id });
            if (!settings.IsLoggingEnabled) return;
            if (settings.LeaveChannelLog is null) return;
            var logChannel = args.Guild.Channels.GetValueOrDefault(settings.LeaveChannelLog.Value);
            if (logChannel is null) return;

            var accountAge = DateTimeOffset.UtcNow - args.Member.CreationTimestamp;
            var timeOnServer = DateTimeOffset.UtcNow - args.Member.JoinedAt;

            var embed = new DiscordEmbedBuilder()
                .WithTitle("User Left")
                .WithDescription($"**Name:** {args.Member.Mention}\n" +
                    $"**Created on:** {args.Member.CreationTimestamp:MMM dd, yyyy}\n" +
                    $"**Account age:** {accountAge.Days} days old\n" +
                    $"**Joined on:** {args.Member.JoinedAt:MMM dd, yyyy} ({timeOnServer.Days} days ago)")
                .WithColor(GrimoireColor.Purple)
                .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                .WithThumbnail(args.Member.GetGuildAvatarUrl(ImageFormat.Auto))
                .WithFooter($"Total Members: {args.Guild.MemberCount}")
                .WithTimestamp(DateTimeOffset.UtcNow)
                .AddField($"Roles[{args.Member.Roles.Count()}]",
                args.Member.Roles.Any()
                ? string.Join(' ', args.Member.Roles.Where(x => x.Id != args.Guild.Id).Select(x => x.Mention))
                : "None");
            await logChannel.SendMessageAsync(embed);
        }
        public async Task DiscordOnGuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            var nicknameTask = this.ProcessNicknameChanges(args);
            var usernameTask = this.ProcessUsernameChanges(args);
            var avatarTask = this.ProcessAvatarChanges(args);

            await Task.WhenAll(nicknameTask, usernameTask, avatarTask);
        }

        private async Task ProcessNicknameChanges(GuildMemberUpdateEventArgs args)
        {
            var nicknameResponse = await this._mediator.Send(new UpdateNicknameCommand
            {
                GuildId = args.Guild.Id,
                UserId = args.Member.Id,
                Nickname = args.NicknameAfter
            });
            if (nicknameResponse is not null && nicknameResponse.BeforeNickname != nicknameResponse.AfterNickname)
            {
                var logChannel = args.Guild.Channels.GetValueOrDefault(nicknameResponse.NicknameChannelLogId);
                if (logChannel is not null)
                {
                    var embed = new DiscordEmbedBuilder()
                    .WithTitle("Nickname Updated")
                    .WithDescription($"**User:** <@!{args.Member.Id}>\n\n" +
                        $"**Before:** {(string.IsNullOrWhiteSpace(nicknameResponse.BeforeNickname)? "None" : nicknameResponse.BeforeNickname)}\n" +
                        $"**After:** {(string.IsNullOrWhiteSpace(nicknameResponse.AfterNickname)? "None" : nicknameResponse.AfterNickname)}")
                    .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                    .WithThumbnail(args.Member.GetGuildAvatarUrl(ImageFormat.Auto))
                    .WithTimestamp(DateTimeOffset.UtcNow);
                    await logChannel.SendMessageAsync(embed);
                }
                await this._mediator.Publish(new NicknameTrackerNotification
                {
                    UserId = args.Member.Id,
                    GuildId = args.Guild.Id,
                    Username = args.Member.GetUsernameWithDiscriminator(),
                    BeforeNickname = nicknameResponse.BeforeNickname,
                    AfterNickname = nicknameResponse.AfterNickname
                });
            }
        }

        private async Task ProcessUsernameChanges(GuildMemberUpdateEventArgs args)
        {
            var usernameResponse = await this._mediator.Send(new UpdateUsernameCommand
            {
                GuildId = args.Guild.Id,
                UserId = args.Member.Id,
                Username = args.MemberAfter.GetUsernameWithDiscriminator()
            });
            if (usernameResponse is not null && usernameResponse.BeforeUsername != usernameResponse.AfterUsername)
            {
                var logChannel = args.Guild.Channels.GetValueOrDefault(usernameResponse.UsernameChannelLogId);
                if (logChannel is not null)
                {
                    var embed = new DiscordEmbedBuilder()
                            .WithTitle("Username Updated")
                            .WithDescription($"**User:** <@!{args.MemberAfter.Id}>\n\n" +
                                $"**Before:** {usernameResponse.BeforeUsername}\n" +
                                $"**After:** {usernameResponse.AfterUsername}")
                            .WithAuthor($"{args.MemberAfter.GetUsernameWithDiscriminator()} ({args.MemberAfter.Id})")
                            .WithThumbnail(args.MemberAfter.GetAvatarUrl(ImageFormat.Auto))
                            .WithTimestamp(DateTimeOffset.UtcNow);
                    await logChannel.SendMessageAsync(embed);
                }
                await this._mediator.Publish(new UsernameTrackerNotification
                {
                    UserId = args.Member.Id,
                    GuildId = args.Guild.Id,
                    BeforeUsername = usernameResponse.BeforeUsername,
                    AfterUsername = usernameResponse.AfterUsername
                });
            }
        }

        private async Task ProcessAvatarChanges(GuildMemberUpdateEventArgs args)
        {
            var avatarResponse = await this._mediator.Send(new UpdateAvatarCommand
            {
                GuildId = args.Guild.Id,
                UserId = args.Member.Id,
                AvatarUrl = args.MemberAfter.GetGuildAvatarUrl(ImageFormat.Auto, 128)
            });
            if (avatarResponse is not null && avatarResponse.BeforeAvatar != avatarResponse.AfterAvatar)
            {
                var logChannel = args.Guild.Channels.GetValueOrDefault(avatarResponse.AvatarChannelLogId);
                if (logChannel is not null)
                {
                    var embed = new DiscordEmbedBuilder()
                    .WithTitle("Avatar Updated")
                    .WithDescription($"**User:** <@!{args.Member.Id}>\n\n" +
                        $"Old avatar in thumbnail. New avatar down below")
                    .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                    .WithThumbnail(avatarResponse.BeforeAvatar)
                    .WithTimestamp(DateTimeOffset.UtcNow);
                    var messageBuilder = await this._imageEmbedService
                        .BuildImageEmbedAsync(new string[]{ avatarResponse.AfterAvatar },
                        args.Member.Id,
                        embed,
                        false);
                    await logChannel.SendMessageAsync(messageBuilder);
                }
                await this._mediator.Publish(new AvatarTrackerNotification
                {
                    UserId = args.Member.Id,
                    GuildId = args.Guild.Id,
                    Username = args.Member.GetUsernameWithDiscriminator(),
                    BeforeAvatar = avatarResponse.BeforeAvatar,
                    AfterAvatar = args.Member.GetAvatarUrl(ImageFormat.Auto, 128)
                });
            }
        }
    }
}
