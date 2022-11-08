// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Cybermancy.Core.Features.Leveling.Commands.ManageRewardsCommands.RemoveReward;
using FluentAssertions;
using NUnit.Framework;

namespace Cybermancy.Core.Test.Unit.Features.Leveling.Commands.ManageRewardsCommands.RemoveReward
{
    [TestFixture]
    public class AddRewardCommandHandlerTests
    {
        public TestDatabaseFixture DatabaseFixture { get; set; } = null!;

        [OneTimeSetUp]
        public void Setup() => this.DatabaseFixture = new TestDatabaseFixture();

        [Test]
        public async Task WhenRemovingReward_IfRewardExists_RemoveRoleAsync()
        {
            var context = this.DatabaseFixture.CreateContext();
            context.Database.BeginTransaction();
            var CUT = new RemoveRewardCommandHandler(context);
            var command = new RemoveRewardCommand
            {
                RoleId = TestDatabaseFixture.Role2.Id
            };

            var response = await CUT.Handle(command, default);

            context.ChangeTracker.Clear();
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Removed <@&7> reward");
        }

        [Test]
        public async Task WhenAddingReward_IfRewardExist_UpdateRoleAsync()
        {
            var context = this.DatabaseFixture.CreateContext();
            context.Database.BeginTransaction();
            var CUT = new RemoveRewardCommandHandler(context);
            var command = new RemoveRewardCommand
            {
                RoleId = TestDatabaseFixture.Role1.Id
            };

            var response = await CUT.Handle(command, default);

            context.ChangeTracker.Clear();
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Did not find a saved reward for role <@&6>");
        }
    }
}