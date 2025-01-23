using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Grpc.Core;
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
                var gameId = Intent.GetStringExtra("gameId");
                var playerId = Intent.GetStringExtra("playerId");

                using var responseStream = grpcClient.WaitForGameStart(new GameRequest { GameId = gameId });

                await foreach (var startGameResponse in responseStream.ResponseStream.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    if (startGameResponse.IsStarted)
                    {
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, "Game started!", ToastLength.Long).Show();
                            var intent = new Intent(this, typeof(QuestionActivity));
                            intent.PutExtra("gameId", gameId);
                            intent.PutExtra("playerId", playerId);
                            StartActivity(intent);
                            Finish();
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

        protected override void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            base.OnDestroy();
        }
    }
}