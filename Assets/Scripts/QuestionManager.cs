using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the question pool: loads from JSON, filters by category,
/// and selects questions based on difficulty progression.
/// </summary>
namespace MillionaireGame
{
    public class QuestionManager : MonoBehaviour
    {
        // ── Internal state ──
        private QuestionDatabase _database;
        private List<QuestionEntry> _filteredPool;          // questions for the chosen category
        private List<QuestionEntry> _selectedQuestions;      // 15 questions for one playthrough
        private readonly HashSet<int> _usedIds = new HashSet<int>();
        private readonly HashSet<string> _usedTopicsThisSession = new HashSet<string>(); // avoid repeating the same topic in a single session

        // ── Public read‑only access ──
        public List<QuestionEntry> SelectedQuestions => _selectedQuestions;
        public bool IsReady => _database != null && _database.questions.Count > 0;

        /// <summary>All distinct category names found in the JSON.</summary>
        public List<string> AvailableCategories { get; private set; } = new List<string>();

        // ─────────────────────────────────────────────
        // Initialization
        // ─────────────────────────────────────────────
        // Helper to normalize category names
        private string NormalizeCategory(string cat)
        {
            if (string.IsNullOrWhiteSpace(cat)) return cat;
            var lower = cat.ToLowerInvariant();
            // Standardize combined General Culture and General Ability
            if (lower.Contains("genel kültür") && lower.Contains("genel yetenek"))
                return "Genel Kültür ve Genel Yetenek";
            // Standardize field categories for Önlisans
            if (lower.Contains("alan dersleri") || lower.Contains("alan"))
                return "Alan Dersleri";
            return cat.Trim();
        }

        public void LoadDatabase(string branchPrefix)
        {
            _database = new QuestionDatabase() { questions = new List<QuestionEntry>() };

            TextAsset[] files = Resources.LoadAll<TextAsset>("Questions");
            foreach (var file in files)
            {
                bool isBranchFile = file.name.StartsWith(branchPrefix, System.StringComparison.OrdinalIgnoreCase);
                bool isGlobalFile = file.name.Equals("kpss_sorular", System.StringComparison.OrdinalIgnoreCase);

                if (isBranchFile || isGlobalFile)
                {
                    var dbPart = JsonLoader.LoadQuestions("Questions/" + file.name);
                    if (dbPart != null && dbPart.questions != null)
                    {
                        foreach (var q in dbPart.questions)
                        {
                            if (isGlobalFile && !string.Equals(q.examType, branchPrefix, System.StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (string.IsNullOrEmpty(q.category))
                            {
                                q.category = !string.IsNullOrEmpty(q.subcategory) ? q.subcategory : (!string.IsNullOrEmpty(q.subject) ? q.subject : file.name);
                            }
                        }
                        _database.questions.AddRange(dbPart.questions.Where(q =>
                            !isGlobalFile || string.Equals(q.examType, branchPrefix, System.StringComparison.OrdinalIgnoreCase)
                        ));
                    }
                }
            }

            if (_database.questions.Count > 0)
            {
                // Discover all categories (subjects, subcategories, and legacy categories)
                var subjects = _database.questions.Select(q => q.subject).Where(s => !string.IsNullOrEmpty(s));
                var subcats = _database.questions.Select(q => q.subcategory).Where(s => !string.IsNullOrEmpty(s));
                var cats = _database.questions.Select(q => q.category).Where(s => !string.IsNullOrEmpty(s));
                // Combine and normalize category names
                var allCategories = subjects.Concat(subcats).Concat(cats);
                var normalized = allCategories.Select(cat => NormalizeCategory(cat));
                AvailableCategories = normalized
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
                // Add "All" option at the beginning
                AvailableCategories.Insert(0, "All");

                Debug.Log($"[QuestionManager] Loaded {_database.questions.Count} questions. Categories: {string.Join(", ", AvailableCategories)}");
            }
            else
            {
                Debug.LogError($"[QuestionManager] No questions found for branch prefix '{branchPrefix}'!");
            }
        }

        // ─────────────────────────────────────────────
        // Prepare questions for a new game
        // ─────────────────────────────────────────────

        /// <summary>
        /// Filters the pool by <paramref name="category"/> and builds a 15‑question
        /// set following the difficulty ladder defined in MoneyLadder.
        /// </summary>
        public bool PrepareQuestions(string category)
        {
            if (_database == null) return false;

            // Filter by category (case‑insensitive)
            if (category.Equals("All", System.StringComparison.OrdinalIgnoreCase))
            {
                _filteredPool = new List<QuestionEntry>(_database.questions);
            }
            else
            {
                _filteredPool = _database.questions
                    .Where(q => (q.subject != null && q.subject.Equals(category, System.StringComparison.OrdinalIgnoreCase)) ||
                                (q.subcategory != null && q.subcategory.Equals(category, System.StringComparison.OrdinalIgnoreCase)) ||
                                (q.category != null && q.category.Equals(category, System.StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (_filteredPool.Count == 0)
            {
                Debug.LogWarning($"[QuestionManager] No questions found for category '{category}'.");
                return false;
            }

            Debug.Log($"[QuestionManager] {_filteredPool.Count} questions in category '{category}'.");

            // Cap the test size at 30 questions (typical KPSS standard length)
            int testSize = Mathf.Min(30, _filteredPool.Count);

            // Count unused questions in this category pool
            int unusedCount = 0;
            foreach (var q in _filteredPool)
            {
                if (!_usedIds.Contains(q.id)) unusedCount++;
            }

            // If we run out of unused questions, reset the tracker to start a new cycle
            if (unusedCount < testSize)
            {
                // Clear only the IDs belonging to this category to keep others preserved,
                // or simply clear all for simplicity. Let's clear IDs of the current category's questions:
                foreach (var q in _filteredPool)
                {
                    _usedIds.Remove(q.id);
                }
            }

            // Initialize the money ladder steps dynamically
            MoneyLadder.Initialize(testSize);

            _selectedQuestions = new List<QuestionEntry>();
            _usedTopicsThisSession.Clear();

            for (int step = 0; step < MoneyLadder.TotalSteps; step++)
            {
                QuestionEntry picked = PickQuestion();
                if (picked != null)
                {
                    _selectedQuestions.Add(picked);
                }
            }

            return true;
        }

        // ─────────────────────────────────────────────
        // Question picking with fallback
        // ─────────────────────────────────────────────
        private QuestionEntry PickQuestion()
        {
            var candidates = _filteredPool.Where(q => !_usedIds.Contains(q.id)).ToList();

            if (candidates.Count == 0)
            {
                // If we somehow run out, fallback to picking from the full pool
                candidates = new List<QuestionEntry>(_filteredPool);
                _usedIds.Clear();
            }

            if (candidates.Count == 0) return null;

            QuestionEntry pick = candidates[Random.Range(0, candidates.Count)];
            _usedIds.Add(pick.id);
            return pick;
        }

        /// <summary>Return the question for the given step (0‑based).</summary>
        public QuestionEntry GetQuestion(int stepIndex)
        {
            if (_selectedQuestions == null || stepIndex < 0 || stepIndex >= _selectedQuestions.Count)
                return null;
            return _selectedQuestions[stepIndex];
        }
    }
}
