namespace ReactiveCollections.Tests.Models
{
    public class Unit
    {
        public string Name = "";

        public int HP;

        public override string ToString()
        {
            return $"{Name} ({HP})";
        }
    }
}
