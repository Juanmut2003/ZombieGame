#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Economy;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The colony's stores: what goes in, what comes out, and what must never happen.
    /// </summary>
    /// <remarks>
    /// Every fault here is arithmetic and silent. A price taken twice, a store that dips below
    /// nothing, a bill that takes the materials and then finds there is no fuel — none of it
    /// shows on screen until a colony has been quietly handed free buildings for an hour.
    /// </remarks>
    [TestFixture]
    public sealed class StockpileTests
    {
        private static Stockpile WithMaterials(int materials = 100)
        {
            var stockpile = new Stockpile();
            stockpile.Set(ResourceKind.Materials, materials);
            return stockpile;
        }

        private static List<ResourceAmount> Cost(params ResourceAmount[] lines) => new(lines);

        [Test]
        public void ANewColonyHasNothing()
        {
            var stockpile = new Stockpile();

            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                Assert.That(stockpile.AmountOf(kind), Is.Zero, $"{kind} came from somewhere");
            }
        }

        [Test]
        public void TheCountOfKindsMatchesTheKinds()
        {
            // The affordability check walks a fixed-size span. Adding a fifth store and leaving
            // this behind would make the new one invisible to every price in the game.
            Assert.That(
                Enum.GetValues(typeof(ResourceKind)).Length,
                Is.EqualTo(Stockpile.Kinds),
                "Stockpile.Kinds no longer matches ResourceKind");
        }

        [Test]
        public void WhatGoesInComesBackOut()
        {
            var stockpile = new Stockpile();
            stockpile.Add(ResourceKind.Food, 12);
            stockpile.Add(ResourceKind.Food, 8);

            Assert.That(stockpile.AmountOf(ResourceKind.Food), Is.EqualTo(20));
            Assert.That(stockpile.AmountOf(ResourceKind.Water), Is.Zero, "the stores leaked");
        }

        [Test]
        public void NothingIsAddedByAddingNothing()
        {
            var stockpile = WithMaterials(50);
            stockpile.Add(ResourceKind.Materials, 0);
            stockpile.Add(ResourceKind.Materials, -30);

            // A negative delivery is a withdrawal by the back door, and would skip every check
            // that spending goes through.
            Assert.That(stockpile.AmountOf(ResourceKind.Materials), Is.EqualTo(50));
        }

        [Test]
        public void AStoreNeverGoesBelowNothing()
        {
            var stockpile = WithMaterials(10);

            Assert.That(stockpile.TrySpend(ResourceKind.Materials, 11), Is.False);
            Assert.That(stockpile.AmountOf(ResourceKind.Materials), Is.EqualTo(10));

            stockpile.Set(ResourceKind.Materials, -5);
            Assert.That(stockpile.AmountOf(ResourceKind.Materials), Is.Zero);
        }

        [Test]
        public void SpendingExactlyWhatIsThereIsAllowed()
        {
            var stockpile = WithMaterials(40);

            Assert.That(stockpile.TrySpend(ResourceKind.Materials, 40), Is.True);
            Assert.That(stockpile.AmountOf(ResourceKind.Materials), Is.Zero);
        }

        [Test]
        public void ABillIsPaidWholeOrNotAtAll()
        {
            var stockpile = new Stockpile();
            stockpile.Set(ResourceKind.Materials, 100);
            stockpile.Set(ResourceKind.Fuel, 1);

            List<ResourceAmount> cost = Cost(
                new ResourceAmount(ResourceKind.Materials, 40),
                new ResourceAmount(ResourceKind.Fuel, 10));

            Assert.That(stockpile.CanAfford(cost), Is.False);
            Assert.That(stockpile.TrySpend(cost), Is.False);

            // The materials were plentiful and must still be there. Paying line by line is what
            // leaves a colony poorer and holding nothing.
            Assert.That(stockpile.AmountOf(ResourceKind.Materials), Is.EqualTo(100));
            Assert.That(stockpile.AmountOf(ResourceKind.Fuel), Is.EqualTo(1));
        }

        [Test]
        public void APayableBillTakesEveryLine()
        {
            var stockpile = new Stockpile();
            stockpile.Set(ResourceKind.Materials, 100);
            stockpile.Set(ResourceKind.Fuel, 20);

            Assert.That(
                stockpile.TrySpend(Cost(
                    new ResourceAmount(ResourceKind.Materials, 40),
                    new ResourceAmount(ResourceKind.Fuel, 10))),
                Is.True);

            Assert.That(stockpile.AmountOf(ResourceKind.Materials), Is.EqualTo(60));
            Assert.That(stockpile.AmountOf(ResourceKind.Fuel), Is.EqualTo(10));
        }

        [Test]
        public void TheSameStoreNamedTwiceIsAddedUp()
        {
            var stockpile = WithMaterials(30);

            // Two lines of the same kind, as a building with a frame and a roof would have.
            // Checked one at a time, both pass and the colony pays sixty out of thirty.
            List<ResourceAmount> cost = Cost(
                new ResourceAmount(ResourceKind.Materials, 25),
                new ResourceAmount(ResourceKind.Materials, 25));

            Assert.That(stockpile.CanAfford(cost), Is.False);
            Assert.That(stockpile.TrySpend(cost), Is.False);
            Assert.That(stockpile.AmountOf(ResourceKind.Materials), Is.EqualTo(30));
        }

        [Test]
        public void FreeThingsAreAlwaysAffordable()
        {
            var stockpile = new Stockpile();

            Assert.That(stockpile.CanAfford(null), Is.True);
            Assert.That(stockpile.CanAfford(new List<ResourceAmount>()), Is.True);
            Assert.That(stockpile.TrySpend(new List<ResourceAmount>()), Is.True);

            // And a line of nothing costs nothing rather than failing.
            Assert.That(
                stockpile.TrySpend(Cost(new ResourceAmount(ResourceKind.Fuel, 0))), Is.True);
        }

        [Test]
        public void TearingSomethingDownGivesThePriceBack()
        {
            var stockpile = WithMaterials(100);
            List<ResourceAmount> cost = Cost(new ResourceAmount(ResourceKind.Materials, 40));

            stockpile.TrySpend(cost);
            stockpile.Refund(cost);

            Assert.That(stockpile.AmountOf(ResourceKind.Materials), Is.EqualTo(100));
        }

        [Test]
        public void TheStoresSayWhenTheyChange()
        {
            var stockpile = new Stockpile();
            var heard = new List<(ResourceKind Kind, int Amount)>();
            stockpile.Changed += (kind, amount) => heard.Add((kind, amount));

            stockpile.Add(ResourceKind.Water, 5);
            stockpile.TrySpend(ResourceKind.Water, 2);

            Assert.That(heard.Count, Is.EqualTo(2));
            Assert.That(heard[0], Is.EqualTo((ResourceKind.Water, 5)));
            Assert.That(heard[1], Is.EqualTo((ResourceKind.Water, 3)));
        }

        [Test]
        public void NothingIsAnnouncedWhenNothingChanges()
        {
            var stockpile = WithMaterials(10);
            int heard = 0;
            stockpile.Changed += (_, _) => heard++;

            stockpile.Set(ResourceKind.Materials, 10);
            stockpile.Add(ResourceKind.Materials, 0);
            stockpile.TrySpend(ResourceKind.Materials, 99);

            // A readout that redraws on every refused purchase flickers for no reason.
            Assert.That(heard, Is.Zero);
        }

        [Test]
        public void APriceIsNeverNegative()
        {
            // A building priced at minus ten would hand out materials for being built.
            var line = new ResourceAmount(ResourceKind.Materials, -10);
            Assert.That(line.Amount, Is.Zero);
        }
    }
}
