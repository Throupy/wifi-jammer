using System;
using System.IO;

public static class Constants {
    public static class Paths {
        public static readonly string BaseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string DirtyJammerOutputFilePath = Path.Combine(BaseDirectory, "jammer-01.csv");
        public static readonly string CleanedJammerOutputFilePath = Path.Combine(BaseDirectory, "jammer-01-cleaned.csv");
    }

    public static class Colours { 
        public const string Green = "#2ec04f";
        public const string Red = "#f23f42";
        public const string DarkGrey = "#282c34";
        public const string Grey = "#404249";
    }
}