using System;
using System.Collections.Generic;
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
        static IiTunes _iTunes;
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
        /// Parse Foodata
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

        static void InitializeServices()
        {
            ConfigureServices();
            if (iTunesFeat)
            { _iTunes = new iTunesApp(); return; }
            if (SpotifyFeat)
            {
                Spotify = new SpotifyLocalAPI();
                if (!SpotifyLocalAPI.IsSpotifyInstalled())
                    throw new Exception("Spotify Not Installed");
                if (!SpotifyLocalAPI.IsSpotifyRunning())
                    SpotifyLocalAPI.RunSpotify();
                if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                    SpotifyLocalAPI.RunSpotifyWebHelper();
                if (!Spotify.Connect())
                    throw new Exception("Couldn't connect to spotify");
                return;
            }
            if(FoobarFeat)
            {
                var ip = Arguments.FirstOrDefault(x => x.StartsWith("--foo-ip=", StringComparison.Ordinal));
                if (ip != null)
                {
                    ip = ip.Remove(0, "--foo-ip=".Length);
                    if (int.TryParse(Arguments.FirstOrDefault(x => x.StartsWith("--foo-port=", StringComparison.Ordinal)).Remove(0, "--foo-port=".Length), out int port) && ip != null)
                    {
                        Foobar = new Foobar(ip, Convert.ToInt32(port));
                    }
                    return;
                }
                Foobar = new Foobar("localhost", 8888);
            }
        }

        static void Main(string[] args)
        {
            int interval;
            Arguments = args;
            isspectrum = false;

            InitializeServices();

            if (Arguments.FirstOrDefault(x=>x.StartsWith("-interval=", StringComparison.Ordinal))!=null)
            {
                var tmparg = Arguments.FirstOrDefault(x => x.StartsWith("-interval=", StringComparison.Ordinal));
                interval = Convert.ToInt32(tmparg.Remove(0, "-interval=".Length)); }
            else
                interval = 1000;
            if (Arguments.Contains("-debug"))
            { Debug = true; Console.WriteLine("\nLoaded Debug Version"); }
            else
                Debug = false;
            tmer = new Timer
            {
                Interval = interval
            };
            tmer.Elapsed += async (s,e) => await Execute();
            tmer.AutoReset = true;
            Console.WriteLine($"Hello!! I work automatically.\nI poll data every {interval}ms and toggle the media keys depending on playback status.\n\nYou can optionally (using the argument \"--showprogress\") show the progress in the number bar.");
            tmer.Start();
            while (true) { }
        }

        static async Task Execute()
        {
            try
            {
                if(await CheckForConnection())
                {
                    if (await CheckForPlaying())
                    {
                        if (iTunesFeat)
                            progress = (float)_iTunes.PlayerPosition / _iTunes.CurrentTrack.Duration * 100;
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
                        if (Debug)
                            Console.WriteLine(progress.ToString("F0") + "%");
                        if (Arguments.Contains("--showprogress"))
                        {

                            if (!await CheckForPlaying())
                            {
                                Chroma.Instance.Keyboard.SetKeys(GetNumberRow(), new Color());
                                if (Debug)
                                    Console.WriteLine("Nulled number row");
                            }
                            else
                            {
                                Chroma.Instance.Keyboard.SetKeys(GetKeysFromProgress(), new Color(5, 255, 101));
                                if (Debug)
                                    Console.WriteLine("Set Progress");
                                if ((int)progress % 10 == 0)
                                {
                                    CleanRow();
                                    if (Debug)
                                        Console.WriteLine("Nulled number row");
                                }
                                if (progress > 10)
                                {
                                    Chroma.Instance.Keyboard.SetKey(GetKeyFromNumber((int)progress / 10), new Color(255, 0, 255));
                                    if (Debug)
                                        Console.WriteLine("Set 10th key");
                                }
                            }
                        }
                        Chroma.Instance.Keyboard.SetKey(Key.F6, new Color(0, 255, 0));
                        if (Debug)
                            Console.WriteLine("Playing music");
                        isspectrum = false;
                    }
                    else
                    {
                        if (!isspectrum)
                        {
                            Chroma.Instance.Keyboard.SetEffect(Effect.SpectrumCycling);
                            isspectrum = true;
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
                Chroma.Instance.Keyboard.SetEffect(Effect.SpectrumCycling);
            }

            tmer.Start();
        }

        static async Task<bool> CheckForPlaying()
        {
            if(iTunesFeat)
            {
                if (_iTunes.PlayerState == ITPlayerState.ITPlayerStatePlaying)
                { return true; }
            }
            if (SpotifyFeat)
            { return Spotify.GetStatus().Playing; }
            if (FoobarFeat)
            {
                var resp = await Foobar.GetStatusAsync();
                if(resp!=null)
                { return resp.Playing; }
            }
            return false;
        }
        static async Task<bool> CheckForConnection()
        {
            if(iTunesFeat)
            {
                if (_iTunes.Version != null)
                { return true; }
                else
                { return false; }
            }
            if(SpotifyFeat)
            {
                if (!SpotifyLocalAPI.IsSpotifyRunning() || !SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                { return false; }
                else if (SpotifyLocalAPI.IsSpotifyRunning() && SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                { return true; }
            }
            if (FoobarFeat)
            {
                if (await Foobar.GetStatusAsync() != null)
                { return true; }
                else
                { return false; }
            }
            return false;
        }
    }
}
