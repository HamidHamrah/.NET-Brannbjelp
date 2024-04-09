using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Moq;
using User = Ignist.Models.User;

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
        //simulerer et scenrio hvor epost-adresse er tom og ikke gyldig
        //sjekker at metoden gir et resultat av type UnauthorizedObjectResul som forventet
        //og gir feilmelding "Invalid Token"

        //arrange
        var userEmail = ""; 
        var updatePasswordModel = new UpdatePasswordModel()
        {
            OldPassword = "oldPassword",
            NewPassword = "newPassword",
            ConfirmNewPassword = "newPassword"
        };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        var authController = new AuthController(mockCosmosDbService.Object);

        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);
        authController.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
        };

        //act
        var result = await authController.UpdatePassword(updatePasswordModel);

        //assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid token.", unauthorizedResult.Value);
    }

    [Fact]
    public async Task TestUpdatePassword_Negative_BadRequest()
    {
        //Testen skal simulere et scenario hvor bruker har kommet helt til siste steg, men
        //noe feiler i selve oppdateringen av passordet. Altså ikke ugyldig epost eller bruker
        //men noe annet som går galt. 
        //sjekker at metoden da returnerer en badrequest 

        //arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "hanne-lf@hotmail.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var updatePassword = new UpdatePasswordModel
        {
            OldPassword = "oldPassword",
            NewPassword = "newPassword",
            ConfirmNewPassword = "newPassword"
        };

        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.UpdateUserPasswordAsync(It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("Bad request.");

        var authController = new AuthController(mockCosmosDbService.Object);
        authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        //act
        var result = await authController.UpdatePassword(updatePassword);

        //assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Bad request.", badResult.Value);
    }

    //Testen feiler - det er ikke av typen User som returneres av metoden
    [Fact]
    public async Task TestAboutMe_Positive()
    {
        //Testen oppretter en gyldig bruker og setter opp slik at metoden returnerer denne
        //brukeren. I Assert sjekkes det om metoden returnerer en OkObjectResult-respons og
        //at det er av typen User som returneres. Sjekker deretter om verdiene i brukeren 
        //som returneres er like som i brukeren vi opprettet i Arrange. 
        
        //arrange
        var user = new Ignist.Models.User
        {
            Email = "hanne-lf@hotmail.com",
            LastName = "Broen",
            UserName = "hannelf",
            Id = "926"
        };
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.AboutMe(user.Id);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userModel = Assert.IsType<Ignist.Models.User>(okResult.Value);
        Assert.Equal(user.Id, userModel.Id);
        Assert.Equal(user.UserName, userModel.UserName);
        Assert.Equal(user.LastName, userModel.LastName);
        Assert.Equal(user.Email, userModel.Email);
    }

    [Fact]
    public async Task TestAboutMe_Negative_IdIsNull()
    {
        //Testen simulerer et scenario hvor id er en tom streng 
        //Setter opp db til å ta inn denne tomme id'en og returnere en tom bruker 
        //I assert sjekkes det at resultatet er en BadRequest og at korrekt feilmelding gis

        //arrange
        var id = "";
        var emptyUser = new Ignist.Models.User();
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.GetUserByIdAsync(id))
            .ReturnsAsync(emptyUser);
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.AboutMe(id);

        //assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ID is required.", badRequest.Value);
    }

    [Fact]
    public async Task TestAboutMe_Negative_UserNotFound_EmptyUser()
    {
        //testen simulerer et scenario hvor brukeren ikke finnes i databasen
        //sjekker at metoden returnerer notFound og korrekt feilmelding

        //arrange
        var id = "123";
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.GetUserByIdAsync(id))
            .ReturnsAsync((User)null);
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.AboutMe(id);

        //assert
        var badRequest = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found.", badRequest.Value);
        
    }

    [Fact]
    public async Task TestGetAllUsers_Positive()
    {
        //Setter opp en test med to brukere 
        //Sjekker at metoden returnerer en liste med typen Users og at det er 2
        //brukere i denne listen og at brukerene er de samme som ble opprettet i arrange

        //arrange
        var users = new List<User>()
            {
                new User
                {
                    Id = "1",
                    UserName = "Brukernavn",
                    LastName = "Bojang",
                    Email = "example@example.com"
                },
                new User
                {
                    Id = "2",
                    UserName = "Bruker 2",
                    LastName = "Felix",
                    Email = "felix@example.com"
                }
            };

        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.GetAllUsersAsync())
            .ReturnsAsync(users);

        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.GetAllUsers();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualUsers = Assert.IsType<List<User>>(okResult.Value);
        Assert.Equal(2, actualUsers.Count);
        Assert.Equal(users, actualUsers);
    }


    [Fact]
    public async Task TestGetAllUsers_Negative()
    {
        //Sjekker at metoden håndterer feil ved henting av brukere
        //Testen fremprovoserer en exception og sjekker at metoden kaster exception og 
        //gir feilmelding. 

        //arrange
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.GetAllUsersAsync())
            .ThrowsAsync(new Exception("Mocked exception"));

        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.GetAllUsers();

        //assert
        var statusCodeResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while retrieving users: Mocked exception", statusCodeResult.Value);
    }

    [Fact]
    public async Task TestDeleteUser_Positive()
    {
        //setter opp scenario for sletting av en bruker
        //verifiserer at slette-metoden kalles med korrekt id
        //sjekker at metoden returnerer OkObjectResult og melding om at bruker er slettet suksessfult

        //arrange
        var id = "123";
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.DeleteUserAsync(id)).Verifiable(); //sjekker at metoden blir kalt med riktig id
        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.DeleteUser(id);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User deleted successfully.", okResult.Value);
        mockCosmosDbService.Verify(); //sjekker at DeleteUserAsync blir kalt med riktig id
    }

    [Fact]
    public async Task TestDeleteUser_Negative_NotFound()
    {
        //Setter opp et scenario hvor metoden ikke finner brukeren 
        //Sjekker at metoden håndterer dette med korrekt feilmelding og NotFound-result

        //arrange
        var id = "1";
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.DeleteUserAsync(id))
            .ThrowsAsync(new ArgumentException("User not found"));

        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.DeleteUser(id);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", notFoundResult.Value);
    }

    [Fact]
    public async Task TestDeleteUser_Negative_BadRequest()
    {
        //Setter opp scenario som fremprovoserer et Exception med en exception-melding
        //Sjekker i Assert at metoden returnerer denne meldingen og returnerer en BadRequest
        
        //arrange
        var id = "4443";
        var exceptionMessage = "Internal server error";
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        mockCosmosDbService.Setup(repo => repo.DeleteUserAsync(id))
            .ThrowsAsync(new Exception(exceptionMessage)); 

        var authController = new AuthController(mockCosmosDbService.Object);

        //act
        var result = await authController.DeleteUser(id);

        //assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal($"An error occurred: {exceptionMessage}", badRequestResult.Value);

    }

    [Fact]
    public async Task TestUpdateUser_Positive()
    {
        //arrange
        //act
        //assert
    }

    [Fact]
    public async Task TestUpdateUser_Negative_UserIdEmpty()
    {
        //arrange
        //act
        //assert
    }

    [Fact]
    public async Task TestUpdateUser_Negative_UpdateModelEmpty()
    {
        //arrange
        //act
        //assert
    }

    [Fact]
    public async Task TestUpdateUser_Negative_ErrorDuringUpdate()
    {
        //arrange
        //act
        //assert
    }

}

