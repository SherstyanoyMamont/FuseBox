namespace FuseBox
{
    // Added interface IFuse
    public interface IFuse
    {
        int Id { get; set; } // Assigned automatically
        string? Name { get; set; }
        int Amper { get; set; }
        double Slots { get; set; }
        int Price { get; set; }
    }

    // Interface for machines that have connected equipment
    public interface IFuseWithEquipment : IFuse
    {
        List<Consumer> Equipments { get; set; }
    }

}
