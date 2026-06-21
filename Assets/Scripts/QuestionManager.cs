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
        private string NormalizeCategory(string cat)
        {
            if (string.IsNullOrWhiteSpace(cat)) return string.Empty;

            string raw = cat.Trim().ToLowerInvariant();

            // Strip prefixes
            raw = raw.Replace("kpss_ortaogretim_", "")
                     .Replace("kpss_onlisans_", "")
                     .Replace("kpss_lisans_", "")
                     .Replace("kpss_oabt_", "")
                     .Replace("dhbt_", "")
                     .Replace("kpss_", "");

            // Exact or substring matching
            if (raw.Contains("genel_kultur") || raw.Contains("general_culture") || raw.Contains("general culture")) return "Genel Kültür";
            if (raw.Contains("genel_yetenek") || raw.Contains("general_ability") || raw.Contains("general ability")) return "Genel Yetenek";
            if (raw.Contains("egitim_bilimleri") || raw.Contains("educational_sciences") || raw.Contains("educational sciences")) return "Eğitim Bilimleri";
            if (raw.Contains("turkce") || raw.Contains("turkish")) return "Türkçe";
            if (raw.Contains("matematik") || raw.Contains("mathematics")) return "Matematik";
            if (raw.Contains("tarih") || raw.Contains("history")) return "Tarih";
            if (raw.Contains("cografya") || raw.Contains("geography")) return "Coğrafya";
            if (raw.Contains("vatandaslik") || raw.Contains("citizenship") || raw.Contains("civics")) return "Vatandaşlık";
            if (raw.Contains("din") || raw.Contains("religion")) return "Din Kültürü";
            if (raw.Contains("guncel") || raw.Contains("güncel") || raw.Contains("general_knowledge")) return "Güncel Bilgiler";
            if (raw.Contains("hukuk") || raw.Contains("law")) return "Hukuk";
            if (raw.Contains("iktisat") || raw.Contains("economics")) return "İktisat";
            if (raw.Contains("isletme") || raw.Contains("business")) return "İşletme";
            if (raw.Contains("maliye") || raw.Contains("finance")) return "Maliye";
            if (raw.Contains("muhasebe") || raw.Contains("accounting")) return "Muhasebe";
            if (raw.Contains("istatistik") || raw.Contains("statistics")) return "İstatistik";
            if (raw.Contains("kamu_yonetimi") || raw.Contains("public_administration")) return "Kamu Yönetimi";
            if (raw.Contains("uluslararasi_iliskiler") || raw.Contains("international_relations")) return "Uluslararası İlişkiler";
            if (raw.Contains("calisma_ekonomisi") || raw.Contains("labor_economics")) return "Çalışma Ekonomisi";
            if (raw.Contains("geometri")) return "Geometri";

            // Standardize combined General Culture + General Ability label
            if (raw.Contains("genel kültür") && raw.Contains("genel yetenek"))
                return "Genel Kültür ve Genel Yetenek";

            // Standardize field/domain categories for Önlisans
            if (raw == "alan dersleri" || raw == "alan")
                return "Alan Dersleri";

            // Fallback: title-case the trimmed original string
            string trimmed = cat.Trim();
            if (trimmed.Length > 0)
                trimmed = char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);

            return trimmed;
        }

        public string GetQuestionCategory(QuestionEntry q)
        {
            if (q == null) return string.Empty;
            string raw = null;
            if (!string.IsNullOrWhiteSpace(q.subcategory))
                raw = q.subcategory;
            else if (!string.IsNullOrWhiteSpace(q.subject))
                raw = q.subject;
            else if (!string.IsNullOrWhiteSpace(q.category))
                raw = q.category;

            return NormalizeCategory(raw);
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
                        }
                        _database.questions.AddRange(dbPart.questions.Where(q =>
                            !isGlobalFile || string.Equals(q.examType, branchPrefix, System.StringComparison.OrdinalIgnoreCase)
                        ));
                    }
                }
            }

            if (_database.questions.Count > 0)
            {
                // Collect unique categories based on the most specific field of loaded questions.
                var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                var uniqueCategories = new List<string>();

                foreach (var q in _database.questions)
                {
                    string cat = GetQuestionCategory(q);
                    if (!string.IsNullOrEmpty(cat))
                    {
                        if (seen.Add(cat))
                        {
                            uniqueCategories.Add(cat);
                        }
                    }
                }

                AvailableCategories = uniqueCategories
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
                    .Where(q => GetQuestionCategory(q).Equals(category, System.StringComparison.OrdinalIgnoreCase))
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
