#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;
using ZombieGame.Economy;
using ZombieGame.Missions;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Loot that is named: what a container holds, and where it ends up.
    /// </summary>
    /// <remarks>
    /// PROJECT.md §8. The point of naming it is that a haul answers a question the colony is
    /// already asking, so the thing worth testing is the whole length of the pipe: a container
    /// fills with water, the run counts it as water, the trip carries water home, and the water
    /// store goes up. A break anywhere along that turns into the colony being paid in salvage
    /// for everything, which looks exactly like the old behaviour and is invisible.
    /// </remarks>
    [TestFixture]
    public sealed class NamedLootTests
    {
        private static MissionDefinition Mission(params ResourceAmount[] table)
        {
            var mission = ScriptableObject.CreateInstance<MissionDefinition>();
            var serialized = new UnityEditor.SerializedObject(mission);

            UnityEditor.SerializedProperty loot = serialized.FindProperty("_lootTable");
            loot.arraySize = table.Length;
            for (int i = 0; i < table.Length; i++)
            {
                UnityEditor.SerializedProperty entry = loot.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").enumValueIndex = (int)table[i].Kind;
                entry.FindPropertyRelative("_amount").intValue = table[i].Amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return mission;
        }

        [Test]
        public void TheTableIsDealtRoundTheContainers()
        {
            MissionDefinition mission = Mission(
                new ResourceAmount(ResourceKind.Materials, 3),
                new ResourceAmount(ResourceKind.Water, 2));

            Assert.That(mission.LootFor(0)[0].Kind, Is.EqualTo(ResourceKind.Materials));
            Assert.That(mission.LootFor(1)[0].Kind, Is.EqualTo(ResourceKind.Water));
            Assert.That(mission.LootFor(2)[0].Kind, Is.EqualTo(ResourceKind.Materials),
                "the table did not wrap round");
            Assert.That(mission.LootFor(3)[0].Amount, Is.EqualTo(2));
        }

        [Test]
        public void AMissionWithNoTableHasEmptyContainersRatherThanThrowing()
        {
            MissionDefinition mission = Mission();

            Assert.That(mission.LootFor(0), Is.Empty);
            Assert.That(mission.LootTable, Is.Empty);
        }

        [Test]
        public void ARunCountsEachStoreSeparatelyAndTotalsThemForTheObjective()
        {
            var run = new MissionProgress(10, 1f);

            run.Collect(new[]
            {
                new ResourceAmount(ResourceKind.Water, 4),
                new ResourceAmount(ResourceKind.Food, 3),
            });
            run.Collect(new[] { new ResourceAmount(ResourceKind.Water, 3) });

            Assert.That(run.CarriedOf(ResourceKind.Water), Is.EqualTo(7));
            Assert.That(run.CarriedOf(ResourceKind.Food), Is.EqualTo(3));
            Assert.That(run.CarriedOf(ResourceKind.Fuel), Is.Zero);
            Assert.That(run.Carried, Is.EqualTo(10), "the objective counts the lot");
            Assert.That(run.ObjectiveMet, Is.True);
        }

        [Test]
        public void NothingComesHomeUntilTheSquadIsOut()
        {
            var run = new MissionProgress(4, 2f);
            run.Collect(new[] { new ResourceAmount(ResourceKind.Fuel, 4) });

            Assert.That(run.Haul, Is.Empty, "the haul counted before the squad was out");

            run.TickExtraction(1f, inZone: true);
            Assert.That(run.Haul, Is.Empty, "a half-finished hold banked the haul");

            run.TickExtraction(1f, inZone: true);

            Assert.That(ResourceNames.Line(run.Haul), Is.EqualTo("4 FUEL"));
        }

        [Test]
        public void ALostSquadBringsNoStoresHome()
        {
            var run = new MissionProgress(4, 1f);
            run.Collect(new[] { new ResourceAmount(ResourceKind.Food, 6) });
            run.Fail();

            Assert.That(run.Haul, Is.Empty);
            Assert.That(run.Banked, Is.Zero);
        }

        [Test]
        public void AHaulLandsInTheStoreItWasNamedAs()
        {
            var run = new ColonyRun(new ColonyLedger());
            run.Bank(new[]
            {
                new ResourceAmount(ResourceKind.Water, 6),
                new ResourceAmount(ResourceKind.Food, 4),
            });

            Assert.That(run.Stores.AmountOf(ResourceKind.Water), Is.EqualTo(6));
            Assert.That(run.Stores.AmountOf(ResourceKind.Food), Is.EqualTo(4));
            Assert.That(run.Stores.AmountOf(ResourceKind.Materials), Is.Zero,
                "everything was still paid as salvage");
            Assert.That(run.Hauled, Is.EqualTo(10), "the objective counts every store");
        }

        [Test]
        public void TheDebriefSaysWhatTheSuppliesTurnedInto()
        {
            var ended = new MissionEndedEvent(
                "Town Run",
                MissionOutcome.Extracted,
                banked: 10,
                lost: 0,
                wanted: 10,
                seconds: 90f,
                heatAtEnd: 0.2f,
                haul: new[]
                {
                    new ResourceAmount(ResourceKind.Materials, 6),
                    new ResourceAmount(ResourceKind.Water, 4),
                });

            IReadOnlyList<DebriefRow> rows = MissionDebrief.Rows(ended);

            bool found = false;
            foreach (DebriefRow row in rows)
            {
                if (row.Label == "INTO THE STORES")
                {
                    found = true;
                    Assert.That(row.Value, Is.EqualTo("6 MAT  4 WATER"));
                }
            }

            Assert.That(found, Is.True, "the debrief never says where the haul went");
        }

        [Test]
        public void ADebriefWithNoHaulLeavesTheLineOut()
        {
            var ended = new MissionEndedEvent(
                "Town Run",
                MissionOutcome.SquadLost,
                banked: 0,
                lost: 5,
                wanted: 10,
                seconds: 30f,
                heatAtEnd: 0.9f);

            foreach (DebriefRow row in MissionDebrief.Rows(ended))
            {
                Assert.That(row.Label, Is.Not.EqualTo("INTO THE STORES"));
            }
        }

        [Test]
        public void AmountsAreTotalledByStoreAndReadInTheStoresOwnOrder()
        {
            string line = ResourceNames.Line(new[]
            {
                new ResourceAmount(ResourceKind.Food, 3),
                new ResourceAmount(ResourceKind.Materials, 2),
                new ResourceAmount(ResourceKind.Food, 2),
            });

            Assert.That(line, Is.EqualTo("2 MAT  5 FOOD"));
            Assert.That(ResourceNames.Line(null), Is.Empty);
            Assert.That(ResourceNames.Line(System.Array.Empty<ResourceAmount>()), Is.Empty);
        }
    }
}
