using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Android.Content;

namespace QuizApp
{
    [Activity(Label = "QuizEndActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class QuizEndActivity : Activity
    {
        private LinearLayout _rankingList;
        private ProgressBar _loader;
        private QuizService.QuizServiceClient _grpcClient;
        private string _gameId;
        private string _playerId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.quiz_end_view);

            // Pobierz dane z poprzedniej aktywności
            _gameId = Intent.GetStringExtra("gameId");
            _playerId = Intent.GetStringExtra("playerId");

            // Ustaw referencje do widoków
            _rankingList = FindViewById<LinearLayout>(Resource.Id.ranking_list);
            _loader = FindViewById<ProgressBar>(Resource.Id.loader);

            var newGameButton = FindViewById<Button>(Resource.Id.new_game_button);

            // Ustawienie zdarzenia kliknięcia
            newGameButton.Click += (sender, args) =>
            {
                var intent = new Intent(this, typeof(MainActivity)); // Przejście do MainActivity
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask); // Wyczyść stos aktywności
                StartActivity(intent); // Uruchom MainActivity
                Finish(); // Zamknij QuizEndActivity
            };

            // Uruchom odbieranie danych z serwera
            Task.Run(async () => await LoadGameResults());
        }

        private async Task LoadGameResults()
        {
            _grpcClient = GrpcClientProvider.Instance.GetClient();

            try
            {
                // Pobierz szczegóły gry z serwera
                var gameDetails = await _grpcClient.GetGameResultsAsync(new GameRequest { GameId = _gameId });

                // Wyciągnij dane gracza
                var currentPlayer = gameDetails.Players.FirstOrDefault(p => p.Id == _playerId);
                var currentPlayerScore = currentPlayer?.Score ?? 0;

                RunOnUiThread(() =>
                {
                    // Wyświetl wynik gracza
                    var scoreTextView = FindViewById<TextView>(Resource.Id.player_score);
                    scoreTextView.Text = currentPlayerScore.ToString();

                    // Wypełnij ranking
                    var ranking = gameDetails.Players
                        .OrderByDescending(p => p.Score)
                        .Select(p => (p.Name, p.Score))
                        .ToList();

                    PopulateRankingList(ranking, currentPlayer?.Name);
                    _loader.Visibility = ViewStates.Gone;
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, $"Błąd: {ex.Message}", ToastLength.Long).Show();
                });
            }
        }

        private void PopulateRankingList(List<(string Name, int Score)> ranking, string currentPlayer)
        {
            _rankingList.RemoveAllViews(); // Wyczyść poprzednią zawartość

            for (int i = 0; i < ranking.Count; i++)
            {
                var (name, score) = ranking[i];

                var rankView = LayoutInflater.From(this).Inflate(Resource.Layout.single_rank_item, _rankingList, false);

                var rankNumber = rankView.FindViewById<TextView>(Resource.Id.rank_number);
                var playerName = rankView.FindViewById<TextView>(Resource.Id.player_name);
                var playerScore = rankView.FindViewById<TextView>(Resource.Id.player_score);

                rankNumber.Text = $"{i + 1}.";
                playerName.Text = name;
                playerScore.Text = $"{score} pkt";

                if (name == currentPlayer)
                {
                    playerName.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
                    playerScore.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
                }

                _rankingList.AddView(rankView);

                // Dodaj separator między elementami
                if (i < ranking.Count - 1)
                {
                    var divider = new View(this)
                    {
                        LayoutParameters = new LinearLayout.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            1) // Wysokość 1dp
                    };
                    divider.SetBackgroundColor(Android.Graphics.Color.Gray);
                    _rankingList.AddView(divider);
                }
            }
        }
    }
}
