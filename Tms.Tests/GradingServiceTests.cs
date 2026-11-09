using Xunit;
using Tms.Api.Services;

namespace Tms.Tests;

public class GradingServiceTests
{
    [Fact]
    public void CalculateFinalGrade_HighScores_ReturnsDistinction()
    {
        // Arrange
        var service = new GradingService();
        var scores = new AssessmentScore { MidtermScore = 45, FinalScore = 50 };

        // Act
        var result = service.CalculateFinalGrade(scores);

        // Assert
        Assert.Equal(GradeLevel.Distinction, result);
    }

    [Theory]
    [InlineData(0, 0, GradeLevel.Fail)]
    [InlineData(30, 20, GradeLevel.Pass)]
    [InlineData(45, 50, GradeLevel.Distinction)]
    [InlineData(-1, 50, GradeLevel.Invalid)] // Boundary: negative score
    [InlineData(51, 50, GradeLevel.Invalid)] // Boundary: exceeds max
    public void CalculateFinalGrade_VariousInputs_ReturnsExpectedLevel(
        int midterm, int final, GradeLevel expected)
    {
        // Arrange
        var service = new GradingService();
        var data = new AssessmentScore { MidtermScore = midterm, FinalScore = final };

        // Act
        var result = service.CalculateFinalGrade(data);

        // Assert
        Assert.Equal(expected, result);
    }
}
