namespace Tms.Api.Services;

public enum GradeLevel
{
    Invalid,
    Fail,
    Pass,
    Distinction
}

public class AssessmentScore
{
    public int MidtermScore { get; set; }
    public int FinalScore { get; set; }
}

public class GradingService
{
    public GradeLevel CalculateFinalGrade(AssessmentScore scores)
    {
        if (scores.MidtermScore < 0 || scores.MidtermScore > 50 || scores.FinalScore < 0 || scores.FinalScore > 50)
            return GradeLevel.Invalid;

        var total = scores.MidtermScore + scores.FinalScore;

        if (total < 40) return GradeLevel.Fail;
        if (total < 75) return GradeLevel.Pass;
        return GradeLevel.Distinction;
    }
}
