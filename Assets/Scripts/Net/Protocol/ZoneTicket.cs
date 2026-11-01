namespace ZenoniaProto.Net
{
    /// <summary>What a zone Hello's token must look like on a real server before the API is even asked: server-api issues 32 random bytes as base64url. Whatever a client sends here is hostile, and the request to the API carries it.</summary>
    public static class ZoneTicket
    {
        public const int MinLength = 20;

        public static bool LooksValid(string token)
        {
            if (token == null || token.Length < MinLength || token.Length > NetProtocol.MaxTokenLength) return false;
            foreach (char c in token)
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '-' || c == '_')) return false;
            return true;
        }
    }
}
