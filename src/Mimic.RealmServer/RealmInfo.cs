namespace Mimic.RealmServer
{
    public class RealmInfo
    {
        public string Name { get; set; }
        public string Ip { get; set; } = "127.0.0.1:1234";

        public float Population { get; set; } = 0.5f;

        // TODO: convert these to enums
        public byte Icon { get; set; }
        public bool Locked { get; set; } = false;
        public byte Flags { get; set; }

        public byte CharacterCount { get; set; }

        public byte TimeZone { get; set; } = 0x01;
    }
}
