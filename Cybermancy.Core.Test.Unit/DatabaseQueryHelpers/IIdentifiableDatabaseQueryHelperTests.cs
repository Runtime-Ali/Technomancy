// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Cybermancy.Core.DatabaseQueryHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Cybermancy.Core.Test.Unit.DatabaseQueryHelpers
{
    [TestFixture]
    public class IIdentifiableDatabaseQueryHelperTests
    {
        public TestDatabaseFixture DatabaseFixture { get; set; } = null!;

        [OneTimeSetUp]
        public void Setup() => this.DatabaseFixture = new TestDatabaseFixture();

        [Test]
        public async Task WhereIdsAre_WhenProvidedValidIds_ReturnsResultAsync()
        {
            var context = this.DatabaseFixture.CreateContext();

            var result = await context.Guilds.WhereIdsAre(new ulong[]{ TestDatabaseFixture.Guild1.Id }).ToArrayAsync();

            result.Should().HaveCount(1);
            result.Should().AllSatisfy(x => x.Id.Should().Be(TestDatabaseFixture.Guild1.Id));
        }

        [Test]
        public async Task WhereIdIs_WhenProvidedValidId_ReturnsResultAsync()
        {
            var context = this.DatabaseFixture.CreateContext();

            var result = await context.Guilds.WhereIdIs(TestDatabaseFixture.Guild2.Id).ToArrayAsync();

            result.Should().HaveCount(1);
            result.Should().AllSatisfy(x => x.Id.Should().Be(TestDatabaseFixture.Guild2.Id));
        }
    }
}