namespace Network.Core
{
    public class ConfigXML
    {
        public static readonly byte xor_code = 130;
        public static readonly uint version = 20230123;
        public static readonly uint recv_bytes_one = 1024 * 16;
        public static readonly uint recv_bytes_max = 1024 * 256;
        public static readonly uint send_bytes_one = 1024 * 16;
        public static readonly uint send_bytes_max = 1024 * 256;
        public static readonly float max_heart_time = 5.0f; //心跳时间
        public static readonly float auto_connect_time = 1.0f; //自动重连时间
        public static readonly string secure_code = "114514";
        public static readonly byte[] check_code = { (byte)'D', (byte)'E' };

        public const uint appid = 200;
        public const int gate_tcp_port = 11111;
        public const int gate_udp_port = 11112;
        public const int logic_tcp_port = 13333;
        public const int logic_udp_port = 14444;
    };
}
