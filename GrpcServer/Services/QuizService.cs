using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using QuizApp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

public class QuizServiceImpl : QuizService.QuizServiceBase
{
       private readonly AppDbContext _context;

    public QuizServiceImpl(AppDbContext context)
    {
        _context = context;
    }

    public override async Task<QuizzesResponse> GetQuizzes(Empty request, ServerCallContext context)
    {
        var quizzes = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
            .ToListAsync();

        var response = new QuizzesResponse();
        foreach (var quiz in quizzes)
        {
            response.Quizzes.Add(new Quiz
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                CreatorId = quiz.CreatorId,
                Questions = { quiz.Questions.Select(q => new Question
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    Answers = { q.Answers.Select(a => new Answer
                    {
                        Id = a.Id,
                        Text = a.Text,
                        IsCorrect = a.IsCorrect
                    }) }
                }) }
            });
        }

        return response;
    }

    public override async Task<QuizDetailsResponse> GetQuizDetails(QuizRequest request, ServerCallContext context)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == request.QuizId);

        if (quiz == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
        }

        var response = new QuizDetailsResponse
        {
            Quiz = new Quiz
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                CreatorId = quiz.CreatorId,
                Questions = { quiz.Questions.Select(q => new Question
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    Answers = { q.Answers.Select(a => new Answer
                    {
                        Id = a.Id,
                        Text = a.Text,
                        IsCorrect = a.IsCorrect
                    }) }
                }) }
            }
        };
        return response;
    }

    public override async Task<ActiveGamesResponse> GetActiveGames(Empty request, ServerCallContext context)
    {
        var games = await _context.Games
            .Include(g => g.Players)
            .ToListAsync();

        var response = new ActiveGamesResponse();
        foreach (var game in games)
        {
            response.Games.Add(new Game
            {
                GameId = game.GameId,
                GameCode = game.GameCode,
                Status = game.Status,
                Players = { game.Players.Select(p => new Player
                {
                    Id = p.Id,
                    Name = p.Name,
                    Score = p.Score
                }) }
            });
        }

        return response;
    }

    public override async Task<QuizResponse> AddQuiz(AddQuizRequest request, ServerCallContext context)
    {
        // Walidacja wejœcia
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Title and description are required."));
        }

        // Tworzenie nowego quizu bez pytañ
        var newQuiz = new DbQuiz
        {
            Title = request.Title,
            Description = request.Description,
            CreatorId = "1",
        };

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // Zapisanie quizu w bazie danych
                await _context.Quizzes.AddAsync(newQuiz);
                await _context.SaveChangesAsync();

                // Pobranie identyfikatora nowego quizu
                var savedQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Title == newQuiz.Title && q.Description == newQuiz.Description);

                if (savedQuiz == null)
                {
                    throw new RpcException(new Status(StatusCode.Internal, "Failed to retrieve the saved quiz."));
                }

                Console.WriteLine($"Saved Quiz ID: {savedQuiz.Id}");

                // Dodawanie pytañ i odpowiedzi do quizu
                foreach (var questionRequest in request.Questions)
                {
                    var newQuestion = new DbQuestion
                    {
                        QuestionText = questionRequest.QuestionText,
                        QuizId = savedQuiz.Id, // Ustawienie relacji z quizem
                        Answers = questionRequest.Answers.Select(a => new DbAnswer
                        {
                            Text = a.Text,
                            IsCorrect = a.IsCorrect
                        }).ToList()
                    };

                    Console.WriteLine($"Adding Question: {newQuestion.QuestionText}, QuizId: {newQuestion.QuizId}");

                    await _context.Questions.AddAsync(newQuestion);
                }

                // Zapis pytañ i odpowiedzi w bazie danych
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new QuizResponse
                {
                    QuizId = savedQuiz.Id,
                    Message = "Quiz successfully added."
                };

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error while saving changes: {ex.Message}");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to save quiz and questions."));
            }
        }
    }

    public override async Task<GameResponse> CreateGame(CreateGameRequest request, ServerCallContext context)
    {
        var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == request.QuizId);
        if (quiz == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
        }

        var newGame = new DbGame
        {
            GameId = Guid.NewGuid().ToString(),
            GameCode = Guid.NewGuid().ToString(),
            QuizId = request.QuizId,
            Status = "Oczekuj¹ca"
        };

        _context.Games.Add(newGame);
        await _context.SaveChangesAsync();

        var response = new GameResponse
        {
            GameId = newGame.GameId,
            GameCode = newGame.GameCode,
            Message = "Game created successfully"
        };
        return response;
    }

    public override async Task<GameDetailsResponse> GetGameDetails(GameRequest request, ServerCallContext context)
    {
        var game = await _context.Games
            .Include(g => g.Players) 
            .FirstOrDefaultAsync(g => g.GameId == request.GameId);

        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == game.QuizId);

        if (quiz == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
        }

        var response = new GameDetailsResponse
        {
            GameId = game.GameId,
            Status = game.Status,
            Players = { game.Players.Select(p => new Player
        {
            Id = p.Id,
            Name = p.Name,
            Score = p.Score
        }) },
            Questions = { quiz.Questions.Select(q => new Question
        {
            Id = q.Id,
            QuestionText = q.QuestionText,
            Answers = { q.Answers.Select(a => new Answer
            {
                Id = a.Id,
                Text = a.Text,
                IsCorrect = a.IsCorrect
            }) }
        }) },
            CurrentQuestionIndex = 0,
            GameCode = game.GameCode,
        };

        return response;
    }

    public override async Task<StartGameResponse> StartGame(GameRequest request, ServerCallContext context)
    {
        // Rozpoczêcie transakcji
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // Pobranie gry z bazy danych na podstawie GameId
                var game = await _context.Games.FirstOrDefaultAsync(g => g.GameId == request.GameId);
                if (game == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
                }

                // Aktualizacja statusu gry
                game.Status = "W toku";
                _context.Entry(game).State = EntityState.Modified;

                // Zapisanie zmian w bazie danych
                await _context.SaveChangesAsync();

                // Potwierdzenie transakcji
                await transaction.CommitAsync();

                var response = new StartGameResponse
                {
                    IsStarted = true,
                    Message = "Game started successfully"
                };

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting game: {ex.Message}");

                await transaction.RollbackAsync();

                throw new RpcException(new Status(StatusCode.Internal, "Error starting the game"));
            }
        }
    }

    public override async Task StreamGameUpdates(GameRequest request, IServerStreamWriter<GameDetailsResponse> responseStream, ServerCallContext context)
    {
        var game = await _context.Games
      .AsNoTracking()
      .Include(g => g.Players)
      .FirstOrDefaultAsync(g => g.GameId == request.GameId);


        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        while (!context.CancellationToken.IsCancellationRequested)
        {
            var updatedGame = await _context.Games
                .AsNoTracking()
                .Include(g => g.Players)  
                .ThenInclude(p => p.Answers)  
                .FirstOrDefaultAsync(g => g.GameId == request.GameId);

            var quiz = await _context.Quizzes
                .Include(q => q.Questions) 
              .ThenInclude(q => q.Answers) 
             .FirstOrDefaultAsync(q => q.Id == game.QuizId);

            if (updatedGame == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
            }

            var response = new GameDetailsResponse
            {
                GameId = updatedGame.GameId,
                Status = updatedGame.Status,
                Players = { updatedGame.Players.Select(p => new Player
            {
                Id = p.Id,
                Name = p.Name,
                Score = p.Score
            }) },
                CurrentQuestionIndex = 0, // Replace with actual logic if needed
                GameCode = updatedGame.GameCode,
            };

            // Send the updated data to the client
            await responseStream.WriteAsync(response);

            // Introduce a delay to control update frequency
            await Task.Delay(5000); 
        }
    }

    public override async Task<JoinGameResponse> JoinGame(JoinGameRequest request, ServerCallContext context)
    {
        // Pobieranie gry z bazy danych na podstawie GameCode
        var game = await _context.Games
            .Include(g => g.Players) 
            .FirstOrDefaultAsync(g => g.GameCode == request.GameCode);

        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        // Tworzenie nowego gracza
        var newPlayer = new DbPlayer
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.PlayerName,
            Score = 0
        };

        // Dodanie gracza do gry i do bazy danych
        game.Players.Add(newPlayer);
        _context.Players.Add(newPlayer);

        // Zapisanie zmian w bazie danych
        await _context.SaveChangesAsync();

        var response = new JoinGameResponse
        {
            GameId = game.GameId,
            IsJoined = true,
            Message = "Player joined the game successfully"
        };

        return response;
    }

    public override async Task<AnswerResponse> SubmitAnswer(AnswerRequest request, ServerCallContext context)
    {
        // Pocz¹tek transakcji
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // Pobieranie gry z bazy danych na podstawie GameId
                var game = await _context.Games
                    .Include(g => g.Players)  
                    .ThenInclude(p => p.Answers)  
                     .FirstOrDefaultAsync(g => g.GameId == request.GameId);

                if (game == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
                }

                // Pobieranie quizu z bazy danych na podstawie QuizId
                var quiz = await _context.Quizzes
                    .Include(q => q.Questions) 
                     .ThenInclude(q => q.Answers) 
                    .FirstOrDefaultAsync(q => q.Id == game.QuizId);

                if (quiz == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
                }

                // Znalezienie pytania
                var question = quiz.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
                if (question == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Question not found"));
                }

                // Znalezienie odpowiedzi
                var answer = question.Answers.FirstOrDefault(a => a.Id == request.AnswerId);
                if (answer == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Answer not found"));
                }

                // Pobieranie gracza
                var player = game.Players.FirstOrDefault(p => p.Id == request.PlayerId);
                if (player == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Player not found"));
                }

                // Zapisanie odpowiedzi gracza
                var userAnswer = new DbAnswerSubmission
                {
                    QuestionId = request.QuestionId,
                    AnswerId = request.AnswerId,
                    PlayerId = player.Id,
                    IsCorrect = answer.IsCorrect
                };

                // Dodanie odpowiedzi do AnswerSubmissions, aby EF œledzi³ tê zmianê
                _context.AnswerSubmissions.Add(userAnswer);

                // Upewnienie siê, ¿e Answers w Playerze jest inicjowane (jeœli nie istnieje)
                if (player.Answers == null)
                {
                    player.Answers = new List<DbAnswerSubmission>();
                }

                // Dodanie odpowiedzi do listy Answers w Playerze
                player.Answers.Add(userAnswer);

                // Aktualizacja wyniku gracza
                if (answer.IsCorrect)
                {
                    player.Score += 1; // Poprawna odpowiedŸ to 1 punkt
                }

                // Sprawdzanie, czy wszyscy gracze odpowiedzieli 
                var allPlayersAnswered = game.Players
                    .Where(p => p.Id != request.PlayerId) // Pomijamy gracza, który aktualnie odpowiada
                    .All(p => p.Answers != null && p.Answers.Count == quiz.Questions.Count);

                if (allPlayersAnswered && player.Answers.Count == quiz.Questions.Count)
                {
                    game.Status = "Zakoñczona";
                }

             
                foreach (var p in game.Players)
                {
                    Console.WriteLine($"Player {p.Id} answers count: {p.Answers?.Count}");
                }
                Console.WriteLine($"All players answered: {allPlayersAnswered}");


                // Upewnienie, ¿e status gry jest zmieniony w kontekœcie
                _context.Entry(game).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new AnswerResponse
                {
                    IsCorrect = answer.IsCorrect,
                    PlayerScore = player.Score,
                    Message = "Answer submitted successfully"
                };

                return response;
            }
            catch (Exception)
            {
                // W przypadku b³êdu, rollback transakcji
                await transaction.RollbackAsync();
                throw; 
            }
        }
    }

    public override async Task<GameResultsResponse> GetGameResults(GameRequest request, ServerCallContext context)
    {
        // Pobranie gry z bazy danych na podstawie GameId
        var game = await _context.Games
            .Include(g => g.Players) // Do³¹czenie graczy
            .FirstOrDefaultAsync(g => g.GameId == request.GameId);

        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        // Mapowanie wyników graczy
        var response = new GameResultsResponse
        {
            GameId = game.GameId,
            Players =
        {
            game.Players.Select(p => new Player
            {
                Id = p.Id,
                Name = p.Name,
                Score = p.Score,
                  })
        }
        };

        return response;
    }
}
