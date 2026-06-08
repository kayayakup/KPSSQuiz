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

            // Distribute prizes logarithmically/exponentially up to $1,000,000
            double startPrize = 100;
            double endPrize = 1000000;
            double ratio = steps > 1 ? Math.Pow(endPrize / startPrize, 1.0 / (steps - 1)) : 1.0;

            for (int i = 0; i < steps; i++)
            {
                double val = startPrize * Math.Pow(ratio, i);
                int roundedVal;
                if (val < 1000)
                {
                    roundedVal = Mathf.RoundToInt((float)val / 100f) * 100;
                }
                else if (val < 10000)
                {
                    roundedVal = Mathf.RoundToInt((float)val / 500f) * 500;
                }
                else if (val < 100000)
                {
                    roundedVal = Mathf.RoundToInt((float)val / 1000f) * 1000;
                }
                else
                {
                    roundedVal = Mathf.RoundToInt((float)val / 25000f) * 25000;
                }
                
                // Ensure strict progression
                if (i > 0)
                {
                    int prevVal = ParsePrize(PrizeLabels[i - 1]);
                    if (roundedVal <= prevVal)
                    {
                        roundedVal = prevVal + 100;
                    }
                }
                
                PrizeLabels[i] = FormatPrize(roundedVal);
            }

            // Set safe havens at 1/3 and 2/3 of the way
            SafeHaven1 = steps / 3;
            SafeHaven2 = (2 * steps) / 3;
            
            // Adjust if they overlap or are out of bounds
            if (SafeHaven1 >= steps) SafeHaven1 = steps - 1;
            if (SafeHaven2 >= steps) SafeHaven2 = steps - 1;
            if (SafeHaven1 < 0) SafeHaven1 = 0;
            if (SafeHaven2 < 0) SafeHaven2 = 0;
        }

        private static int ParsePrize(string text)
        {
            string clean = text.Replace("$", "").Replace(",", "").Replace(".", "");
            if (int.TryParse(clean, out int val)) return val;
            return 0;
        }

        private static string FormatPrize(int amount)
        {
            return "$" + string.Format("{0:N0}", amount);
        }

        public static string GetGuaranteedPrize(int currentStep)
        {
            if (TotalSteps == 0) return "$0";
            if (currentStep > SafeHaven2 && SafeHaven2 < TotalSteps) return PrizeLabels[SafeHaven2];
            if (currentStep > SafeHaven1 && SafeHaven1 < TotalSteps) return PrizeLabels[SafeHaven1];
            return "$0";
        }
    }
}
