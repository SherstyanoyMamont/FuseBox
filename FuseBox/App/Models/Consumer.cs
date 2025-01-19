namespace FuseBox
{
    public class Consumer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Watt { get; set; }
        public bool Contactor { get; set; }   // Connected to contactor?
        public bool SeparateRCD { get; set; } // Separate RCD?
        public bool IsCritical { get; set; }  // Critical line (non-switchable)
    }
}
