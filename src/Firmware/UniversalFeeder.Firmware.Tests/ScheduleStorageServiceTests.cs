using UniversalFeeder.Firmware;
using UniversalFeeder.Shared;

namespace UniversalFeeder.Firmware.Tests
{
    public class ScheduleStorageServiceTests
    {
        private ScheduleStorageService CreateService() => new();

        private FeedScheduleEntry CreateEntry(string id = "s1", int hour = 8, int minute = 0)
        {
            return new FeedScheduleEntry
            {
                Id = id,
                Hour = hour,
                Minute = minute,
                DurationSeconds = 5,
                PlayChime = true,
                IsEnabled = true
            };
        }

        // ── CRUD ─────────────────────────────────────────

        [Fact]
        public void AddOrUpdate_ShouldAddNewEntry()
        {
            var svc = CreateService();
            svc.AddOrUpdate(CreateEntry("s1"));

            Assert.Equal(1, svc.Count);
        }

        [Fact]
        public void AddOrUpdate_ShouldReplaceExistingEntry()
        {
            var svc = CreateService();
            svc.AddOrUpdate(CreateEntry("s1", hour: 8));
            svc.AddOrUpdate(CreateEntry("s1", hour: 12));

            Assert.Equal(1, svc.Count);
            var stored = (FeedScheduleEntry)svc.GetAll()[0];
            Assert.Equal(12, stored.Hour);
        }

        [Fact]
        public void AddOrUpdate_MultipleDifferentIds_ShouldKeepAll()
        {
            var svc = CreateService();
            svc.AddOrUpdate(CreateEntry("s1"));
            svc.AddOrUpdate(CreateEntry("s2"));
            svc.AddOrUpdate(CreateEntry("s3"));

            Assert.Equal(3, svc.Count);
        }

        [Fact]
        public void Remove_ExistingEntry_ShouldReturnTrueAndRemove()
        {
            var svc = CreateService();
            svc.AddOrUpdate(CreateEntry("s1"));

            bool removed = svc.Remove("s1");

            Assert.True(removed);
            Assert.Equal(0, svc.Count);
        }

        [Fact]
        public void Remove_NonExistentEntry_ShouldReturnFalse()
        {
            var svc = CreateService();
            svc.AddOrUpdate(CreateEntry("s1"));

            bool removed = svc.Remove("s999");

            Assert.False(removed);
            Assert.Equal(1, svc.Count);
        }

        // ── DaysToString / StringToDays ──────────────────

        [Theory]
        [InlineData(new[] { true, true, true, true, true, true, true }, "1111111")]
        [InlineData(new[] { true, true, true, true, true, false, false }, "1111100")]
        [InlineData(new[] { false, false, false, false, false, true, true }, "0000011")]
        [InlineData(new[] { true, false, true, false, true, false, true }, "1010101")]
        public void DaysToString_ShouldProduceCorrectString(bool[] days, string expected)
        {
            Assert.Equal(expected, ScheduleStorageService.DaysToString(days));
        }

        [Theory]
        [InlineData("1111111", new[] { true, true, true, true, true, true, true })]
        [InlineData("1111100", new[] { true, true, true, true, true, false, false })]
        [InlineData("0000011", new[] { false, false, false, false, false, true, true })]
        public void StringToDays_ShouldProduceCorrectArray(string input, bool[] expected)
        {
            var result = ScheduleStorageService.StringToDays(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void StringToDays_NullInput_ShouldReturnAllFalse()
        {
            var result = ScheduleStorageService.StringToDays(null!);
            Assert.All(result, b => Assert.False(b));
        }

        [Fact]
        public void StringToDays_WrongLength_ShouldReturnAllFalse()
        {
            var result = ScheduleStorageService.StringToDays("111");
            Assert.All(result, b => Assert.False(b));
        }

        [Fact]
        public void DaysRoundTrip_ShouldPreserveValues()
        {
            bool[] original = { true, false, true, true, false, false, true };
            string str = ScheduleStorageService.DaysToString(original);
            bool[] restored = ScheduleStorageService.StringToDays(str);
            Assert.Equal(original, restored);
        }

        // ── ToScheduleListJson ───────────────────────────

        [Fact]
        public void ToScheduleListJson_EmptyStore_ShouldProduceValidJson()
        {
            var svc = CreateService();
            var json = svc.ToScheduleListJson();

            Assert.Contains($"\"{MqttCommands.KeyAction}\":\"{MqttCommands.ActionSchedulesList}\"", json);
            Assert.Contains("\"schedules\":[]", json);
        }

        [Fact]
        public void ToScheduleListJson_WithEntries_ShouldContainAllFields()
        {
            var svc = CreateService();
            var entry = CreateEntry("abc123", hour: 14, minute: 30);
            entry.DurationSeconds = 10;
            entry.PlayChime = false;
            entry.IsEnabled = true;
            entry.Days = new[] { true, true, true, true, true, false, false };
            svc.AddOrUpdate(entry);

            var json = svc.ToScheduleListJson();

            Assert.Contains("\"sid\":\"abc123\"", json);
            Assert.Contains("\"hour\":14", json);
            Assert.Contains("\"min\":30", json);
            Assert.Contains("\"dur\":10", json);
            Assert.Contains("\"chime\":0", json);  // PlayChime = false
            Assert.Contains("\"on\":1", json);      // IsEnabled = true
            Assert.Contains("\"days\":\"1111100\"", json);
        }

        [Fact]
        public void ToScheduleListJson_MultipleEntries_ShouldContainAll()
        {
            var svc = CreateService();
            svc.AddOrUpdate(CreateEntry("s1", hour: 7));
            svc.AddOrUpdate(CreateEntry("s2", hour: 19));

            var json = svc.ToScheduleListJson();

            Assert.Contains("\"sid\":\"s1\"", json);
            Assert.Contains("\"sid\":\"s2\"", json);
            Assert.Contains("\"hour\":7", json);
            Assert.Contains("\"hour\":19", json);
        }

        // ── UtcOffsetMinutes ─────────────────────────────

        [Fact]
        public void UtcOffsetMinutes_DefaultsToZero()
        {
            var svc = CreateService();
            Assert.Equal(0, svc.UtcOffsetMinutes);
        }

        [Fact]
        public void UtcOffsetMinutes_CanBeSetAndRead()
        {
            var svc = CreateService();
            svc.UtcOffsetMinutes = -300; // EST
            Assert.Equal(-300, svc.UtcOffsetMinutes);
        }
    }
}
