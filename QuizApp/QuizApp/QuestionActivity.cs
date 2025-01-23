using Android.App;
using Android.OS;
using Android.Widget;
using Android.Views;
using System.Threading.Tasks;
using Android.Graphics;
using System.Collections.Generic;
using Grpc.Core; // Dodaj to dla obsługi gRPC

namespace QuizApp
{
    [Activity(Label = "QuestionActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class QuestionActivity : Activity
    {
        private int _selectedAnswerId;
        private LinearLayout _answersContainer;
        private QuizService.QuizServiceClient _grpcClient;
        private int _currentQuestionId;
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
            LoadQuestionsFromStream();

            // Obsługa przycisku przesyłania odpowiedzi
            var submitButton = FindViewById<Button>(Resource.Id.submit_button);
            submitButton.Click += (s, e) => SubmitAnswer();
        }

        private async Task LoadQuestionsFromStream()
        {
            try
            {
                // Rozpocznij strumieniowe pobieranie pytań
                using (var call = _grpcClient.NextQuestion(new GameRequest { GameId = _gameId }))
                {
                    // Odbierz pytania z serwera
                    while (await call.ResponseStream.MoveNext())
                    {
                        var questionResponse = call.ResponseStream.Current;

                        // Wyświetlenie pytania
                        var questionTextView = FindViewById<TextView>(Resource.Id.question_text);
                        questionTextView.Text = questionResponse.QuestionText;

                        // Wyczyść poprzednie odpowiedzi
                        _answersContainer.RemoveAllViews();

                        // Wyświetlenie odpowiedzi
                        foreach (var answer in questionResponse.Answers)
                        {
                            var answerView = LayoutInflater.From(this).Inflate(Resource.Layout.single_answer_view, _answersContainer, false);

                            var letterText = answerView.FindViewById<TextView>(Resource.Id.answer_letter);
                            var answerText = answerView.FindViewById<TextView>(Resource.Id.answer_text);

                            letterText.Text = answer.Id.ToString();
                            answerText.Text = answer.Text;

                            answerView.Click += (s, e) => HandleAnswerClick(answerView, answer.Id);

                            _answersContainer.AddView(answerView);
                        }

                        // Zapisz ID pytania
                        _currentQuestionId = questionResponse.QuestionId;

                        // Jeśli to ostatnie pytanie, przerwij strumień
                        if (questionResponse.IsFinished)
                        {
                            break;
                        }

                        // Poczekaj na następne pytanie
                        await Task.Delay(5000); // Czekaj 15 sekund na pytanie
                    }
                }
            }
            catch
            {
                Toast.MakeText(this, "Nie udało się załadować pytania.", ToastLength.Long).Show();
            }
        }

        private void HandleAnswerClick(View answerView, int answerId)
        {
            // Reset zaznaczenia
            for (int i = 0; i < _answersContainer.ChildCount; i++)
            {
                var child = _answersContainer.GetChildAt(i);
                child.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.LightGray);
            }

            // Zaznaczenie odpowiedzi
            answerView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.LightBlue);

            _selectedAnswerId = answerId;
        }

        private async void SubmitAnswer()
        {
            if (_selectedAnswerId == 0)
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
                    QuestionId = _currentQuestionId.ToString(),
                    AnswerId = _selectedAnswerId.ToString()
                };

                // Wysłanie odpowiedzi do serwera
                var response = await _grpcClient.SubmitAnswerAsync(request);

                // Przejście do ekranu pośredniego
                var intent = new Android.Content.Intent(this, typeof(IntermediateScreenActivity));
                intent.PutExtra("isAnswerCorrect", response.IsCorrect);
                intent.PutExtra("playerScore", response.PlayerScore);

                // Zakończenie aktywności
                StartActivity(intent);
                Finish();
            }
            catch
            {
                Toast.MakeText(this, "Nie udało się przesłać odpowiedzi.", ToastLength.Long).Show();
            }
        }
    }
}