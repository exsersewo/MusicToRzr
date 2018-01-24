using System;
using System.Collections.Generic;
using Corale.Colore.Core;
using Corale.Colore.Razer.Keyboard;
using Newtonsoft.Json;

namespace MusicToRzr
{
    /* Thanks to "Diver Dan" on StackOverflow https://stackoverflow.com/questions/14427596/convert-an-int-to-bool-with-json-net */
    //Converts "1" & "0" into bool representables
    public class BoolConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((bool)value) ? 1 : 0);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value.ToString() == "1";
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }
    }

    public partial class Program
    {
        /// <summary>
        /// Converts percentage to key representables
        /// </summary>
        /// <returns>List of Keys to light up</returns>
        static List<Key> GetKeysFromProgress()
        {
            List<Key> tmprr = new List<Key>();

            if (progress < 10)
            {
                for (int x = 1; x <= progress; x++)
                {
                    if (x == (int)progress) { continue; } //If already lit, skip

                    tmprr.Add(GetKeyFromNumber(x));
                }
            }
            else
            {
                int tmpval = 0;
                int tmp10th = ((int)progress / 10) * 10;
                tmpval = (int)progress - tmp10th;

                for (int x = 1; x <= tmpval; x++)
                {
                    if (x == tmp10th / 10) { continue; } //If already lit, skip

                    tmprr.Add(GetKeyFromNumber(x));
                }
            }

            return tmprr;
        }

        /// <summary>
        /// Gets the Key represented by the number
        /// </summary>
        /// <param name="number">Key to get</param>
        /// <returns>Key from Number</returns>
        static Key GetKeyFromNumber(int number)
        {
            Key rtn = 0;
            switch (number)
            {
                case 1:
                    rtn = Key.D1;
                    break;
                case 2:
                    rtn = Key.D2;
                    break;
                case 3:
                    rtn = Key.D3;
                    break;
                case 4:
                    rtn = Key.D4;
                    break;
                case 5:
                    rtn = Key.D5;
                    break;
                case 6:
                    rtn = Key.D6;
                    break;
                case 7:
                    rtn = Key.D7;
                    break;
                case 8:
                    rtn = Key.D8;
                    break;
                case 9:
                    rtn = Key.D9;
                    break;
                case 0:
                    rtn = Key.D0;
                    break;
            }
            return rtn;
        }

        /// <summary>
        /// Gets the entire number row
        /// </summary>
        /// <returns>Number Row List</returns>
        static List<Key> GetNumberRow()
        {
            List<Key> rtn = new List<Key>();
            for (int x = 0; x < 10; x++)
            {
                rtn.Add(GetKeyFromNumber(x));
            }
            return rtn;
        }

        /// <summary>
        /// Gets the number associated by the key
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <returns>Number from key</returns>
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

        /// <summary>
        /// Wipe number row
        /// </summary>
        static void CleanRow()
        {
            var keys = GetNumberRow();
            foreach (var key in keys)
            {
                if (Chroma.Instance.Keyboard.IsSet(key) && (int)progress / 10 == GetNumberFromKey(key)) { continue; } //If the key is the 10th value, don't do anything

                Chroma.Instance.Keyboard.SetKey(key, new Color());
            }
        }
    }
}
