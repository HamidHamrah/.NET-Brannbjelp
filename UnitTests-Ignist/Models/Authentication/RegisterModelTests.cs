using Ignist.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests_Ignist.Models.Authentication;

public class RegisterModelTests
{
    [Fact]
    public void TestUsernameIsRequired()
    {
        //arrange
        var registerModel = new RegisterModel { UserName = null };

        //act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(registerModel, new ValidationContext(registerModel),
            validationResults, true);

        //assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains("UserName"));
    }

    [Fact]
    public void TestLastnameIsRequired()
    {
        //arrange
        var registerModel = new RegisterModel { LastName = null };

        //act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(registerModel, new ValidationContext(registerModel),
            validationResults, true);

        //assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains("LastName"));
    }

    [Fact]
    public void TestEmailIsRequired()
    {
        //arrange
        var registerModel = new RegisterModel { Email = null };

        //act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(registerModel, new ValidationContext(registerModel),
            validationResults, true);

        //assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Email"));
    }

    //test for invalid epost, test for valid epost

    [Fact]
    public void TestPasswordIsRequired()
    {
        //arrange
        var registerModel = new RegisterModel { Password = null };

        //act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(registerModel, new ValidationContext(registerModel),
            validationResults, true);

        //assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Password"));
    }
}
