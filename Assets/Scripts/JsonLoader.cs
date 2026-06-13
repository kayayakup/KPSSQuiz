using UnityEngine;

/// <summary>
/// Helper class that loads the questions JSON from Resources.
/// Place your "questions" TextAsset inside  Assets/Resources/questions.json
/// </summary>
namespace MillionaireGame
{
    public static class JsonLoader
    {
        /// <summary>
        /// Loads and deserializes the question database from Resources/questions.json.
        /// Returns null if the file is missing or malformed.
        /// </summary>
        public static QuestionDatabase LoadQuestions(string fileName)
        {
            string resourcePath = NormalizeResourcePath(fileName);
            TextAsset textAsset = LoadTextAsset(resourcePath);

            if (textAsset == null)
            {
                Debug.LogError($"[JsonLoader] Could not find Resources/{resourcePath}.json!");
                return null;
            }

            // Unity's JsonUtility cannot deserialize a raw array,
            // so the JSON file must wrap the array: { "questions": [ ... ] }
            string jsonText = textAsset.text.Trim();
            if (jsonText.StartsWith("["))
            {
                // Wrap raw array dynamically
                jsonText = "{ \"questions\": " + jsonText + " }";
            }

            QuestionDatabase db = JsonUtility.FromJson<QuestionDatabase>(jsonText);

            if (db == null || db.questions == null || db.questions.Count == 0)
            {
                Debug.LogError("[JsonLoader] JSON parsed but question list is empty!");
                return null;
            }

            Debug.Log($"[JsonLoader] Loaded {db.questions.Count} questions from JSON.");
            return db;
        }

        /// <summary>
        /// Loads and deserializes the reminders database.
        /// </summary>
        public static ReminderDatabase LoadReminders(string fileName)
        {
            string resourcePath = NormalizeResourcePath(fileName);
            TextAsset textAsset = LoadTextAsset(resourcePath);

            if (textAsset == null)
            {
                Debug.LogWarning($"[JsonLoader] Could not find Resources/{resourcePath}.json.");
                return null;
            }

            ReminderDatabase db = JsonUtility.FromJson<ReminderDatabase>(textAsset.text);

            if (db == null || db.items == null || db.items.Count == 0)
            {
                Debug.LogError($"[JsonLoader] JSON parsed but reminder list is empty for {fileName}!");
                return null;
            }

            Debug.Log($"[JsonLoader] Loaded {db.items.Count} reminders from JSON.");
            return db;
        }

        private static TextAsset LoadTextAsset(string resourcePath)
        {
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset != null) return textAsset;

            int slashIndex = resourcePath.LastIndexOf('/');
            if (slashIndex < 0) return null;

            string folder = resourcePath.Substring(0, slashIndex);
            string assetName = resourcePath.Substring(slashIndex + 1);
            TextAsset[] folderAssets = Resources.LoadAll<TextAsset>(folder);

            for (int i = 0; i < folderAssets.Length; i++)
            {
                if (string.Equals(folderAssets[i].name, assetName, System.StringComparison.OrdinalIgnoreCase))
                    return folderAssets[i];
            }

            return null;
        }

        private static string NormalizeResourcePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;

            string path = fileName.Replace('\\', '/').Trim();
            if (path.StartsWith("Resources/", System.StringComparison.OrdinalIgnoreCase))
                path = path.Substring("Resources/".Length);
            if (path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - ".json".Length);

            return path;
        }
    }
}
