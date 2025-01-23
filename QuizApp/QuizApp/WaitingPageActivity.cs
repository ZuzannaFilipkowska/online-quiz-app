using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Newtonsoft.Json;

namespace QuizApp
{
    [Activity(Label = "WaitingPageActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class WaitingPageActivity : Activity
    {
        private CancellationTokenSource _cancellationTokenSource;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set the view for this activity
            SetContentView(Resource.Layout.waiting_page);

            // Access views if needed
            ProgressBar progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            TextView waitingText = FindViewById<TextView>(Resource.Id.waitingText);

            // Initialize cancellation token
            _cancellationTokenSource = new CancellationTokenSource();

            // Start waiting for server response
            WaitForStartSignal();
        }

        private async void WaitForStartSignal()
        {
            try
            {
                var grpcClient = GrpcClientProvider.Instance.GetClient();
                var gameId = Intent.GetStringExtra("GameId");

                using var responseStream = grpcClient.WaitForGameStart(new GameRequest { GameId = gameId });

                while (await responseStream.ResponseStream.MoveNext(_cancellationTokenSource.Token))
                {
                    var startGameResponse = responseStream.ResponseStream.Current;

                    if (startGameResponse.IsStarted)
                    {
                        // Game has started, fetch the first question
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, "Game started!", ToastLength.Long).Show();
                            FetchFirstQuestion(gameId);
                        });
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long).Show();
                });
            }
        }

        private async void FetchFirstQuestion(string gameId)
        {
            try
            {
                var grpcClient = GrpcClientProvider.Instance.GetClient();
                var response = await grpcClient.NextQuestionAsync(new GameRequest { GameId = gameId, CurrentQuestionIndex = 0 });

                // Navigate to the first question
                RunOnUiThread(() =>
                {
                    var intent = new Intent(this, typeof(QuestionActivity));
                    intent.PutExtra("QuestionText", response.QuestionText);
                    intent.PutExtra("Answers", JsonConvert.SerializeObject(response.Answers));
                    StartActivity(intent);
                    Finish();
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, $"Error fetching first question: {ex.Message}", ToastLength.Long).Show();
                });
            }
        }

        protected override void OnDestroy()
        {
            // Cancel the token to stop waiting when activity is destroyed
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            base.OnDestroy();
        }
    }
}