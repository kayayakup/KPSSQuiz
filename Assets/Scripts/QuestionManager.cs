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
        private HashSet<int> _usedIds = new HashSet<int>(); // avoid repeats across playthroughs

        // ── Public read‑only access ──
        public List<QuestionEntry> SelectedQuestions => _selectedQuestions;
        public bool IsReady => _database != null && _database.questions.Count > 0;

        /// <summary>All distinct category names found in the JSON.</summary>
        public List<string> AvailableCategories { get; private set; } = new List<string>();

        // ─────────────────────────────────────────────
        // Initialization
        // ─────────────────────────────────────────────
        public void LoadDatabase(string branchPrefix)
        {
            _database = new QuestionDatabase() { questions = new List<QuestionEntry>() };
            
            TextAsset[] files = Resources.LoadAll<TextAsset>("Questions");
            foreach (var file in files)
            {
                if (file.name.StartsWith(branchPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    var dbPart = JsonLoader.LoadQuestions("Questions/" + file.name);
                    if (dbPart != null && dbPart.questions != null)
                    {
                        foreach(var q in dbPart.questions) {
                            if (string.IsNullOrEmpty(q.category)) {
                                q.category = !string.IsNullOrEmpty(q.subject) ? q.subject : file.name;
                            }
                        }
                        _database.questions.AddRange(dbPart.questions);
                    }
                }
            }

            if (_database.questions.Count > 0)
            {
                // Discover all categories
                AvailableCategories = _database.questions
                    .Select(q => q.category)
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
                    .Where(q => q.category.Equals(category, System.StringComparison.OrdinalIgnoreCase))
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

            for (int step = 0; step < MoneyLadder.TotalSteps; step++)
            {
                int targetDifficulty = MoneyLadder.StepDifficulty[step];
                QuestionEntry picked = PickQuestion(targetDifficulty);
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
        private QuestionEntry PickQuestion(int difficulty)
        {
            // Try exact difficulty first
            var candidates = _filteredPool
                .Where(q => q.difficulty == difficulty && !_usedIds.Contains(q.id))
                .ToList();

            // Fallback: nearest difficulty
            if (candidates.Count == 0)
            {
                for (int delta = 1; delta <= 4; delta++)
                {
                    candidates = _filteredPool
                        .Where(q => (q.difficulty == difficulty - delta || q.difficulty == difficulty + delta)
                                    && !_usedIds.Contains(q.id))
                        .ToList();
                    if (candidates.Count > 0) break;
                }
            }

            // Last resort: allow repeats
            if (candidates.Count == 0)
            {
                candidates = _filteredPool
                    .Where(q => q.difficulty == difficulty)
                    .ToList();

                if (candidates.Count == 0)
                    candidates = new List<QuestionEntry>(_filteredPool);
            }

            // Pick a random one from candidates
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
