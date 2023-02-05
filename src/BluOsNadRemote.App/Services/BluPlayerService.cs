using Blu4Net;
using BluOsNadRemote.App.Utils;
using System.Reactive.Linq;

namespace BluOsNadRemote.App.Services
{
    public class BluPlayerService
    {
        private BluPlayer _player;

        public bool IsInitialized { get; set; }

        public async Task<string> InitializeAsync()
        {
            Uri endpoint;
            if (Preferences.Endpoint == null)
            {
                endpoint = await BluEnvironment.ResolveEndpoints().FirstOrDefaultAsync();

                if (endpoint == null)
                {
                    return "Player not found";
                }

                Preferences.Endpoint = endpoint.ToString();
            }
            else
            {
                endpoint = new Uri(Preferences.Endpoint, UriKind.RelativeOrAbsolute);
            }

            _player = await BluPlayer.Connect(endpoint);
            Console.WriteLine($"Player: {_player}");

#if DEBUG            
            _player.Log = new DebugTextWriter();
#endif
            IsInitialized = true;

            return _player.ToString();
        }

        public BluPlayer BluPlayer => _player;

        public MusicContentEntry MusicContentEntry { get; set; }
        public MusicContentNode MusicContentNode { get; set; }

    }
}