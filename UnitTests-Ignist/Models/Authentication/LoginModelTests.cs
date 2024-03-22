using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ignist.Models;

namespace UnitTests_Ignist.Models.Authentication;

public class LoginModelTests
{
    [Fact]
    public void TestEmailIsRequired()
    {
        //testen sjekker om "Required"-annotasjonen i login-modellen virker
        //som den skal. Testen sjekker at valideringen feiler når en login
        //ikke har email.

        //arrange
        var login = new LoginModel { email = null };

        //act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(login, new ValidationContext(login),
            validationResults, true);

        //assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains("email"));
    }
}
