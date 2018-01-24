using System;
using System.Linq;
using System.Timers;
using iTunesLib;
using Corale.Colore.Core;
using Corale.Colore.Razer.Keyboard;
using Corale.Colore.Razer.Keyboard.Effects;
using SpotifyAPI.Local;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace MusicToRzr
{
    public static class WebReq
    {
        /// <summary>
        /// Base Web scraper for returning content as string
        /// </summary>
        /// <param name="url">URL to connect to</param>
        /// <returns></returns>
        static async Task<string> ReturnString(Uri url)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (resp.IsSuccessStatusCode)
                    {
                        var responce = await resp.Content.ReadAsStringAsync();
                        resp.Dispose();
                        client.Dispose();
                        return responce;
                    }
                    else
                    {
                        resp.Dispose();
                        client.Dispose();
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets data from set location from foobar using foo_httpcontrol custom theme
        /// </summary>
        /// <param name="ip">IP of server to connect to</param>
        /// <param name="port">Port of server to connect to</param>
        /// <returns></returns>
        public static async Task<FooReturn> GetFoobarAsync(string ip, int port)
        {
            var temp = new Uri($"http://{ip}:{port}/statsreq/?param3=req.json");
            var resp = await ReturnString(temp);
            if (resp != null)
            {
                return JsonConvert.DeserializeObject<FooReturn>(resp);
            }
            return null;
        }
    }

    partial class Program
    {
        //Start Variables
        static IiTunes iTunes;
        static SpotifyLocalAPI Spotify;
        static string[] Arguments;
        static Timer tmer;
        static float progress;
        static bool iTunesFeat;
        static bool SpotifyFeat;
        static bool FoobarFeat;
        static bool Debug;
        static bool isspectrum;
        static Foobar Foobar;
        //End Variables

        /// <summary>
        /// Configures Which Service to use
        /// </summary>
        static void ConfigureServices()
        {
            if (Arguments.Contains("-itunes"))
            { iTunesFeat = true; SpotifyFeat = false; FoobarFeat = false; }

            if (Arguments.Contains("-spotify"))
            { iTunesFeat = false; SpotifyFeat = true; FoobarFeat = false; }

            if (Arguments.Contains("-foobar"))
            { iTunesFeat = false; SpotifyFeat = false; FoobarFeat = true; }
        }

        /// <summary>
        /// Create the instance to use
        /// </summary>
        static void InitializeServices()
        {
            ConfigureServices();
            if (iTunesFeat)
                iTunes = new iTunesApp(); //new instance of itunes using COM service

            if (SpotifyFeat)
            {
                //Check for spotify errors before connecting
                if (!SpotifyLocalAPI.IsSpotifyInstalled())
                    throw new Exception("Spotify Not Installed"); //Why run spotify mode if you don't have it installed?

                if (!SpotifyLocalAPI.IsSpotifyRunning())
                    SpotifyLocalAPI.RunSpotify(); //Start spotify if not running

                if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                    SpotifyLocalAPI.RunSpotifyWebHelper(); //Start webhelper if not running

                Spotify = new SpotifyLocalAPI(); //new instance

                if (!Spotify.Connect()) //critical
                    throw new Exception("Couldn't connect to spotify");
            }

            if(FoobarFeat)
            {
                var ip = Arguments.FirstOrDefault(x => x.StartsWith("--foo-ip=", StringComparison.Ordinal)); //gets foo-ip

                if (ip != null) //checks for null
                {
                    ip = ip.Remove(0, "--foo-ip=".Length); //remove the argument text

                    if (int.TryParse(Arguments.FirstOrDefault(x => x.StartsWith("--foo-port=", StringComparison.Ordinal)).Remove(0, "--foo-port=".Length), out int port) && ip != null) //tries to convert foo-port argument to int after removing text, and checks if ip isn't null
                    {
                        Foobar = new Foobar(ip, Convert.ToInt32(port)); //use user-args
                    }

                    return;
                }
                Foobar = new Foobar("localhost", 8888); //no user-args, use default
            }
        }

        static void Main(string[] args)
        {
            int interval = 1000; //use default of 1 second
            Arguments = args;
            isspectrum = false;

            InitializeServices();

            var rawintvl = Arguments.FirstOrDefault(x => x.StartsWith("-interval=", StringComparison.Ordinal));
            if (rawintvl != null) //if rawintvl not null
            {
                if (int.TryParse(rawintvl.Remove(0, "-interval=".Length), out int intvl)) //checks if interval exists, and sucessfully converted to int
                {
                    interval = intvl;
                }
            }

            if (Arguments.Contains("-debug")) //if use debug version
            {
                Debug = true;
                Console.WriteLine("\nLoaded Debug Version");
            }
            else
                Debug = false;

            tmer = new Timer
            {
                Interval = interval //new timer with interval
            };

            tmer.Elapsed += async (s,e) => await Execute(); //sets elapsed method
            tmer.AutoReset = true;

            Console.WriteLine($"Hello!! I work automatically.\nI poll data every {interval}ms and toggle the media keys depending on playback status.\n\nYou can optionally (using the argument \"--showprogress\") show the progress in the number bar.");

            tmer.Start();

            Console.ReadLine();
        }

        /// <summary>
        /// Main Execution method to parse progress & Media Keys
        /// </summary>
        /// <returns>Void</returns>
        static async Task Execute()
        {
            try
            {
                if(await CheckForConnection()) //is the service alive?
                {
                    if (await CheckForPlaying()) //is the music player playing music?
                    {
                        if (Arguments.Contains("--showprogress"))
                        {
                            //Begin Progress parser
                            if (iTunesFeat)
                                progress = (float)iTunes.PlayerPosition / iTunes.CurrentTrack.Duration * 100;

                            if (SpotifyFeat)
                            {
                                var status = Spotify.GetStatus();
                                if (!status.Track.IsAd())
                                {
                                    progress = (float)status.PlayingPosition / status.Track.Length * 100;
                                }
                                else
                                    progress = 0;
                            }

                            if (FoobarFeat)
                            {
                                var foodata = await Foobar.GetStatusAsync();
                                if (foodata != null)
                                {
                                    progress = (float)foodata.ElapsedTime / foodata.TrackLength * 100;
                                }
                            }
                            //End progress parser

                            if (Debug)
                                Console.WriteLine(progress.ToString("F0") + "%"); //If debug version, dump output

                            Chroma.Instance.Keyboard.SetKeys(GetKeysFromProgress(), new Color(5, 255, 101)); //Sets the key to current progress

                            if (Debug)
                                Console.WriteLine("Set Progress");

                            if ((int)progress % 10 == 0) //if it is divisible by 10 perfectly after casting to int
                            {
                                CleanRow();

                                if (Debug)
                                    Console.WriteLine("Nulled number row");
                            }

                            if (progress > 10) // if greater than 10
                            {
                                Chroma.Instance.Keyboard.SetKey(GetKeyFromNumber((int)progress / 10), new Color(255, 0, 255)); //set the 10th value
                                if (Debug)
                                    Console.WriteLine("Set 10th value key");
                            }
                        }
                        Chroma.Instance.Keyboard.SetKey(Key.F6, new Color(0, 255, 0)); //playing music, so set the play key to green

                        if (Debug)
                            Console.WriteLine("Playing music");

                        isspectrum = false; //disable spectrum
                    }
                    else
                    {
                        Chroma.Instance.Keyboard.SetKeys(GetNumberRow(), new Color()); //clean row

                        if (!isspectrum) //if not spectrumed
                        {
                            Chroma.Instance.Keyboard.SetEffect(Effect.SpectrumCycling); //set to spectrum
                            isspectrum = true; //enable spectrum to prevent loop

                            if (Debug)
                                Console.WriteLine("Enabled Spectrum");
                        }

                        if (Debug)
                            Console.WriteLine("Not playing music");
                    }
                }
            }
            catch
            {
                Chroma.Instance.Keyboard.SetEffect(Effect.SpectrumCycling); //if error'd, spectrum
            }

            tmer.Start(); //restart timer
        }

        /// <summary>
        /// Checks if service is playing music
        /// </summary>
        /// <returns>True if playing music, False if not playing music</returns>
        static async Task<bool> CheckForPlaying()
        {
            if(iTunesFeat)
            {
                if (iTunes.PlayerState == ITPlayerState.ITPlayerStatePlaying)
                    return true;
            }

            if (SpotifyFeat)
            {
                return Spotify.GetStatus().Playing;
            }

            if (FoobarFeat)
            {
                var resp = await Foobar.GetStatusAsync();

                if(resp!=null)
                    return resp.Playing;
            }
            return false;
        }

        /// <summary>
        /// Checks if any of the services has a connection
        /// </summary>
        /// <returns>True if service has a connection, false if not</returns>
        static async Task<bool> CheckForConnection()
        {
            if(iTunesFeat)
            {
                if (iTunes.Version != null)
                    return true;
            }

            if(SpotifyFeat)
            {
                if (SpotifyLocalAPI.IsSpotifyRunning() && SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                    return true;
            }

            if (FoobarFeat)
            {
                if (await Foobar.GetStatusAsync() != null)
                    return true;
            }
            return false;
        }
    }
}
