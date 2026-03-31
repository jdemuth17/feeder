namespace UniversalFeeder.Shared
{
    public static class MqttCommands
    {
        // Topic Structure
        public const string TopicRoot = "feeders";
        public const string CommandSuffix = "commands";
        public const string ScheduleSuffix = "schedule";
        public const string LogSuffix = "logs";

        // JSON Keys
        public const string KeyAction = "action";
        public const string KeyDurationMs = "ms";
        public const string KeyVolume = "vol";
        public const string KeySchedule = "schedule";
        public const string KeyTime = "time";
        public const string KeyAmount = "amount";
        public const string KeyEnabled = "enabled";
        public const string KeyLog = "log";
        public const string KeyTimestamp = "timestamp";
        public const string KeySuccess = "success";
        public const string KeyStatus = "status";
        public const string KeyManual = "manual";

        // Action Values
        public const string ActionFeed = "feed";
        public const string ActionChime = "chime";
        public const string ActionSetSchedule = "set_schedule";
        public const string ActionAckSchedule = "ack_schedule";
        public const string ActionLog = "log";

        public static string GetCommandTopic(string uniqueId) => $"{TopicRoot}/{uniqueId}/{CommandSuffix}";
        public static string GetScheduleTopic(string uniqueId) => $"{TopicRoot}/{uniqueId}/{ScheduleSuffix}";
        public static string GetLogTopic(string uniqueId) => $"{TopicRoot}/{uniqueId}/{LogSuffix}";
    }
}
