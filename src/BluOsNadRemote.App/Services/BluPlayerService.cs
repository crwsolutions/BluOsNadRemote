using Blu4Net;
using BluOsNadRemote.App.Utils;
using System.Reactive.Linq;

namespace BluOsNadRemote.App.Services
{
    public partial class BluPlayerService
    {
        private BluPlayer _player;

        [Dependency]
        private readonly ConfigurationService _configurationService;

        public bool IsInitialized { get; set; }

        public async Task<string> InitializeAsync()
        {
            Uri uri;
            if (_configurationService.SelectedEndpoint == null)
            {
                uri = await BluEnvironment.ResolveEndpoints().FirstOrDefaultAsync();

                if (uri == null)
                {
                    return "Player not found";
                }

                _configurationService.SetEndpoint(uri);
            }
            else
            {
                uri = _configurationService.SelectedEndpoint.Uri;
            }

            _player = await BluPlayer.Connect(uri);
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