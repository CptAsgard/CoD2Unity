using System.IO;

namespace Potion
{
    public static class Utils
    {
        public static string CoD2Path = "C:\\Program Files (x86)\\Activision\\Call of Duty 2\\";

        public static string ReadStringTerminated( this BinaryReader br, byte terminatingChar = 0x00 ) 
        {
            char[] rawName = new char[64];

            int i = 0;
            for( i = 0; i < 64; i++ ) {
                if( br.PeekChar() == terminatingChar )
                    break;

                rawName[i] = br.ReadChar();
            }

            return new string( rawName ).Replace( "\0", string.Empty ).Trim();
        }

        public static string ReadStringLength( this BinaryReader br, uint length ) 
        {
            char[] rawName = new char[length];

            rawName = br.ReadChars( (int) length );

            return new string( rawName ).Replace( "\0", string.Empty ).Trim();
        }
    }
}