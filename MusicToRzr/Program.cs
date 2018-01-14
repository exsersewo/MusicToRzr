using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using iTunesLib;
using Corale.Colore.Core;
using Corale.Colore.Razer.Keyboard;
using SpotifyAPI.Local;

namespace MusicToRzr
{
    class Program
    {
        static IiTunes _iTunes;
        static SpotifyLocalAPI Spotify;
        static string[] Arguments;
        static Timer tmer;
        static float progress;
        static bool iTunesFeat;
        static bool SpotifyFeat;
        static bool Debug;

        static void ConfigureServices()
        {
            if (Arguments.Contains("-itunes"))
            { iTunesFeat = true; SpotifyFeat = false; }
            if (Arguments.Contains("-spotify"))
            { iTunesFeat = false; SpotifyFeat = true; }
        }

        static void InitializeServices()
        {
            ConfigureServices();
            if (iTunesFeat)
                _iTunes = new iTunesApp();
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
            }
        }

        static void Main(string[] args)
        {
            int interval;
            Arguments = args;

            InitializeServices();

            if (Arguments.FirstOrDefault(x=>x.StartsWith("-interval=", StringComparison.Ordinal))!=null)
            {
                var tmparg = Arguments.FirstOrDefault(x => x.StartsWith("-interval=", StringComparison.Ordinal));
                interval = Convert.ToInt32(tmparg.Remove(0, "-interval=".Length)); }
            else
                interval = 500;
            if (Arguments.Contains("-debug"))
            { Debug = true; Console.WriteLine("\nLoaded Debug Version"); }
            else
                Debug = false;
            tmer = new Timer
            {
                Interval = interval
            };
            tmer.Elapsed += (s,e) => Execute();
            tmer.AutoReset = true;
            Console.WriteLine($"Hello!! I work automatically.\nI poll data every {interval}ms and toggle the media keys depending on playback status.\n\nYou can optionally (using the argument \"--showprogress\") show the progress in the number bar.");
            tmer.Start();
            while (true) { }
        }

        static void Execute()
        {
            try
            {
                if(CheckForConnection())
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
                    if (Debug)
                        Console.WriteLine(progress.ToString("F0") + "%");
                    if (Arguments.Contains("--showprogress"))
                    {

                        if (!CheckForPlaying())
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
                    if (CheckForPlaying())
                    {
                        Chroma.Instance.Keyboard.SetKey(Key.F6, new Color(0, 255, 0));
                        if (Debug)
                            Console.WriteLine("Playing music");
                    }
                    else
                    {
                        Chroma.Instance.Keyboard.SetKey(Key.F6, new Color(255, 0, 0));
                        if (Debug)
                            Console.WriteLine("Not playing music");
                    }
                }
            }
            catch (Exception ex)
            {
                if(ex.Message.Contains("E_FAIL"))
                    Console.WriteLine("I failed pulling data from the media player");
                if (ex.Message.Contains("Object reference not set"))
                    Console.WriteLine("Something was null. :/");
                if (Debug)
                    Console.WriteLine(ex);
            }

            tmer.Start();
        }

        static List<Key> GetKeysFromProgress()
        {            
            List<Key> tmprr = new List<Key>();
            if(progress<10)
            {
                for (int x = 1; x <= progress; x++)
                {
                    if (x == (int)progress) { continue; }
                    else { tmprr.Add(GetKeyFromNumber(x)); }
                }
            }
            else
            {
                int tmpval=0;
                int tmp10th = ((int)progress / 10) * 10;
                tmpval = (int)progress - tmp10th;
                for (int x = 1; x <= tmpval; x++)
                {
                    if (x == tmp10th / 10) { continue; }
                    else{ tmprr.Add(GetKeyFromNumber(x)); }
                }
            }

            return tmprr;
        }
        static Key GetKeyFromNumber(int number)
        {
            Key rtn = 0;
            switch(number)
            {
                case 1:
                    rtn = Key.D1;
                    break;
                case 2:
                    rtn =  Key.D2;
                    break;
                case 3:
                    rtn =  Key.D3;
                    break;
                case 4:
                    rtn =  Key.D4;
                    break;
                case 5:
                    rtn =  Key.D5;
                    break;
                case 6:
                    rtn =  Key.D6;
                    break;
                case 7:
                    rtn =  Key.D7;
                    break;
                case 8:
                    rtn =  Key.D8;
                    break;
                case 9:
                    rtn =  Key.D9;
                    break;
                case 0:
                    rtn =  Key.D0;
                    break;
            }
            return rtn;
        }
        static List<Key> GetNumberRow()
        {
            List<Key> rtn = new List<Key>();
            for(int x=0;x<10;x++)
            {
                rtn.Add(GetKeyFromNumber(x));
            }
            return rtn;
        }
        static int GetNumberFromKey(Key key)
        {
            int rtn = 0;
            switch (key)
            {
                case Key.D1:
                    rtn = 1;
                    break;
                case Key.D2:
                    rtn = 2;
                    break;
                case Key.D3:
                    rtn = 3;
                    break;
                case Key.D4:
                    rtn = 4;
                    break;
                case Key.D5:
                    rtn = 5;
                    break;
                case Key.D6:
                    rtn = 6;
                    break;
                case Key.D7:
                    rtn = 7;
                    break;
                case Key.D8:
                    rtn = 8;
                    break;
                case Key.D9:
                    rtn = 9;
                    break;
                case Key.D0:
                    rtn = 0;
                    break;
            }
            return rtn;
        }
        static void CleanRow()
        {
            var keys = GetNumberRow();
            foreach(var key in keys)
            {
                if(Chroma.Instance.Keyboard.IsSet(key) && (int)progress/10 == GetNumberFromKey(key))
                { continue; }
                else
                {
                    Chroma.Instance.Keyboard.SetKey(key, new Color());
                }
            }
        }
        static bool CheckForPlaying()
        {
            if(iTunesFeat)
            { if(_iTunes.PlayerState == ITPlayerState.ITPlayerStatePlaying) { return true; } else { return false; } }
            if (SpotifyFeat)
            { return Spotify.GetStatus().Playing; }
            return false;
        }
        static bool CheckForConnection()
        {
            if(iTunesFeat)
            {
                if (_iTunes.Version != null)
                    return true;
                else
                    return false;
            }
            if(SpotifyFeat)
            {
                if (!SpotifyLocalAPI.IsSpotifyRunning() || !SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                    return false;
                else if(SpotifyLocalAPI.IsSpotifyRunning() && SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                    return true;
            }
            return false;
        }
    }
}
