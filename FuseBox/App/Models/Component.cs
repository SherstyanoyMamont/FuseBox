namespace FuseBox
{
    public abstract class Component : BaseEntity
    {
        public int Amper { get; set; }
        public int Slots { get; set; }
        public decimal Price { get; set; }
        public int Poles { get; set; }
    }
}
