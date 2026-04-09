namespace ComplaintClassifier.Application.Utilities;

public static class ConfidenceCalculator
{
    public static double Compute(int winnerScore, int secondScore, int totalScore)
    {
        if (winnerScore <= 0 || totalScore <= 0)
        {
            return 0;
        }

        var dominance = (double)winnerScore / totalScore;
        var gapFactor = winnerScore == 0 ? 0 : (double)(winnerScore - secondScore) / winnerScore;
        var confidence = 0.55 + (dominance * 0.30) + (gapFactor * 0.25);

        return Math.Round(Math.Clamp(confidence, 0.0, 0.99), 2);
    }
}
