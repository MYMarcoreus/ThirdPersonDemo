namespace Network.Core
{
    public enum MessageParseErrorCode {
         eNoError = 0,
         eInvalidCheckCode = 1,
         eNotReceiveFullHeader = 2,
         eInvalidFullLength = 3,
         eNotReceiveFullLength = 5,
         eParseError = 6,
         eUnkonwnMessage = 7,
     };
    
    public class ProtobufCodec
    {
        
    }
}