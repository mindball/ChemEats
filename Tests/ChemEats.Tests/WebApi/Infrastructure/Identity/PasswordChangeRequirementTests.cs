using WebApi.Infrastructure.Identity;

namespace ChemEats.Tests.WebApi.Infrastructure.Identity;

public class PasswordChangeRequirementTests
{
    [Fact]
    public void GenerateTemporaryPassword_WhenCalled_ShouldSatisfyPasswordPolicy()
    {
        for (int i = 0; i < 200; i++)
        {
            string password = PasswordChangeRequirement.GenerateTemporaryPassword();

            Assert.True(password.Length >= 8);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(password, char.IsLower);
            Assert.True(password.Distinct().Count() >= 4);
        }
    }

    [Fact]
    public void GenerateTemporaryPassword_WhenCalledTwice_ShouldReturnDifferentValues()
    {
        string first = PasswordChangeRequirement.GenerateTemporaryPassword();
        string second = PasswordChangeRequirement.GenerateTemporaryPassword();

        Assert.NotEqual(first, second);
    }
}
