using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MusicToRzr
{
    public class FooReturn
    {
        /// <summary>
        /// Returns empty if title empty
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Returns empty if Artist empty
        /// </summary>
        [JsonProperty(PropertyName = "artist")]
        public string Artist { get; set; }

        /// <summary>
        /// Returns empty if Album empty
        /// </summary>
        [JsonProperty(PropertyName = "album")]
        public string Album { get; set; }

        /// <summary>
        /// Returns empty if not playing from playlist
        /// </summary>
        [JsonProperty(PropertyName = "playlist")]
        public string Playlist { get; set; }

        /// <summary>
        /// Is Foobar currently Playing?
        /// </summary>
        [JsonProperty(PropertyName = "isPlaying"), JsonConverter(typeof(BoolConverter))]
        public bool Playing { get; set; }

        /// <summary>
        /// ElapsedTime in seconds
        /// </summary>
        [JsonProperty(PropertyName = "elapsedTime")]
        public int ElapsedTime { get; set; }

        /// <summary>
        /// TrackLength in seconds
        /// </summary>
        [JsonProperty(PropertyName = "trackLength")]
        public int TrackLength { get; set; }
    }

    public class Foobar
    {
        public string Server { get => _server; }

        public int Port { get => _port; }

        static string _server;
        static int _port;

        public Foobar()
        { }

        static void SetData(string server, int port)
        {
            _server = server;
            _port = port;
        }

        /// <summary>
        /// Creates a new foobar instance
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>
        public Foobar(string server, int port)
            => SetData(server, port);

        public async Task<FooReturn> GetStatusAsync()
        {
            return await WebReq.GetFoobarAsync(_server, _port);
        }
    }
}
