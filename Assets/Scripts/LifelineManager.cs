using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the three lifelines: 50:50, Ask the Audience, Phone a Friend.
/// Each lifeline can only be used once per game.
/// </summary>
namespace MillionaireGame
{
    public class LifelineManager : MonoBehaviour
    {
        // ── Lifeline availability ──
        private bool _fiftyFiftyUsed;
        private bool _askAudienceUsed;
        private bool _phoneUsed;

        public bool FiftyFiftyAvailable => !_fiftyFiftyUsed;
        public bool AskAudienceAvailable => !_askAudienceUsed;
        public bool PhoneAvailable => !_phoneUsed;

        // ─────────────────────────────────────────────
        // Reset for new game
        // ─────────────────────────────────────────────
        public void ResetLifelines()
        {
            _fiftyFiftyUsed = false;
            _askAudienceUsed = false;
            _phoneUsed = false;
        }

        // ─────────────────────────────────────────────
        // 50:50 – returns the indices of the TWO answers to KEEP
        //         (the correct one + one random wrong one)
        // ─────────────────────────────────────────────
        public List<int> UseFiftyFifty(QuestionEntry question)
        {
            if (_fiftyFiftyUsed)
            {
                Debug.LogWarning("[LifelineManager] 50:50 already used.");
                return null;
            }

            _fiftyFiftyUsed = true;

            int correct = question.correctAnswerIndex;

            // Collect wrong answer indices
            List<int> wrongIndices = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                if (i != correct) wrongIndices.Add(i);
            }

            // Pick one random wrong answer to KEEP
            int keepWrong = wrongIndices[Random.Range(0, wrongIndices.Count)];

            // Return the two indices to keep visible
            return new List<int> { correct, keepWrong };
        }

        // ─────────────────────────────────────────────
        // Ask the Audience – returns float[4] percentages (sum = 100)
        // Correct answer gets 50‑85 %, rest is distributed randomly.
        // ─────────────────────────────────────────────
        public float[] UseAskAudience(QuestionEntry question)
        {
            if (_askAudienceUsed)
            {
                Debug.LogWarning("[LifelineManager] Ask the Audience already used.");
                return null;
            }

            _askAudienceUsed = true;

            float[] percentages = new float[4];
            int correct = question.correctAnswerIndex;

            // Give the correct answer a high share
            float correctShare = Random.Range(50f, 85f);
            percentages[correct] = correctShare;

            // Distribute the remainder among the wrong answers
            float remaining = 100f - correctShare;
            List<int> wrongIndices = new List<int>();
            for (int i = 0; i < 4; i++)
                if (i != correct) wrongIndices.Add(i);

            for (int i = 0; i < wrongIndices.Count; i++)
            {
                if (i == wrongIndices.Count - 1)
                {
                    // Last one gets whatever is left (avoids rounding issues)
                    percentages[wrongIndices[i]] = Mathf.Max(0, remaining);
                }
                else
                {
                    float share = Random.Range(0f, remaining);
                    percentages[wrongIndices[i]] = Mathf.Round(share);
                    remaining -= percentages[wrongIndices[i]];
                }
            }

            // Round for display
            for (int i = 0; i < 4; i++)
                percentages[i] = Mathf.Round(percentages[i]);

            return percentages;
        }

        // ─────────────────────────────────────────────
        // Phone a Friend – returns a string with the friend's "opinion".
        // 70 % chance the friend suggests the correct answer.
        // ─────────────────────────────────────────────
        public string UsePhoneFriend(QuestionEntry question)
        {
            if (_phoneUsed)
            {
                Debug.LogWarning("[LifelineManager] Phone a Friend already used.");
                return null;
            }

            _phoneUsed = true;

            bool friendCorrect = Random.value <= 0.70f;
            int suggestedIndex;

            if (friendCorrect)
            {
                suggestedIndex = question.correctAnswerIndex;
            }
            else
            {
                // Pick a random wrong answer
                List<int> wrong = new List<int>();
                for (int i = 0; i < 4; i++)
                    if (i != question.correctAnswerIndex) wrong.Add(i);
                suggestedIndex = wrong[Random.Range(0, wrong.Count)];
            }

            string letter = new string[] { "A", "B", "C", "D" }[suggestedIndex];
            string[] friendLines = new string[]
            {
                $"Hmm… I'm fairly sure it's {letter}: {question.answers[suggestedIndex]}.",
                $"I'd go with {letter}: {question.answers[suggestedIndex]}. Good luck!",
                $"I think the answer is {letter}: {question.answers[suggestedIndex]}.",
                $"My best guess is {letter}: {question.answers[suggestedIndex]}.",
                $"I remember reading about this — try {letter}: {question.answers[suggestedIndex]}."
            };

            return friendLines[Random.Range(0, friendLines.Length)];
        }
    }
}
