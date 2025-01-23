using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;

namespace QuizApp
{
    [Activity(Label = "IntermediateScreenActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class IntermediateScreenActivity : Activity
    {
        private LinearLayout _rankingContainer;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.intermediate_screen);

            // Pobierz dane z poprzedniej aktywności
            var wasAnswerCorrect = Intent.GetBooleanExtra("isAnswerCorrect", false);
            var playersRanking = Intent.GetStringArrayListExtra("rankingList")?.ToList() ?? new List<string>();
            var isLastQuestion = Intent.GetBooleanExtra("isLastQuestion", false);

            // Ustaw informację o poprawności odpowiedzi
            var feedbackTextView = FindViewById<TextView>(Resource.Id.answer_feedback);
            feedbackTextView.Text = wasAnswerCorrect ? "Dobra odpowiedź!" : "Zła odpowiedź!";

            // Wypełnij ranking
            _rankingContainer = FindViewById<LinearLayout>(Resource.Id.ranking_container);
            PopulateRanking(playersRanking);

            // Obsługa przycisku przejścia dalej
            var nextQuestionButton = FindViewById<Button>(Resource.Id.next_question_button);
            nextQuestionButton.Text = isLastQuestion ? "Zakończ quiz" : "Kolejne pytanie";
            nextQuestionButton.Click += (sender, e) =>
            {
                if (isLastQuestion)
                {
                    var intent = new Android.Content.Intent(this, typeof(QuizEndActivity));
                    StartActivity(intent);
                }
                else
                {
                    var intent = new Android.Content.Intent(this, typeof(QuestionActivity));
                    StartActivity(intent);
                }
                Finish();
            };

        }

        private void PopulateRanking(List<string> playersRanking)
        {
            for (int i = 0; i < playersRanking.Count; i++)
            {
                var playerData = playersRanking[i].Split(':');
                var playerName = playerData[0];
                var playerScore = playerData.Length > 1 ? playerData[1] : "0";

                var rankView = LayoutInflater.From(this).Inflate(Resource.Layout.single_rank_item, _rankingContainer, false);

                var rankNumber = rankView.FindViewById<TextView>(Resource.Id.rank_number);
                var nameView = rankView.FindViewById<TextView>(Resource.Id.player_name);
                var scoreView = rankView.FindViewById<TextView>(Resource.Id.player_score);

                rankNumber.Text = $"{i + 1}.";
                nameView.Text = playerName;
                scoreView.Text = $"{playerScore} pkt";

                _rankingContainer.AddView(rankView);

                if (i < playersRanking.Count - 1)
                {
                    var divider = new View(this)
                    {
                        LayoutParameters = new LinearLayout.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            1)
                    };
                    divider.SetBackgroundColor(Android.Graphics.Color.Gray);
                    _rankingContainer.AddView(divider);
                }
            }
        }
    }
}
