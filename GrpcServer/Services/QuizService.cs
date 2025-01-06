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
            CreatorId = "1",
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
            CreatorId = "1",
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

    private static readonly List<Game> Games = new()
{
    new Game
    {
        GameId = "1",
        GameCode = "ABC123",
        QuizId = "1",
        Status = "Oczekuj¹ca",
        Players =
        {
            new Player
            {
                Id = "1",
                Name = "Jan Kowalski",
                Score = 0,
                Answers = { }
            },
            new Player
            {
                Id = "2",
                Name = "Piotr Nowak",
                Score = 0,
                Answers = { }
            },
            new Player
            {
                Id = "3",
                Name = "Micha³ Zieliñski",
                Score = 0,
                Answers = { }
            }
        }
    }
};

    private static readonly List<Player> Players = new();

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

  

    public override Task<ActiveGamesResponse> GetActiveGames(Empty request, ServerCallContext context)
    {
        var response = new ActiveGamesResponse();
        response.Games.AddRange(Games);
        return Task.FromResult(response);
    }

    public override Task<GameResponse> CreateGame(CreateGameRequest request, ServerCallContext context)
    {
        var quiz = Quizzes.FirstOrDefault(q => q.Id == request.QuizId);
        if (quiz == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
        }

        var newGame = new Game
        {
            GameId = Guid.NewGuid().ToString(),
            QuizId = request.QuizId,
            GameCode = Guid.NewGuid().ToString(),
            Status = "Oczekuj¹ca",
            Players = { },
        };

        Games.Add(newGame);

        var response = new GameResponse
        {
            GameId = newGame.GameId,
            GameCode = newGame.GameCode,
            Message = "Game created successfully"
        };
        return Task.FromResult(response);
    }

    public override Task<GameDetailsResponse> GetGameDetails(GameRequest request, ServerCallContext context)
    {
        var game = Games.FirstOrDefault(g => g.GameId == request.GameId);
        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        var quiz = Quizzes.FirstOrDefault(q => q.Id == game.QuizId);
        if (quiz == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
        }

        var response = new GameDetailsResponse
        {
            GameId = game.GameId,
            Status = game.Status,
            Players = { game.Players },
            Questions = { quiz.Questions },
            CurrentQuestionIndex = 0 // nie wiem co to jest, wiêc ustawiam na 0
        };

        return Task.FromResult(response);
    }


    public override Task<AnswerResponse> SubmitAnswer(AnswerRequest request, ServerCallContext context)
    {
        var game = Games.FirstOrDefault(g => g.GameId == request.GameId);
        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        var quiz = Quizzes.FirstOrDefault(q => q.Id == game.QuizId);
        var questions = quiz?.Questions;

        var question = questions?.FirstOrDefault(q => q.Id == request.QuestionId);
        if (question == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Question not found"));
        }

        var answer = question.Answers.FirstOrDefault(a => a.Id == request.AnswerId);
        if (answer == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Answer not found"));
        }

        var userAnswer = new AnswerSubmission
        {
            QuestionId = request.QuestionId,
            AnswerId = request.AnswerId,
            IsCorrect = answer.IsCorrect
        };

        Players.FirstOrDefault(p => p.Id == request.PlayerId)?.Answers.Add(userAnswer);

        var response = new AnswerResponse
        {
            IsCorrect = answer.IsCorrect,
            PlayerScore = Players.FirstOrDefault(p => p.Id == request.PlayerId)?.Score ?? 0,
            Message = "Answer submitted successfully"
        };
        return Task.FromResult(response);
    }

    public override Task<JoinGameResponse> JoinGame(JoinGameRequest request, ServerCallContext context)
    {
        var game = Games.FirstOrDefault(g => g.GameCode == request.GameCode);
        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        var newPlayer = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.PlayerName,
            Score = 0,
            Answers = { }
        };

        Players.Add(newPlayer);
        game.Players.Add(newPlayer);

        var response = new JoinGameResponse
        {
            GameId = game.GameId,
            IsJoined = true,
            Message = "Player joined the game successfully"
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

        var newQuiz = new Quiz
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            CreatorId = "1",
            Questions = { request.Questions }
        };

        // Add quiz to the in-memory list
        Quizzes.Add(newQuiz);

        var response = new QuizResponse
        {
            QuizId = newQuiz.Id,
            Message = "Quiz successfully added."
        };
        return Task.FromResult(response);
    }

    public override Task<StartGameResponse> StartGame(GameRequest request, ServerCallContext context)
    {
        var game = Games.FirstOrDefault(g => g.GameId == request.GameId);
        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        game.Status = "W toku";

        var response = new StartGameResponse
        {
            IsStarted = true,
            Message = "Game started successfully"
        };
        return Task.FromResult(response);
    }

    public override Task<GameResultsResponse> GetGameResults(GameRequest request, ServerCallContext context)
    {
        var game = Games.FirstOrDefault(g => g.GameId == request.GameId);
        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        var players = game.Players;

        var response = new GameResultsResponse
        {
            GameId = game.GameId,
            Players = { players }
        };
        return Task.FromResult(response);
    }
}
