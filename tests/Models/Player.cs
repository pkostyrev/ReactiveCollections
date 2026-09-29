namespace ReactiveCollections.Tests.Models
{
    /// <summary>
    /// Тестовая доменная модель: игрок.
    /// </summary>
    /// <remarks>
    /// <see cref="Equals"/> и <see cref="GetHashCode"/> переопределены по
    /// <see cref="Id"/>. Это позволяет тестам на дубликаты (несколько
    /// элементов, равных по Equals) работать осмысленно и проверять
    /// ветки кода, где сравниваются элементы источника.
    /// </remarks>
    public class Player
    {
        public int Id;
        public string Name = string.Empty;
        public int TeamId;
        public int Level;

        public override bool Equals(object? obj)
            => obj is Player other && Id == other.Id;

        public override int GetHashCode()
            => Id;
    }
}