using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using QuizApp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Diagnostics;

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
        // Walidacja wej�cia
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Title and description are required."));
        }

        // Tworzenie nowego quizu bez pyta�
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

                // Dodawanie pyta� i odpowiedzi do quizu
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

                // Zapis pyta� i odpowiedzi w bazie danych
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
            Status = "Oczekująca"
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
        // Rozpocz�cie transakcji
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
                game.CurrentQuestionIndex = 0;
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

        // Pętla streamująca dane w czasie rzeczywistym
        while (!context.CancellationToken.IsCancellationRequested)
        {
            var updatedGame = await _context.Games
                .AsNoTracking()
                .Include(g => g.Players)
                .ThenInclude(p => p.Answers)
                .FirstOrDefaultAsync(g => g.GameId == request.GameId);

            if (updatedGame == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
            }

            // Upewnij się, że masz dostęp do quizu powiązanego z grą
            var quiz = await _context.Quizzes
                .Include(q => q.Questions) // Ładuj pytania powiązane z quizem
                .FirstOrDefaultAsync(q => q.Id == game.QuizId);

            if (quiz == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
            }

            // Przekonwertuj ICollection na List<DbQuestion>, aby móc używać indeksowania
            var questionsList = quiz.Questions.ToList();
            var allPlayersAnswered = false;

            if (updatedGame.CurrentQuestionIndex > 0 && updatedGame.CurrentQuestionIndex <= questionsList.Count)
            {
                var currentQuestionId = questionsList[updatedGame.CurrentQuestionIndex - 1].Id;

                // Sprawdzenie, czy wszyscy gracze odpowiedzieli na dane pytanie
                allPlayersAnswered = game.Players
                    .All(p => _context.AnswerSubmissions
                        .Any(answSubm => answSubm.PlayerId == p.Id && answSubm.QuestionId == currentQuestionId));

                Console.WriteLine($"allPlayersAnswered: {allPlayersAnswered}, Type: {allPlayersAnswered.GetType()}");
            }
            else
            {
                Console.WriteLine("CurrentQuestionIndex is out of range.");
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
                AllPlayersAnswered = allPlayersAnswered,
                CurrentQuestionIndex = updatedGame.CurrentQuestionIndex,
                GameCode = updatedGame.GameCode,
            };

            // Wysłanie odpowiedzi do klienta
            await responseStream.WriteAsync(response);

            await Task.Delay(5000);
            if (allPlayersAnswered)
                break; 
        }
    }

    // Przykład, gdy gracz odpowiada na pytanie
    public void AnswerQuestion(int playerId, bool isCorrect)
    {
        var player = _context.Players.Find(playerId);
        if (player != null)
        {
            // Ustawienie flagi HasAnswered po odpowiedzi gracza
            player.HasAnswered = true;

            // Zaktualizowanie wyników (możesz dodać więcej logiki, np. przyznawanie punktów)
            player.Score += isCorrect ? 1 : 0;
            _context.SaveChanges();
        }
    }

    public override async Task<JoinGameResponse> JoinGame(JoinGameRequest request, ServerCallContext context)
    {
        // Pobieranie gry z bazy danych na podstawie GameCode
        var game = await _context.Games
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.GameCode == request.GameCode);

        if (game == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));

        // Tworzenie nowego gracza
        var playerId = Guid.NewGuid().ToString();
        var newPlayer = new DbPlayer
        {
            Id = playerId,
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
            PlayerId = playerId,
            IsJoined = true,
            Message = "Player joined the game successfully"
        };

        return response;
    }

    public override async Task<AnswerResponse> SubmitAnswer(AnswerRequest request, ServerCallContext context)
    {
        if (request == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Answer cannot be null."));
        }
        // Pocz�tek transakcji
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

                // Zapisanie zmian w bazie danych
                await _context.SaveChangesAsync();

                // Zapisanie odpowiedzi gracza
                var userAnswer = new DbAnswerSubmission
                {
                    QuestionId = request.QuestionId,
                    AnswerId = request.AnswerId,
                    PlayerId = player.Id,
                    IsCorrect = answer.IsCorrect
                };

                // Dodanie odpowiedzi do AnswerSubmissions, aby EF �ledzi� t� zmian�
                _context.AnswerSubmissions.Add(userAnswer);

                // Upewnienie si�, �e Answers w Playerze jest inicjowane (je�li nie istnieje)
                if (player.Answers == null)
                {
                    player.Answers = new List<DbAnswerSubmission>();
                }

                // Dodanie odpowiedzi do listy Answers w Playerze
                player.Answers.Add(userAnswer);

                // Aktualizacja wyniku gracza
                if (answer.IsCorrect)
                {
                    player.Score += 1; // Poprawna odpowied� to 1 punkt
                }

                // Sprawdzanie, czy wszyscy gracze odpowiedzieli 
                var allPlayersAnswered = game.Players
                    .Where(p => p.Id != request.PlayerId) // Pomijamy gracza, kt�ry aktualnie odpowiada
                    .All(p => p.Answers != null && p.Answers.Count == quiz.Questions.Count);

                if (allPlayersAnswered)
                {
                    game.CurrentQuestionIndex++;
                    _context.Entry(game).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                if (allPlayersAnswered && player.Answers.Count == quiz.Questions.Count)
                {
                    game.Status = "Zakończona";
                }

                foreach (var p in game.Players)
                {
                    Console.WriteLine($"Player {p.Id} answers count: {p.Answers?.Count}");
                }

                // Upewnienie, �e status gry jest zmieniony w kontek�cie
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
            catch (Exception e)
            {
                // W przypadku b��du, rollback transakcji
                await transaction.RollbackAsync();
                throw e;
            }
        }
    }

    public override async Task<GameResultsResponse> GetGameResults(GameRequest request, ServerCallContext context)
    {
        // Pobranie gry z bazy danych na podstawie GameId
        var game = await _context.Games
            .Include(g => g.Players) // Do��czenie graczy
            .FirstOrDefaultAsync(g => g.GameId == request.GameId);

        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        // Mapowanie wynik�w graczy
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

    public override async Task<StartQuizResponse> StartQuiz(GameRequest request, ServerCallContext context)
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
            .FirstOrDefaultAsync(q => q.Id == game.QuizId);

        if (quiz == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));
        }

        // Ustaw pierwsze pytanie
        game.CurrentQuestionIndex = 0;
        game.Status = "W toku";

        _context.Entry(game).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return new StartQuizResponse
        {
            IsStarted = true,
            Message = "Quiz started successfully"
        };
    }
    public override async Task<QuestionResponse> NextQuestion(GameRequest request, ServerCallContext context)
    {
        var game = await _context.Games
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.GameId == request.GameId);

        if (game == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == game.QuizId);

        if (quiz == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Quiz not found"));

        // Sprawdź, czy są jeszcze pytania do wyświetlenia
        if (game.CurrentQuestionIndex >= quiz.Questions.Count)
            return new QuestionResponse
            {
                IsFinished = true,
                IsLastQuestion = true,
                QuestionText = "Quiz finished"
            };

        var currentQuestion = quiz.Questions.ElementAt(game.CurrentQuestionIndex);

        if (currentQuestion == null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "No questions in Quiz!"));

        // Przygotuj listę odpowiedzi
        var answers = currentQuestion.Answers.Select(a => new UserAnswer
        {
            Id = a.Id,
            Text = a.Text
        });

        var response = new QuestionResponse
        {
            QuestionId = currentQuestion.Id,
            QuestionText = currentQuestion.QuestionText,
            IsFinished = false,
            IsLastQuestion = game.CurrentQuestionIndex == quiz.Questions.Count - 1,
            Answers = { answers }
        };

        return response;
    }

    public override async Task WaitForGameStart(GameRequest request, IServerStreamWriter<StartGameResponse> responseStream, ServerCallContext context)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.GameId == request.GameId);

        if (game == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        var timeout = TimeSpan.FromMinutes(5);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new RpcException(new Status(StatusCode.DeadlineExceeded, "Waiting for game start timed out."));
                }

                await Task.Delay(1000);
                _context.Entry(game).Reload();

                if (game.Status == "W toku")
                {
                    await responseStream.WriteAsync(new StartGameResponse
                    {
                        IsStarted = true
                    });
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Bezpieczne zakończenie w przypadku anulowania
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Unexpected error: {ex.Message}"));
        }
    }

}