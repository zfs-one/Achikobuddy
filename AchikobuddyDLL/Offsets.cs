namespace AchikobuddyDll
{
    public static class Offsets
    {
        public static class Player
        {
            public const uint Base = 0x008C8E90; // Player base pointer
            public const uint Name = 0x00B4A2A8; // Player name string
            public const uint Health = 0x58; // Offset from base
            public const uint X = 0x798; // Position X
            public const uint Y = 0x79C; // Position Y
            public const uint Z = 0x7A0; // Position Z
            public const uint TargetGuid = 0x2C; // Target GUID
        }

        public static class ClickToMove
        {
            public const uint Function = 0x6936F0; // CGPlayer_C__ClickToMove
            public const uint Base = 0xC4D88C; // CTM struct
            public const uint Action = 0x14; // Action type offset
            public const uint Guid = 0x8; // Target GUID offset
            public const uint X = 0x1C; // Position X offset
            public const uint Y = 0x20; // Position Y offset
            public const uint Z = 0x24; // Position Z offset
        }

        public static class ObjectManager
        {
            public const uint Base = 0x00AC6A50; // Object manager base
        }
    }
}