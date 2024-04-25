public interface IJammableDevice {
    byte[] GenerateDeauthFrame();
    bool IsJammed {get; set;}
    int Channel {get; set;}
}