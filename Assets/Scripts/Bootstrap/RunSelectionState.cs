namespace Dagon.Bootstrap
{
    public static class RunSelectionState
    {
        private static string selectedCharacterId;
        private static string lastSelectedCharacterId;
        private static bool openCharacterSelectOnMenu;

        public static void SelectCharacter(string characterId)
        {
            selectedCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
            if (!string.IsNullOrWhiteSpace(selectedCharacterId))
            {
                lastSelectedCharacterId = selectedCharacterId;
            }

            openCharacterSelectOnMenu = false;
        }

        public static string ConsumeSelectedCharacterId()
        {
            var value = selectedCharacterId;
            selectedCharacterId = null;
            return value;
        }

        public static string LastSelectedCharacterId => lastSelectedCharacterId;

        public static void PrepareRetry()
        {
            selectedCharacterId = string.IsNullOrWhiteSpace(lastSelectedCharacterId) ? null : lastSelectedCharacterId;
            openCharacterSelectOnMenu = false;
        }

        public static void OpenCharacterSelectOnMenu()
        {
            openCharacterSelectOnMenu = true;
        }

        public static bool ConsumeOpenCharacterSelectOnMenu()
        {
            var value = openCharacterSelectOnMenu;
            openCharacterSelectOnMenu = false;
            return value;
        }

        public static void Clear()
        {
            selectedCharacterId = null;
            lastSelectedCharacterId = null;
            openCharacterSelectOnMenu = false;
        }
    }
}
