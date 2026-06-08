using UnityEngine;

/// <summary>
/// Defines the 15‑step prize ladder and the two guaranteed safe‑haven levels.
/// </summary>
namespace MillionaireGame
{
    public static class MoneyLadder
    {
        // ── 15 prize steps (index 0 = question 1) ──
        public static readonly string[] PrizeLabels = new string[]
        {
            "$100",         // Step  1  – Difficulty 1
            "$200",         // Step  2  – Difficulty 1
            "$300",         // Step  3  – Difficulty 1
            "$500",         // Step  4  – Difficulty 2
            "$1,000",       // Step  5  – Difficulty 2  ★ Safe haven
            "$2,000",       // Step  6  – Difficulty 2
            "$4,000",       // Step  7  – Difficulty 3
            "$8,000",       // Step  8  – Difficulty 3
            "$16,000",      // Step  9  – Difficulty 3
            "$32,000",      // Step 10  – Difficulty 4  ★ Safe haven
            "$64,000",      // Step 11  – Difficulty 4
            "$125,000",     // Step 12  – Difficulty 4
            "$250,000",     // Step 13  – Difficulty 5
            "$500,000",     // Step 14  – Difficulty 5
            "$1,000,000"    // Step 15  – Difficulty 5
        };

        // Map each step index → required question difficulty (1‑5)
        public static readonly int[] StepDifficulty = new int[]
        {
            1, 1, 1,        // Steps 1‑3
            2, 2, 2,        // Steps 4‑6
            3, 3, 3,        // Steps 7‑9
            4, 4, 4,        // Steps 10‑12
            5, 5, 5         // Steps 13‑15
        };

        // Safe‑haven indices (0‑based). If the player fails, they keep this amount.
        public static readonly int SafeHaven1 = 4;   // $1,000
        public static readonly int SafeHaven2 = 9;   // $32,000

        public static int TotalSteps => PrizeLabels.Length;   // 15

        /// <summary>
        /// Returns the guaranteed prize the player keeps after a wrong answer.
        /// </summary>
        public static string GetGuaranteedPrize(int currentStep)
        {
            if (currentStep > SafeHaven2) return PrizeLabels[SafeHaven2];
            if (currentStep > SafeHaven1) return PrizeLabels[SafeHaven1];
            return "$0";
        }
    }
}
