using System;
using UnityEngine;

namespace MillionaireGame
{
    public static class MoneyLadder
    {
        public static string[] PrizeLabels { get; private set; }
        public static int[] StepDifficulty { get; private set; }
        public static int SafeHaven1 { get; private set; }
        public static int SafeHaven2 { get; private set; }
        public static int TotalSteps => PrizeLabels != null ? PrizeLabels.Length : 0;

        static MoneyLadder()
        {
            // Default initialization
            Initialize(15);
        }

        public static void Initialize(int steps)
        {
            steps = Mathf.Clamp(steps, 1, 100);
            PrizeLabels = new string[steps];
            StepDifficulty = new int[steps];

            // Distribute difficulty 1 to 5 proportionally across steps
            for (int i = 0; i < steps; i++)
            {
                if (steps <= 1)
                {
                    StepDifficulty[i] = 3;
                }
                else
                {
                    float progress = (float)i / (steps - 1);
                    StepDifficulty[i] = Mathf.Clamp(Mathf.FloorToInt(progress * 5) + 1, 1, 5);
                }
            }

            for (int i = 0; i < steps; i++)
            {
                PrizeLabels[i] = "Soru " + (i + 1);
            }

            SafeHaven1 = -1; // No safe havens in exam mode
            SafeHaven2 = -1;
        }

        private static int ParsePrize(string text)
        {
            return 0; // Not needed
        }

        private static string FormatPrize(int amount)
        {
            return amount.ToString();
        }

        public static string GetGuaranteedPrize(int currentStep)
        {
            return "";
        }
    }
}
