using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ignist.Models;

namespace UnitTests_Ignist.Models;

public class PublicationModelTests
{
    [Fact]
    public void TestTitleIsRequired()
    {
        //testen sjekker om "Required"-annotasjonen i publication-modellen virker
        //som den skal. Testen sjekker at valideringen feiler når en publikasjon
        //ikke har tittel. 

        //arrange
        var publication = new Publication { Title = null };

        //act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(publication, new ValidationContext(publication),
            validationResults, true);

        //assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Title"));
    }

    [Fact]
    public void TestContentIsRequired()
    {
        //Sjekker at required-annotasjon på 'content' fungerer som den skal

        //arrange
        var publication = new Publication { Content = null };

        //act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(publication, new ValidationContext(publication),
            validationResults, true);

        //assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Content"));
    }


}
