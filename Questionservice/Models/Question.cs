using System.ComponentModel.DataAnnotations;

namespace Questionservice.Models
{
    public class Question
    {
        [MaxLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [MaxLength(300)]
        public required string Title { get; set; }
        [MaxLength(5000)]
        public required string Content { get; set; }
        [MaxLength(36)]
        public required string AskerId { get; set; }
        [MaxLength(300)]
        public required string AskerDisplayName { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdateAt { get; set; }
        public int viewCount { get; set; }
        public List<string> TagSlugs { get; set; } = [];
        public bool HasAcceptedAnswer { get; set; }
        public int Votes { get; set; }
    }
}
