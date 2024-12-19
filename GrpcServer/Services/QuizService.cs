//using Grpc.Core;
//using Grpc.Net.Client;
//using GrpcClient;
//using System.Threading.Tasks;
//namespace GrpcServer.Services
using Grpc.Core;
using QuizApp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;


public class QuizServiceImpl : QuizService.QuizServiceBase
{
    private static readonly List<Quiz> Quizzes = new()
    {
        new Quiz { Id = "1", Title = "Quiz 1", Description = "Pierwszy quiz" },
        new Quiz { Id = "2", Title = "Quiz 2", Description = "Drugi quiz" },
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

        // Generowanie przyk³adowych pytañ i odpowiedzi
        var questions = new List<Question>
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
        },
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
        },
        new Question
        {
            Id = "q4",
            QuestionText = "Który jêzyk jest u¿ywany w Blazorze?",
            Answers =
            {
                new Answer { Id = "a1", Text = "JavaScript", IsCorrect = false },
                new Answer { Id = "a2", Text = "Python", IsCorrect = false },
                new Answer { Id = "a3", Text = "C#", IsCorrect = true },
                new Answer { Id = "a4", Text = "Java", IsCorrect = false }
            }
        },
        new Question
        {
            Id = "q5",
            QuestionText = "Co to jest gRPC?",
            Answers =
            {
                new Answer { Id = "a1", Text = "Protokó³ komunikacji", IsCorrect = true },
                new Answer { Id = "a2", Text = "System operacyjny", IsCorrect = false },
                new Answer { Id = "a3", Text = "Framework do UI", IsCorrect = false },
                new Answer { Id = "a4", Text = "Biblioteka baz danych", IsCorrect = false }
            }
        }
    };

        // Dodawanie pytañ do odpowiedzi
        response.Questions.AddRange(questions);

        return Task.FromResult(response);
    }


    public override Task<GameResponse> CreateGame(QuizRequest request, ServerCallContext context)
    {
        var gameId = Guid.NewGuid().ToString();
        var response = new GameResponse
        {
            GameId = gameId,
            Message = "Gra zosta³a utworzona"
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

    // Implementacja metody AddQuiz
    public override Task<QuizResponse> AddQuiz(AddQuizRequest request, ServerCallContext context)
    {
        // Walidacja danych
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Tytu³ i opis quizu s¹ wymagane."));
        }

        // Generowanie nowego ID dla quizu
        var newQuizId = Guid.NewGuid().ToString();

        // Tworzenie nowego quizu
        var newQuiz = new Quiz
        {
            Id = newQuizId,
            Title = request.Title,
            Description = request.Description,
            // Przekazywanie pytañ
          //  Questions = { request.Questions }
        };

        // Zapisanie quizu w pamiêci
        //_quizzes.Add(newQuiz);

        // Przygotowanie odpowiedzi
        var response = new QuizResponse
        {
            QuizId = newQuizId,
            Message = "Quiz zosta³ pomyœlnie dodany."
        };

        return Task.FromResult(response);
    }

}
