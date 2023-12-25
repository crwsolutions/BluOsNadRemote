using Blu4Net;
using BluOsNadRemote.App.Utils;
using System.Reactive.Linq;

namespace BluOsNadRemote.App.Services
{
    public partial class BluPlayerService
    {
        private BluPlayer _player;

        [Dependency]
        private readonly PreferencesRepository _preferencesRepository;

        public bool IsInitialized { get; set; }

        public async Task<string> InitializeAsync()
        {
            Uri endpoint;
            if (_preferencesRepository.SelectedEndpoint == null)
            {
                endpoint = await BluEnvironment.ResolveEndpoints().FirstOrDefaultAsync();

                if (endpoint == null)
                {
                    return "Player not found";
                }

                _preferencesRepository.SetEndpoint(endpoint);
            }
            else
            {
                endpoint = _preferencesRepository.SelectedEndpoint.Uri;
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