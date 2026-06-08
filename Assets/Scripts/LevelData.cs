using UnityEngine;

/// <summary>
/// Optional ScriptableObject to define category metadata.
/// You can create instances via Assets → Create → Millionaire → Level Data
/// and assign display names, icons, or descriptions for each category.
///
/// The current game uses category strings from the JSON directly,
/// but this ScriptableObject provides a hook if you want richer category UI later.
/// </summary>
namespace MillionaireGame
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Millionaire/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("Category key – must match the 'category' field in questions.json")]
        public string categoryKey;

        [Tooltip("Display name shown in the UI")]
        public string displayName;

        [Tooltip("Short description of this category")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Optional icon for the category button")]
        public Sprite icon;

        [Tooltip("Minimum number of questions recommended for this category")]
        public int recommendedMinQuestions = 30;
    }
}
