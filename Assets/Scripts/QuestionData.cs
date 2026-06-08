using System;
using System.Collections.Generic;

/// <summary>
/// Serializable data classes for JSON question loading.
/// These mirror the JSON structure so Unity's JsonUtility can deserialize them.
/// </summary>
namespace MillionaireGame
{
    // ─────────────────────────────────────────────
    // Single question entry
    // ─────────────────────────────────────────────
    [Serializable]
    public class QuestionEntry
    {
        public int id;
        public string questionText;
        public string[] answers;         // always 4 elements
        public int correctAnswerIndex;   // 0‑3
        public int difficulty;           // 1‑5
        
        // New KPSS fields
        public string examType;
        public string subject;
        public string subcategory;
        public string topic;
        public string explanation;
        
        // Legacy field for compatibility
        public string category;          
    }

    // ─────────────────────────────────────────────
    // Wrapper so JsonUtility can parse the root array
    // ─────────────────────────────────────────────
    [Serializable]
    public class QuestionDatabase
    {
        public List<QuestionEntry> questions;
    }

    // ─────────────────────────────────────────────
    // Reminders
    // ─────────────────────────────────────────────
    [Serializable]
    public class ReminderEntry
    {
        public int id;
        public string text;
        public string source;
    }

    [Serializable]
    public class ReminderDatabase
    {
        public string language;
        public int version;
        public List<ReminderEntry> items;
    }
}
