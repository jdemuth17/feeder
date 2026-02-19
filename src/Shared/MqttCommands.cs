namespace UniversalFeeder.Shared
{
    public static class MqttCommands
    {
        // Topic Structure
        public const string TopicRoot = "feeders";
        public const string CommandSuffix = "commands";

        // JSON Keys
        public const string KeyAction = "action";
        public const string KeyDurationMs = "ms";
        public const string KeyVolume = "vol";

        // Action Values
        public const string ActionFeed = "feed";
        public const string ActionChime = "chime";

        public static string GetCommandTopic(string uniqueId) => $"{TopicRoot}/{uniqueId}/{CommandSuffix}";
    }
}
