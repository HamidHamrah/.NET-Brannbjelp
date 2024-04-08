using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Ignist.Controllers;
using Ignist.Data;
using Ignist.Data.Services;
using Ignist.Models;
using Ignist.Models.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace UnitTests_Ignist.Controllers;

public class AuthControllerTests
{

    [Fact]
    public async Task TestRegister_Positive()
    {
        //testen simulerer et positivt scenario hvor en bruker blir registrert med gyldige data
        //sjekker om resultatet av testen er en OkObjectResult som forventet, og at verdien
        //av resultatet er "User registered."

        //arrange
        var registerModel = new RegisterModel() { UserName = "hanne" , LastName = "Larsen", Email = "hanne-lf@hotmail.com", Password = "passord123"};
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.RegisterUserAsync(registerModel))
            .ReturnsAsync("User registered.");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Register(registerModel);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
        Assert.Equal("User registered.", okResult.Value);
    }

    [Fact]
    public async Task TestRegister_Negative_UserNameIsNull()
    {
        //testen sjekker et negativt scenario hvor det oppstår et problem med registrering av user.
        //setter opp testen til å returnere "bad" (random valgt verdi)
        //sjekker til slutt at testen returnerer resultat av typen BadRequestObjectResult og at 
        //verdien til resultatet er "bad"

        //arrange
        var registerModel = new RegisterModel() { UserName = null, LastName = "", Email = "hanne-lf@hotmail.com", Password = "passord123" };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.RegisterUserAsync(registerModel))
            .ReturnsAsync("bad");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Register(registerModel);

        //assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("bad", badRequest.Value);
    }

    [Fact]
    public async Task TestRegister_Negative_InvalidEmail()
    {
        //testen sjekker et negativt scenario hvor det oppstår et problem med registrering av user.
        //setter opp testen til å returnere "bad" (random valgt verdi)
        //sjekker til slutt at testen returnerer resultat av typen BadRequestObjectResult og at 
        //verdien til resultatet er "bad"

        //arrange
        var registerModel = new RegisterModel() { UserName = "brukernavn", LastName = "petter", Email = "example.com", Password = "passord123" };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.RegisterUserAsync(registerModel))
            .ReturnsAsync("bad");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Register(registerModel);

        //assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("bad", badRequest.Value);
    }

    [Fact]
    public async Task TestRegister_Negative_InvalidModelState()
    {
        //oppretter registermodel med ugyldig data (username = null) og legger til en feil i modelstate
        //testen sjekker at resultatet er en BadRequestObjectResult og at verdien er av typen 
        //'SerializableError' som er en samling feil fra ModelState. 

        //arrange
        var registerModel = new RegisterModel() { UserName = null, LastName = "Larsen", Email = "hanne-lf@hotmail.com", Password = "passord123" };
        var authController = new AuthController(new Mock<ICosmosDbService>().Object);
        authController.ModelState.AddModelError("UserName", "Username is required.");

        //act
        var result = await authController.Register(registerModel);

        //assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequest.Value);
    }

    [Fact]
    public async Task TestLogin_Positive()
    {
        //en postiv test som oppretter en gyldig instans av loginmodel. 
        //testen simulerer at man forsøker å logge inn og at dette fungerer. 
        //sjekker at metoden returnerer result av type OkObjectResult og at verdien til denne er 
        //"valid_token", som vi satte opp i arrange-delen.

        //arrange
        var loginModel = new LoginModel() { email = "hanne-lf@hotmail.com", Password = "passord123" };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.LoginUserAsync(loginModel))
            .ReturnsAsync("valid_token");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Login(loginModel);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
        Assert.Equal("valid_token", okResult.Value);
    }

    [Fact]
    public async Task TestLogin_Negative_InvalidLoginDetails_EmailIsNull()
    {
        //sjekker at metoden gjør korrekt feilhåndtering dersom epost er null. 
        //sjekker at den returner en BadRequestObjectResult med korrekt feilmelding.

        //arrange
        var loginModel = new LoginModel() { email = null, Password = "123" };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.LoginUserAsync(loginModel))
            .ReturnsAsync("Missing or invalid login details.");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Login(loginModel);

        //assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Missing or invalid login details.", badRequest.Value);
    }

    [Fact]
    public async Task TestLogin_Negative_InvalidLoginDetails_InvalidEmail()
    {
        //metoden består ikke denne testen 

        //testen oppretter en instans av loginmodel hvor eposten ikke er gyldig, mangler @ i eposten
        //sjekker om metoden håndterer instans av ugyldig epost og returnerer en badrequest

        //arrange
        var loginModel = new LoginModel() { email = "example.com", Password = "fafffef123" };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.LoginUserAsync(loginModel))
            .ReturnsAsync("Missing or invalid login details.");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Login(loginModel);

        //assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Missing or invalid login details.", badRequest.Value);
    }

    [Fact]
    public async Task TestLogin_Negative_InvalidLoginDetails_PasswordIsNull()
    {
        //sjekker at metoden håndterer feil dersom passord er null
        //sjekker at metoden returnerer en BadRequestObjectResult og korrekt feilmelding

        //arrange
        var loginModel = new LoginModel() { email = "test@example.com", Password = null };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.LoginUserAsync(loginModel))
            .ReturnsAsync("Missing or invalid login details.");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Login(loginModel);

        //assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Missing or invalid login details.", badRequest.Value);
    }

    [Fact]
    public async Task TestLogin_Negative_InvalidPassword()
    {
        //sjekker at metoden håndterer feil hvor passord ikke er ugyldig, i dette tilfellet kun 
        //ett tegn 'g'. Testen sjekker at metoden returnerer melding "Invalid password." som antatt 
        //og en feilmelding av typen UnauthorizedObjectResult

        //arrange
        var loginModel = new LoginModel() { email = "hanne-lf@example.com", Password = "g" };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.LoginUserAsync(loginModel))
            .ReturnsAsync("Invalid password.");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Login(loginModel);

        //assert
        var unauthorizedObjectResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid password.", unauthorizedObjectResult.Value);
    }

    [Fact]
    public async Task TestLogin_Negative_UserNotFound()
    {
        //testen sjekker at metoden håndterer feil hvor en bruker ikke finnes
        //sjekker at metoden returnerer feilmelding "User not found." og et resultat av 
        //typen UnauthorizedObjectResult

        //arrange
        var loginModel = new LoginModel() {email = "finnesikke@epost.no", Password = "brapassord"};
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.LoginUserAsync(loginModel))
            .ReturnsAsync("User not found.");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.Login(loginModel);

        //assert
        var unauthorizedObjectResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User not found.", unauthorizedObjectResult.Value);
    }

    [Fact]
    public async Task TestForgotPassword_Positive()
    {
        //testen simulerer et scenario hvor eposten er registrert i systemet og metoden
        //returnerer korrekt melding. Sjekker om resultatet er av typen OkObjectResult
        //og om meldingen er som forventet. 

        //arrange
        var email = "epost@epost.no";
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.HandleForgotPasswordAsync(email))
            .ReturnsAsync("{ message = Password change request is sent to epost@epost.no. Please check your email for the reset code. }");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.ForgotPassword(email);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseString = okResult.Value.ToString();
        Assert.Equal("{ message = Password change request is sent to epost@epost.no. Please check your email for the reset code. }", responseString);
    }

    [Fact]
    public async Task TestForgotPassword_Negative_UserNotRegistered()
    {
        //sjekker at metoden håndterer situasjon hvor epost ikke er registrert
        //setter opp slik at metoden returner null for denne eposten, dvs. at bruker ikke er registrert
        //sjekker at metoden resulterer i en NotFoundObjectResult og at meldingen er som forventet.

        //arrange
        var email = "ikkereg@epost.no";
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.HandleForgotPasswordAsync(email))
            .ReturnsAsync((string)null);
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.ForgotPassword(email);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var responseString = notFoundResult.Value.ToString();
        Assert.Equal("{ message = User not registered. }", responseString);
    }

    [Fact]
    public async Task TestResetPassword_Positive()
    {
        //simulerer en request for å endre/reset passord
        //setter opp testen til å returnere at passord oppdateres vellykket og
        //sjekker at metoden returnerer forventet tilbakemelding og er av typen
        //OkObjectResult

        //arrange
        var resetPasswordRequest = new Ignist.Models.Authentication.ResetPasswordRequest { 
            Code = "998877", 
            Email = "hanne-lf@hotmail.com",
            NewPassword = "newPassword",
            ConfirmPassword = "newPassword"
        };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.HandleResetPasswordAsync(resetPasswordRequest))
            .ReturnsAsync("Password has been successfully reset.");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.ResetPassword(resetPasswordRequest);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Password has been successfully reset.", okResult.Value);
    }

    [Fact]
    public async Task TestResetPassword_Negative_BadRequest()
    {
        //Testen simulerer en request for å resette passord som ikke er gyldig
        //Sjekker at metoden håndterer dette ved å gi feilmelding og returnere
        //resultat av typen BadRequest

        //arrange
        var resetPasswordRequest = new Ignist.Models.Authentication.ResetPasswordRequest
        {
            Code = "998877",
            Email = "hanne-lf@hotmail.com"
        };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.HandleResetPasswordAsync(resetPasswordRequest))
            .ReturnsAsync("Didnt work");
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.ResetPassword(resetPasswordRequest);

        //assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Didnt work", badResult.Value);
    }

    [Fact]
    public async Task TestUpdatePassword_Positive()
    {
        //Testen simulerer et scenario hvor en bruker vil oppdatere passordet sitt.
        //I arrange-delen opprettes testdata som trengs, en brukeridentitet og en epostadresse
        //Kaller deretter metoden med testdataene i act-delen
        //I assert sjekkes det at metoden leverer det som er forventet, som er at det
        //returneres en "OkObjectREsult" og melding om at passordet ble oppdatert

        //arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "hanne-lf@hotmail.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var updatePassword = new UpdatePasswordModel { 
            OldPassword = "oldPassword", 
            NewPassword = "newPassword", 
            ConfirmNewPassword = "newPassword"};

        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.UpdateUserPasswordAsync(It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("Password updated successfully.");

        var authController = new AuthController(mockCosmosDbService.Object);
        authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

        //act
        var result = await authController.UpdatePassword(updatePassword);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Password updated successfully.", okResult.Value);
    }

    [Fact]
    public async Task TestUpdatePassword_Negative_InvalidModelState()
    {
        //Testen simulerer et scenario hvor modelstate ikke er gyldig, confirmpassord er ikke
        //lik som newpassword. Lagt til model-feil ved å kalle AddModelError i arrange-delen
        //Verifiserer at resultatet av updatePassword er en BadRequest med modell-feil (SerializableError)

        //arrange
        var updatePasswordModel = new UpdatePasswordModel() {
            OldPassword = "oldPassword",
            NewPassword = "newPassword",
            ConfirmNewPassword = "newPassword1"
        };
        var authController = new AuthController(new Mock<ICosmosDbService>().Object);
        authController.ModelState.AddModelError("ConfirmedPassword", "Something is wrong.");

        //act
        var result = await authController.UpdatePassword(updatePasswordModel);

        //assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequest.Value);
    }

    [Fact]
    public async Task TestUpdatePassword_Negative_InvalidToken() 
    { 
        //arrange
        //act
        //assert
    }

    [Fact]
    public async Task TestUpdatePassword_Negative_BadRequest()
    {
        //arrange
        //act
        //assert
    }
}
