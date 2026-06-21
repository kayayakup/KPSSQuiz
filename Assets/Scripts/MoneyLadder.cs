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

        private static readonly string[] ClassicPrizes = {
            "500", "1,000", "2,000", "3,000", "5,000",
            "7,500", "15,000", "30,000", "60,000", "125,000",
            "250,000", "500,000", "1,000,000", "2,500,000", "5,000,000"
        };

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
                if (steps == 15 && i < ClassicPrizes.Length)
                {
                    PrizeLabels[i] = "₺" + ClassicPrizes[i];
                }
                else
                {
                    PrizeLabels[i] = "Soru " + (i + 1);
                }
            }

            if (steps == 15)
            {
                SafeHaven1 = 4; // Step 5 (index 4)
                SafeHaven2 = 9; // Step 10 (index 9)
            }
            else
            {
                SafeHaven1 = -1;
                SafeHaven2 = -1;
            }
        }

        public static string GetGuaranteedPrize(int currentStep)
        {
            if (SafeHaven2 != -1 && currentStep > SafeHaven2)
            {
                return PrizeLabels[SafeHaven2];
            }
            if (SafeHaven1 != -1 && currentStep > SafeHaven1)
            {
                return PrizeLabels[SafeHaven1];
            }
            return "₺0";
        }
    }
}
