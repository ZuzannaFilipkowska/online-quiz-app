using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;

namespace QuizApp
{
    [Activity(Label = "QuizEndActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class QuizEndActivity : Activity
    {
        private LinearLayout _rankingList;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.quiz_end_view);

            string currentPlayer = "Player1"; // Zastąp dynamicznie uzyskaną nazwą gracza
            int currentPlayerScore = 100;     // Wynik gracza
            var ranking = new List<(string Name, int Score)>
            {
                ("Player1", 100),
                ("Player2", 85),
                ("Player3", 75),
                ("Player4", 65)
            };

            // Ustaw wynik gracza
            var scoreTextView = FindViewById<TextView>(Resource.Id.player_score);
            scoreTextView.Text = currentPlayerScore.ToString();

            // Wypełnij listę rankingową
            _rankingList = FindViewById<LinearLayout>(Resource.Id.ranking_list);
            PopulateRankingList(ranking, currentPlayer);

            // Obsługa przycisku nowej gry
            var newGameButton = FindViewById<Button>(Resource.Id.new_game_button);
            newGameButton.Click += (sender, args) =>
            {
                var intent = new Android.Content.Intent(this, typeof(MainActivity));
                intent.AddFlags(Android.Content.ActivityFlags.ClearTop | Android.Content.ActivityFlags.NewTask);
                StartActivity(intent);
                Finish(); // Zamknięcie obecnego ekranu
            };
        }

        private void PopulateRankingList(List<(string Name, int Score)> ranking, string currentPlayer)
        {
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
