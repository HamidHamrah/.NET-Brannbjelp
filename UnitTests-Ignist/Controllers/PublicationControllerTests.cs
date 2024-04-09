using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ignist.Controllers;
using Ignist.Data;
using Ignist.Models;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace UnitTests_Ignist.Controllers;

public class PublicationControllerTests
{

    [Fact]
    public async Task TestGetAllPublications_Positive()
    {
        //Testen simulerer en liste med to publikasjoner i databasen. 
        //Tester at antall publikasjoner i listen er som forventet (2 stk i dette tilfellet).
        //Sjekker om verdien av OkObjectResult er av typen List<Publication> (forventet resultat av metoden)
        //Sjekker at resultatet er av typen OkObjekctResult, som indikerer at forespørselen er vellykket.
        //Sjekker om listen som opprettes i "arrange" er lik listen som returneres fra metoden.  

        //arrange
        var publicationList = new List<Publication>()
            {
                new Publication
                {
                    Id = "1",
                    Title = "Tittel publikasjon",
                    Content = "plds duedn edjandks nf",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UserId = "user123",
                    ParentId = "parent123",
                    ChildPublications = new List<Publication>()
                },
                new Publication
                {
                    Id = "2",
                    Title = "Hovedbygning brann",
                    Content = "hfek fef efkf"
                }
            };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.GetAllPublicationsAsync())
            .ReturnsAsync(publicationList);

        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.GetAllPublications();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var publications = Assert.IsType<List<Publication>>(okResult.Value);
        Assert.Equal(2, publications.Count);
        Assert.Equal(publicationList, publications);
    }

    [Fact]
    public async Task TestGetAllPublications_Negative()
    {
        //Sjekker at metoden håndterer feil ved henting av publikasjoner
        //Testen fremprovoserer en exception og sjekker at metoden kaster exception og 
        //gir feilmelding. Sjekker også at det gis statuskode 500 internal server error.

        //arrange
        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.GetAllPublicationsAsync())
            .ThrowsAsync(new Exception("Mocked exception"));

        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.GetAllPublications();

        //assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while retrieving publications. please try again later.", statusCodeResult.Value);
    }

    /* -- kommentert ut to tester som var skrevet for metoder som nå er sletta i controlleren
    
    [Fact]
    public async Task TestGetLastPublication_Positive()
    {
        //Denne testen sjekker at metoden returnerer riktig verdi (OkObjectResult)
        //og at id'en som returneres er den forventede Id'en

        //arrange
        var newPublication = new Publication
        {
            Id = "1",
            Title = "Tittel publikasjon",
            Content = "plds duedn edjandks nf",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UserId = "user123",
            ParentId = "parent123"
        };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.GetLastPublicationAsync()).
            ReturnsAsync(newPublication);
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.GetLastPublication();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(newPublication.Id, okResult.Value);
    }

    [Fact]
    public async Task TestGetLastPublication_Negative()
    {
        //Testen sjekker at metoden returnerer NotFoundObject dersom det ikke er noen
        //publikasjoner i mock-databasen. 

        //arrange
        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.GetLastPublicationAsync());
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.GetLastPublication();

        //assert
        var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundObjectResult.StatusCode);
    }
    */

    [Fact]
    public async Task TestGetPublication_Positive()
    {
        //Testen sjekker at metoden returnerer korrekt Id 

        //arrange
        var newPublication = new Publication
        {
            Id = "1",
            Title = "Tittel publikasjon",
            Content = "plds duedn edjandks nf",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UserId = "user123",
            ParentId = "parent123"
        };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.GetPublicationByIdAsync(newPublication.Id, newPublication.UserId)).ReturnsAsync(newPublication);
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.GetPublication(newPublication.Id, newPublication.UserId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var publication = Assert.IsType<Publication>(okResult.Value);
        Assert.Equal(newPublication.Id, publication.Id);
    }

    [Fact]
    public async Task TestGetPublication_Negative()
    {
        //Testen simulerer at vi leter etter userId og publicationId som ikke finnes i databasen
        //Sjekker at metoden da returnerer feilmelding og Assert av typen 'NotFoundObjectResult'

        //arrange
        var userId = "user123wrong";
        var publicationId = "wrong";
        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.GetPublicationByIdAsync(publicationId, userId)).ReturnsAsync((Publication?)null);
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.GetPublication(publicationId, userId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var expectedMessage = "Publication not found.";
        Assert.Equal(expectedMessage, notFoundResult.Value);
    }


    [Fact]
    public async Task TestDeletePublication_Positive()
    {
        //Denne testen simulerer slettingen av en publikasjon
        //Det forventes at metoden returnerer NoContent (HTTP-statuskode 204),
        //og at DeletePublicationAsync-metoden i repository blir kalt én gang med riktig
        //id og bruker-ID.

        //arrange
        var publicationId = "1";
        var userId = "user123";
        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.DeletePublicationAsync(publicationId, userId));
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.DeletePublication(publicationId, userId);

        //assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal((int)HttpStatusCode.NoContent, noContentResult.StatusCode);
        mockPublicationRepository.Verify(repo => repo.DeletePublicationAsync(publicationId, userId), Times.Once);
    }

    [Fact]
    public async Task TestDeletePublication_Negative()
    {
        //Denne testen simulerer slettingen av en publikasjon som ikke finnes i databasen.
        //Det forventes at metoden returnerer NotFound (HTTP-statuskode 404) med en
        //passende feilmelding, og at DeletePublicationAsync-metoden i repository kaster
        //en CosmosException med statuskoden for "Not Found".

        //arrange
        var publicationId = "1";
        var userId = "user123";
        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.DeletePublicationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new CosmosException("Publication not found.", HttpStatusCode.NotFound, 404, "Not Found", 0));

        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        // act
        var result = await publicationController.DeletePublication(publicationId, userId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Publication not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task TestAddPublication_Positive()
    {
        //Testen oppretter en ny publikasjon, simulerer en forespørsel om å legge
        //den til.
        //Testen sjekker om responsen er en CreatedAtActionResult.

        //arrange
        var newPublication = new Publication
        {
            Id = "1",
            Title = "Ny publikasjon",
            Content = "Dette er innholdet i den nye publikasjonen.",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UserId = "user123",
            ParentId = "parent123",
            ChildPublications = new List<Publication>()
        };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.AddPublication(newPublication);

        //assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(newPublication, createdAtActionResult.Value);
    }

    [Fact]
    public async Task TestAddPublication_Negative()
    {
        //Simulerer en situasjon hvor det oppstår feil når man skal legge til en publikasjon
        //i databasen.Tester at metoden kaster en exeption med korrekt feilmelding

        //arrange
        var newPublication = new Publication
        {
            Id = "1",
            Title = "Titteli",
            Content = "Dette er innholdet i publikasjonen",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UserId = "user123",
            ParentId = "parent123",
            ChildPublications = new List<Publication>()
        };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.AddPublicationAsync(newPublication)).
            ThrowsAsync(new Exception("Something went wrong when adding new publication."));
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act & assert
        await Assert.ThrowsAsync<Exception>(() =>
            publicationController.AddPublication(newPublication));
    }

    [Fact]
    public async Task TestUpdatePublication_Positive()
    {
        //Sjekker at update-metoden returnerer korrekt type respons og at UpdatePublicationAsync-metoden
        //kalles med riktig publikasjon

        //arrange
        string id = "1";
        var newPublication = new Publication
        {
            Id = id,
            Title = "tittel",
            Content = "Oppdaterer innholdet",
            UserId = "user123",
            ParentId = "parent123",
        };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.UpdatePublicationAsync(newPublication));
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.UpdatePublication(id, newPublication);

        //assert
        mockPublicationRepository.Verify(repo => repo.UpdatePublicationAsync(newPublication), Times.Once);
        Assert.IsType<ActionResult<Publication>>(result);

    }

    [Fact]
    public async Task TestUpdatePublication_InvalidId()
    {
        //Testen forsøker å oppdatere en publikasjon, men med feil id. 
        //Sjekker at metoden returnerer feilmelding, returnerer assert av type
        //'BadRequest', og at updatemetoden ikke gjennomføres.

        //arrange
        string id = "1";
        string invalidId = "2";
        var updatedPublication = new Publication
        {
            Id = id,
            Title = "Updated Title",
            Content = "Updated Content",
            UserId = "user123",
            ParentId = "parent123",
        };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.UpdatePublication(invalidId, updatedPublication);

        //assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("The ID of the publication does not match the request.", badRequestResult.Value);
        mockPublicationRepository.Verify(repo => repo.UpdatePublicationAsync(updatedPublication), Times.Never);

    }

    [Fact]
    public async Task UpdatePublication_PublicationNotFound()
    {
        //En test som sjekker tilfellet dersom publikasjonen ikke blir funnet i databasen
        //Testen sjekker at metoden returnerer en CosmosException og NotFound status/respons

        //arrange
        string id = "1";
        var updatedPublication = new Publication
        {
            Id = id,
            Title = "Updated Title",
            Content = "Updated Content",
            UserId = "user123",
            ParentId = "parent123",
        };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.UpdatePublicationAsync(updatedPublication))
            .ThrowsAsync(new CosmosException("Publication not found.", HttpStatusCode.NotFound, 404, "", 1.0));

        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.UpdatePublication(id, updatedPublication);

        //assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
        mockPublicationRepository.Verify(repo => repo.UpdatePublicationAsync(updatedPublication), Times.Once);
    }



}
