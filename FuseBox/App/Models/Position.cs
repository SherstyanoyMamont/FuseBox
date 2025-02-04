namespace FuseBox.App.Models
{
    public class Position
    {
        public int IndexStart { get; set; }
        public int IndexFinish { get; set; }

        public Position(int indexStart, int indexFinish)
        {
            IndexStart = indexStart;
            IndexFinish = indexFinish;
        }


        // public override string ToString() => $"List №{ListIndex}, Object №{ObjectIndex}";
    }
}
