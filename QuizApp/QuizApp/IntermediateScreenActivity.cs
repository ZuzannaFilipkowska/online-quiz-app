using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;

namespace QuizApp
{
    [Activity(Label = "IntermediateScreenActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class IntermediateScreenActivity : Activity
    {
        private LinearLayout _rankingContainer;
        private ProgressBar _loader;
        private bool _allPlayersAnswered = false;
        private QuizService.QuizServiceClient _grpcClient;
        private string _gameId;
        private string _playerId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.intermediate_screen);

            // Pobierz dane z poprzedniej aktywności
            var wasAnswerCorrect = Intent.GetBooleanExtra("isAnswerCorrect", false);
            _gameId = Intent.GetStringExtra("gameId");
            _playerId = Intent.GetStringExtra("playerId");

            // Ustaw informację o poprawności odpowiedzi
            var feedbackTextView = FindViewById<TextView>(Resource.Id.answer_feedback);
            feedbackTextView.Text = wasAnswerCorrect ? "Dobra odpowiedź!" : "Zła odpowiedź!";

            // Wypełnij ranking
            _rankingContainer = FindViewById<LinearLayout>(Resource.Id.ranking_container);
            _loader = FindViewById<ProgressBar>(Resource.Id.loader);


            Task.Run(async () => await ReceiveGameUpdates());
        }

        private async Task ReceiveGameUpdates()
        {
            _grpcClient = GrpcClientProvider.Instance.GetClient();
            var cts = new CancellationTokenSource(); // Token do kontrolowania pętli
            try
            {
                using (var call = _grpcClient.StreamGameUpdates(new GameRequest { GameId = _gameId }))
                {
                    while (await call.ResponseStream.MoveNext(cancellationToken: cts.Token))
                    {
                        var gameDetails = call.ResponseStream.Current;

                        RunOnUiThread(async () =>
                        {
                            ShowRanking(gameDetails.Players
                                .ToList()
                                .OrderByDescending(p => p.Score)
                                .Select(p => $"{p.Name}:{p.Score}")
                                .ToList());

                            if (gameDetails.AllPlayersAnswered)
                            {
                                _allPlayersAnswered = true;

                                await Task.Delay(3000);
                                cts.Cancel(); // Anulowanie dalszych iteracji
                                var intent = new Android.Content.Intent(this, typeof(QuestionActivity));
                                intent.PutExtra("gameId", _gameId);
                                intent.PutExtra("playerId", _playerId);
                                StartActivity(intent);
                                Finish();
                            }
                        });

                        if (cts.Token.IsCancellationRequested)
                        {
                            break; // Bezpieczne przerwanie pętli
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, $"Błąd: {ex.Message}", ToastLength.Long).Show();
                });
            }
            finally
            {
                cts.Dispose(); // Zwolnij zasoby tokena
            }
        }


        private void PopulateRanking(List<string> playersRanking)
        {
            _rankingContainer.RemoveAllViews(); // Wyczyść poprzedni ranking

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

        private void ShowRanking(List<string> playersRanking)
        {
            // Ukryj loader
            _loader.Visibility = ViewStates.Gone;

            // Pokaż ranking
            var rankingContainer = FindViewById<LinearLayout>(Resource.Id.ranking_container);
            rankingContainer.Visibility = ViewStates.Visible;

            // Wypełnij ranking danymi
            PopulateRanking(playersRanking);
        }

    }
}