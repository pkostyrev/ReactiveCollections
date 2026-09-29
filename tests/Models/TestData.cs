namespace ReactiveCollections.Tests.Models
{
    public static class TestData
    {
        public static Player CreatePlayer(
            int id,
            string name,
            int teamId,
            int level)
        {
            return new Player
            {
                Id = id,
                Name = name,
                TeamId = teamId,
                Level = level
            };
        }

        public static void Bind(
            Player source,
            PlayerView result)
        {
            result.Id = source.Id;
            result.Text = source.Name;
            result.TeamId = source.TeamId;
        }
    }
}
