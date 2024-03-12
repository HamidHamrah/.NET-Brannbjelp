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

//Kan teste: Post, Delete og GetAllPublications()
//Update er ikke klar for testing 

namespace UnitTests_Ignist.Controllers;

public class PublicationControllerTests
{

    [Fact]
    public async Task TestGetAllPublications_Positive()
    {
        //Testen simulerer en liste med to publikasjoner i databasen. 
        //Tester at antall publikasjoner i listen er som forventet (2 stk i dette tilfellet).
        //Sjekker om verdien av OkObjectResult er av typen List<Publication> (forventet resultat av metoden)
        //Sjekker at resultatet er av typen OK\kObjekctResult, som indikerer at forespørselen er vellykket.
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
        //Denne testen sjekker hva som skjer når det ikke finnes noen publikasjoner.
        //Sjekker at metoden returnerer en tom liste når det ikke er noen i
        //databasen
        //*

        //arrange
        var emptyPublicationList = new List<Publication>();

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.GetAllPublicationsAsync()).ReturnsAsync(emptyPublicationList);
        var publicationController = new PublicationsController(mockPublicationRepository.Object);

        //act
        var result = await publicationController.GetAllPublications();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var publications = Assert.IsType<List<Publication>>(okResult.Value);
        Assert.Empty(publications);
    }

    [Fact]
    public async Task TestDeletePublication_Positive()
    {
        //Denne testen simulerer slettingen av en publikasjon som finnes i databasen.
        //Det forventes at metoden returnerer NoContent (HTTP-statuskode 204),
        //og at DeletePublicationAsync-metoden i repository blir kalt én gang med riktig
        //id og bruker-ID.
        //*

        //arrange
        var publicationId = "1";
        var userId = "user123";
        var publication = new Publication { Id = publicationId, Title = "Tittel", Content = "Content", UserId = userId };

        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        var publicationController = new PublicationsController(mockPublicationRepository.Object);
        mockPublicationRepository.Setup(repo => repo.AddPublicationAsync(publication));
                      
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
        var mockPublicationRepository = new Mock<IPublicationsRepository>();
        mockPublicationRepository.Setup(repo => repo.DeletePublicationAsync(It.IsAny<string>(), It.IsAny<string>()))
                                  .ThrowsAsync(new CosmosException("Publication not found.", HttpStatusCode.NotFound, 404, "Not Found", 0));

        var publicationController = new PublicationsController(mockPublicationRepository.Object);
        var publicationId = "1";
        var userId = "user123";

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
        var publicationController = new PublicationsController(
            mockPublicationRepository.Object);

        //act & assert
        await Assert.ThrowsAsync<Exception>(() => 
            publicationController.AddPublication(newPublication));
    }

 
}
