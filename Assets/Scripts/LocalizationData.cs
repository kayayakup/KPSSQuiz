using System;
using System.Collections.Generic;
using UnityEngine;

namespace MillionaireGame
{
    [Serializable]
    public class TranslationItem
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class LanguageData
    {
        public string code;
        public string name;
        public List<TranslationItem> items;
    }

    [Serializable]
    public class LocalizationDataWrapper
    {
        public List<LanguageData> languages;
    }

    public static class LocalizationManager
    {
        private static Dictionary<string, Dictionary<string, string>> _localizedStrings = new Dictionary<string, Dictionary<string, string>>();
        private static List<LanguageData> _languages = new List<LanguageData>();
        private static string _currentLanguageCode = "EN";

        public static List<LanguageData> AvailableLanguages => _languages;

        public static void LoadData()
        {
            TextAsset textAsset = Resources.Load<TextAsset>("LocalizationData");
            if (textAsset == null)
            {
                Debug.LogError("[LocalizationManager] Could not find Resources/LocalizationData.json!");
                return;
            }

            LocalizationDataWrapper wrapper = JsonUtility.FromJson<LocalizationDataWrapper>(textAsset.text);
            _languages = wrapper.languages;

            _localizedStrings.Clear();
            foreach (var lang in _languages)
            {
                var dict = new Dictionary<string, string>();
                foreach (var item in lang.items)
                {
                    dict[item.key] = item.value;
                }
                _localizedStrings[lang.code] = dict;
            }
        }

        public static void SetLanguage(string code)
        {
            _currentLanguageCode = code;
        }

        public static string Get(string key, string defaultValue = "")
        {
            if (_localizedStrings.TryGetValue(_currentLanguageCode, out var dict))
            {
                if (dict.TryGetValue(key, out var value))
                {
                    return value;
                }
            }
            
            // Fallback to English if not found in current language
            if (_currentLanguageCode != "EN" && _localizedStrings.TryGetValue("EN", out var enDict))
            {
                if (enDict.TryGetValue(key, out var enValue))
                {
                    return enValue;
                }
            }

            return string.IsNullOrEmpty(defaultValue) ? key : defaultValue;
        }
    }
}
