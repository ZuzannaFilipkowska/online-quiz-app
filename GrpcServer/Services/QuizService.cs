using Grpc.Core;
using QuizApp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

public class QuizServiceImpl : QuizService.QuizServiceBase
{
    private static readonly List<Quiz> Quizzes = new()
    {
        new Quiz
        {
            Id = "1",
            Title = "Quiz 1",
            Description = "Pierwszy quiz",
            Questions =
            {
                new Question
                {
                    Id = "q1",
                    QuestionText = "Co to jest Blazor?",
                    Answers =
                    {
                        new Answer { Id = "a1", Text = "Framework", IsCorrect = true },
                        new Answer { Id = "a2", Text = "Biblioteka", IsCorrect = false },
                        new Answer { Id = "a3", Text = "System operacyjny", IsCorrect = false },
                        new Answer { Id = "a4", Text = "Przegl¹darka", IsCorrect = false }
                    }
                },
                new Question
                {
                    Id = "q2",
                    QuestionText = "Który protokó³ u¿ywa gRPC?",
                    Answers =
                    {
                        new Answer { Id = "a1", Text = "HTTP/1.1", IsCorrect = false },
                        new Answer { Id = "a2", Text = "HTTP/2", IsCorrect = true },
                        new Answer { Id = "a3", Text = "FTP", IsCorrect = false },
                        new Answer { Id = "a4", Text = "SMTP", IsCorrect = false }
                    }
                }
            }
        },
        new Quiz
        {
            Id = "2",
            Title = "Quiz 2",
            Description = "Drugi quiz",
            Questions =
            {
                new Question
                {
                    Id = "q3",
                    QuestionText = "Jaka jest podstawowa funkcja Blazora?",
                    Answers =
                    {
                        new Answer { Id = "a1", Text = "Tworzenie aplikacji mobilnych", IsCorrect = false },
                        new Answer { Id = "a2", Text = "Tworzenie aplikacji webowych", IsCorrect = true },
                        new Answer { Id = "a3", Text = "Obs³uga baz danych", IsCorrect = false },
                        new Answer { Id = "a4", Text = "Zarz¹dzanie serwerami", IsCorrect = false }
                    }
                }
            }
        }
    };

    public override Task<QuizzesResponse> GetQuizzes(Empty request, ServerCallContext context)
    {
        var response = new QuizzesResponse();
        response.Quizzes.AddRange(Quizzes);
        return Task.FromResult(response);
    }

    public override Task<QuizDetailsResponse> GetQuizDetails(QuizRequest request, ServerCallContext context)
    {
        var quiz = Quizzes.FirstOrDefault(q => q.Id == request.QuizId);
        if (quiz == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
        }

        var response = new QuizDetailsResponse
        {
            Quiz = quiz
        };
        return Task.FromResult(response);
    }

    public override Task<QuizResponse> AddQuiz(AddQuizRequest request, ServerCallContext context)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Title and description are required."));
        }

        // Generate new quiz ID
        var newQuizId = Guid.NewGuid().ToString();

        // Create new quiz
        var newQuiz = new Quiz
        {
            Id = newQuizId,
            Title = request.Title,
            Description = request.Description,
            Questions = { request.Questions }
        };

        // Add quiz to the in-memory list
        Quizzes.Add(newQuiz);

        var response = new QuizResponse
        {
            QuizId = newQuizId,
            Message = "Quiz successfully added."
        };
        return Task.FromResult(response);
    }

    public override Task<ActiveGamesResponse> GetActiveGames(Empty request, ServerCallContext context)
    {
        var response = new ActiveGamesResponse();

        response.Games.Add(new Game
        {
            GameId = "1",
            GameCode = "ABC123",
            Status = "W toku"
        });

        response.Games.Add(new Game
        {
            GameId = "2",
            GameCode = "XYZ789",
            Status = "Oczekuj¹ca"
        });

        response.Games.Add(new Game
        {
            GameId = "3",
            GameCode = "DEF456",
            Status = "Zakoñczona"
        });

        return Task.FromResult(response);
    }
}
