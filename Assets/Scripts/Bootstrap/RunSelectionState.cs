namespace Dagon.Bootstrap
{
    public static class RunSelectionState
    {
        private static string selectedCharacterId;

        public static void SelectCharacter(string characterId)
        {
            selectedCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
        }

        public static string ConsumeSelectedCharacterId()
        {
            var value = selectedCharacterId;
            selectedCharacterId = null;
            return value;
        }

        public static void Clear()
        {
            selectedCharacterId = null;
        }
    }
}
