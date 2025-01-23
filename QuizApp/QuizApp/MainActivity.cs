using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Widget;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Google.Android.Material.TextField;
using System.Threading.Tasks;
using Android.Content;

namespace QuizApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private TextInputEditText _gameCodeInput;
        private TextInputEditText _nickInput;
        private TextView _gameCodeError;
        private TextView _nickError;
        private TextView _joinError;
        private QuizService.QuizServiceClient _grpcClient;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            var joinButton = FindViewById<Button>(Resource.Id.joinButton);
            joinButton.Click += (s, e) => OnJoin();

            _gameCodeInput = FindViewById<TextInputEditText>(Resource.Id.gameCodeInput);
            _nickInput = FindViewById<TextInputEditText>(Resource.Id.nickInput);
            _gameCodeError = FindViewById<TextView>(Resource.Id.gameCodeError);
            _nickError = FindViewById<TextView>(Resource.Id.nickError);
            _joinError = FindViewById<TextView>(Resource.Id.joinError);

            _gameCodeInput.TextChanged += (s, e) => _gameCodeError.Visibility = ViewStates.Invisible;
            _nickInput.TextChanged += (s, e) => _nickError.Visibility = ViewStates.Invisible;

            _grpcClient = GrpcClientProvider.Instance.GetClient();

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
                return true;

            return base.OnOptionsItemSelected(item);
        }

        private async void OnJoin()
        {
            var gameCode = _gameCodeInput.Text;
            var nick = _nickInput.Text;

            if (!ValidateData(gameCode, nick))
                return;


            await JoinTheGame(gameCode, nick);
        }

        private async Task JoinTheGame(string gameCode, string nick)
        {
            try
            {

                var joinRequest = new JoinGameRequest
                {
                    GameCode = gameCode,
                    PlayerName = nick
                };

                var response = await _grpcClient.JoinGameAsync(joinRequest);

                if (response.IsJoined)
                {
                    Toast.MakeText(this, response.Message, ToastLength.Long).Show();
                    NavigateToWaitingPage(response.GameId);
                }
            }
            catch (Exception e)
            {
                _joinError.Visibility = ViewStates.Visible;
                _joinError.Text = e.Message;
            }
        }

        private void NavigateToWaitingPage(string gameId)
        {
            var intent = new Intent(this, typeof(WaitingPageActivity));
            intent.PutExtra("GameId", gameId);
            StartActivity(intent);
        }

        private bool ValidateData(string gameCode, string nickInput)
        {
            _gameCodeError.Visibility = StringContentToVisibility(_gameCodeInput.Text);
            _nickError.Visibility = StringContentToVisibility(_nickInput.Text);

            return !string.IsNullOrEmpty(gameCode) && !string.IsNullOrEmpty(nickInput);
        }

        private ViewStates StringContentToVisibility(string value) => string.IsNullOrEmpty(value) ? ViewStates.Visible : ViewStates.Invisible;

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}