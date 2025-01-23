using Android.App;
using Android.OS;
using Android.Widget;
using Android.Views;
using System.Threading.Tasks;
using Android.Graphics;
using System;
using System.Collections.Generic;

namespace QuizApp
{
    [Activity(Label = "QuestionActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class QuestionActivity : Activity
    {
        private string _selectedAnswerId;
        private LinearLayout _answersContainer;
        private QuizService.QuizServiceClient _grpcClient;
        private string _currentQuestionId;
        private string _gameId;
        private string _playerId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.question_view);

            // Pobranie danych przekazanych z poprzedniej aktywności
            _gameId = Intent.GetStringExtra("gameId");
            _playerId = Intent.GetStringExtra("playerId");

            // Inicjalizacja widoków
            _answersContainer = FindViewById<LinearLayout>(Resource.Id.answers_container);

            _grpcClient = GrpcClientProvider.Instance.GetClient();

            // Załaduj pytanie z serwera
            Task.Run(async () => await LoadNextQuestion());

            // Obsługa przycisku przesyłania odpowiedzi
            var submitButton = FindViewById<Button>(Resource.Id.submit_button);
            submitButton.Click += (s, e) => SubmitAnswer();
        }

        private async Task LoadNextQuestion()
        {
            try
            {
                // Pobierz pytanie i odpowiedzi z serwera
                var questionResponse = await _grpcClient.NextQuestionAsync(new GameRequest { GameId = _gameId });

                RunOnUiThread(() =>
                {
                    // Wyświetl pytanie
                    var questionTextView = FindViewById<TextView>(Resource.Id.question_text);
                    questionTextView.Text = questionResponse.QuestionText;

                    // Wyczyść poprzednie odpowiedzi
                    _answersContainer.RemoveAllViews();
                    var letterToAnswerId = new Dictionary<char, string>();

                    // Wyświetlenie odpowiedzi
                    char currentLetter = 'A'; // Pierwsza litera
                    foreach (var answer in questionResponse.Answers)
                    {
                        var answerView = LayoutInflater.From(this).Inflate(Resource.Layout.single_answer_view, _answersContainer, false);

                        var letterText = answerView.FindViewById<TextView>(Resource.Id.answer_letter);
                        var answerText = answerView.FindViewById<TextView>(Resource.Id.answer_text);

                        // Ustawienie litery i tekstu odpowiedzi
                        letterText.Text = currentLetter.ToString();
                        answerText.Text = string.IsNullOrEmpty(answer.Text) ? "Brak tekstu odpowiedzi" : answer.Text;

                        // Zapisanie mapowania litery na ID odpowiedzi
                        letterToAnswerId[currentLetter] = answer.Id;

                        // Obsługa kliknięcia odpowiedzi
                        answerView.Click += (s, e) =>
                        {
                            HandleAnswerClick(answerView, answer.Id); // Tu używamy ID odpowiedzi
                        };

                        _answersContainer.AddView(answerView);
                        currentLetter++; // Przejdź do kolejnej litery
                    }

                    // Zapisz ID pytania
                    _currentQuestionId = questionResponse.QuestionId;

                    // Jeśli to ostatnie pytanie
                    if (questionResponse.IsFinished)
                    {
                        Toast.MakeText(this, "Quiz finished!", ToastLength.Long).Show();
                    }
                });
            }
            catch (Exception e)
            {
                RunOnUiThread(() =>
                {
                    // Pokazanie komunikatu o błędzie, jeśli wystąpił problem podczas ładowania pytania
                    Toast.MakeText(this, $"Nie udało się załadować pytania.\n {e.Message}", ToastLength.Long).Show();
                });
            }
        }

        private void HandleAnswerClick(View answerView, string answerId)
        {
            // Reset zaznaczenia
            for (int i = 0; i < _answersContainer.ChildCount; i++)
            {
                var child = _answersContainer.GetChildAt(i);
                child.SetBackgroundColor(Color.LightGray); // Ustawienie koloru tła
            }

            // Zaznaczenie odpowiedzi
            answerView.SetBackgroundColor(Color.LightBlue);

            _selectedAnswerId = answerId;
        }

        private async void SubmitAnswer()
        {
            if (string.IsNullOrEmpty(_selectedAnswerId))
            {
                Toast.MakeText(this, "Wybierz odpowiedź!", ToastLength.Short).Show();
                return;
            }

            try
            {
                // Przygotowanie żądania do serwera
                var request = new AnswerRequest
                {
                    GameId = _gameId,
                    PlayerId = _playerId,
                    QuestionId = _currentQuestionId,
                    AnswerId = _selectedAnswerId
                };

                // Wysłanie odpowiedzi do serwera
                var response = await _grpcClient.SubmitAnswerAsync(request);

                RunOnUiThread(() =>
                {
                    // Przejście do ekranu pośredniego
                    var intent = new Android.Content.Intent(this, typeof(IntermediateScreenActivity));
                    intent.PutExtra("isAnswerCorrect", response.IsCorrect);
                    intent.PutExtra("gameId", _gameId);
                    intent.PutExtra("playerId", _playerId);

                    // Zakończenie aktywności
                    StartActivity(intent);
                    Finish();
                });
            }
            catch (Exception e)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, $"Nie udało się przesłać odpowiedzi.\n {e.Message}", ToastLength.Long).Show();
                });
            }
        }
    }
}
