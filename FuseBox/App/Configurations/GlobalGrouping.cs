namespace FuseBox
{
    // Grouping lines (default: "By rooms")
    public class GlobalGrouping
    {
        public int Sockets { get; set; }
        public int Lighting { get; set; }
        public int Conditioners { get; set; }
        public GlobalGrouping(int sockets, int lighting, int conditioners)
        {
            this.Sockets = sockets;
            this.Lighting = lighting;
            this.Conditioners = conditioners;
        }
        public GlobalGrouping() { }
    }
}
