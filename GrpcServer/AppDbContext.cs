using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Dla Key
using System.ComponentModel.DataAnnotations.Schema; // Dla DatabaseGenerated

namespace QuizApp
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<DbQuiz> Quizzes { get; set; }
        public DbSet<DbQuestion> Questions { get; set; }
        public DbSet<DbAnswer> Answers { get; set; }
        public DbSet<DbGame> Games { get; set; }
        public DbSet<DbPlayer> Players { get; set; }
        public DbSet<DbAnswerSubmission> AnswerSubmissions { get; set; }

      
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbQuiz>()
          .HasKey(q => q.Id);

            modelBuilder.Entity<DbQuestion>()
                .HasKey(q => q.Id);

            // Quiz -> Questions relationship (one-to-many)
            modelBuilder.Entity<DbQuestion>()
                .HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DbAnswer>()
                .HasKey(a => a.Id);

            // Question -> Answers relationship (one-to-many)
            modelBuilder.Entity<DbAnswer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DbGame>()
                .HasKey(g => g.GameId);

            modelBuilder.Entity<DbGame>()
                .HasMany(g => g.Players)
                .WithOne()
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DbPlayer>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<DbPlayer>()
                .HasMany(p => p.Answers)
                .WithOne()
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DbAnswerSubmission>()
                .HasKey(a => new { a.QuestionId, a.PlayerId });

            // DbAnswerSubmission -> Question, Player, Answer relationships
            modelBuilder.Entity<DbAnswerSubmission>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DbAnswerSubmission>()
                .HasOne(a => a.Player)
                .WithMany(p => p.Answers)
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DbAnswerSubmission>()
                .HasOne(a => a.Answer)
                .WithMany()
                .HasForeignKey(a => a.AnswerId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class DbQuiz
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Automatyczne generowanie ID
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CreatorId { get; set; }
        public ICollection<DbQuestion> Questions { get; set; }
    }

    public class DbQuestion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Automatyczne generowanie ID
        public string Id { get; set; }
        public string QuestionText { get; set; }
        public string QuizId { get; set; }
        public DbQuiz Quiz { get; set; }
        public ICollection<DbAnswer> Answers { get; set; }
    }

    public class DbAnswer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Automatyczne generowanie ID
        public string Id { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
        public string QuestionId { get; set; }

        // Navigation property to the related Question
        public DbQuestion Question { get; set; } // This establishes the relationship with DbQuestion
    }

    public class DbAnswerSubmission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Automatyczne generowanie ID
        public string QuestionId { get; set; }
        public string PlayerId { get; set; }
        public string AnswerId { get; set; }
        public bool IsCorrect { get; set; }

        // Navigation properties for relationships
        public DbQuestion Question { get; set; } // This establishes the relationship with DbQuestion
        public DbPlayer Player { get; set; } // This establishes the relationship with DbPlayer
        public DbAnswer Answer { get; set; } // This establishes the relationship with DbAnswer
    }

    public class DbGame
    {
        public string GameId { get; set; }
        public string GameCode { get; set; }
        public string QuizId { get; set; }
        public string Status { get; set; }
        public ICollection<DbPlayer> Players { get; set; }
    }

    public class DbPlayer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Automatyczne generowanie ID
        public string Id { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public string GameId { get; set; }
        public ICollection<DbAnswerSubmission> Answers { get; set; }
    }

  
}
