namespace FuseBox
{
    public class EmptySlot : Component
    {
        public EmptySlot(int slots)
        {
            if (slots <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slots),
                    "Пустой участок должен занимать хотя бы один слот.");
            }

            Name = "Empty Slot";

            // Используем унаследованное Component.Slots.
            Slots = slots;
        }

        // Для Entity Framework.
        public EmptySlot()
        {
            Name = "Empty Slot";
        }
    }
}