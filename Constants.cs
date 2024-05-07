using System;
using System.IO;
using System.Globalization;

public static class Constants {
    public static class Paths {
        public static readonly string BaseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string DirtyJammerOutputFilePath = Path.Combine(BaseDirectory, "jammer-01.csv");
        public static readonly string CleanedJammerOutputFilePath = Path.Combine(BaseDirectory, "jammer-01-cleaned.csv");
        public static readonly string OuiFilePath = "/usr/share/ieee-data/oui.txt";
    }

    public static class Colours { 
        public const string Green = "#2ec04f";
        public const string Red = "#f23f42";
        public const string DarkGrey = "#282c34";
        public const string Grey = "#404249";
    }

    public static class Utils {
        public static byte[] ParseMacAddress(string mac) {
            string[] macParts = mac.Split(":");
            byte[] macBytes = new byte[macParts.Length];
            for (int i = 0; i < macParts.Length; i++)
            {
                macBytes[i] = byte.Parse(macParts[i], NumberStyles.HexNumber);
            }
            return macBytes;
        }
    }
}