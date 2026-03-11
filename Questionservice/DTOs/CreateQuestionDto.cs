using Questionservice.Validators;
using System.ComponentModel.DataAnnotations;

namespace Questionservice.DTOs
{
    public record CreateQuestionDto(
        [Required]string Title, 
        [Required]string Content,
        [Required][TagListValidator(1,5)] List<string> Tags
    );
}
