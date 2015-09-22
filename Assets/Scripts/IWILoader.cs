using UnityEngine;
using System.Collections;

using System.IO;
using System.Text;

namespace Potion
{
    public class MaterialCreator
    {
        private FileStream fs;
        private BinaryReader br;

        /**
         * Creates a material from the filename
         * @param materialName Name of the material file in the zip
         */
        public void CreateMaterial( string materialName )
        {
            using( fs = new FileStream( Common.CoD2Path + "main\\materials\\" + materialName, FileMode.Open, FileAccess.Read ) )
            {
                using( br = new BinaryReader( fs, new ASCIIEncoding() ) )
                {
                    if( fs.CanRead )
                        StartReading();
                }
            }
        }

        private void StartReading()
        {
            string materialName = GetMaterialName();
            string textureName = GetColorMapName();
        }

        private string GetMaterialName()
        {
            // DWORD 0 = Material name offset
            br.BaseStream.Seek( 0, SeekOrigin.Begin );
            uint offset = br.ReadUInt32();

            return GetString( offset );
        }

        private string GetColorMapName()
        {
            // DWORD 1 = Texture name offset
            br.BaseStream.Seek( 4, SeekOrigin.Begin );
            uint offset = br.ReadUInt32();

            return GetString( offset );
        }

        private string GetString( uint offset, byte terminatingChar = 0x00 )
        {
            byte[] rawName = new byte[64];

            br.BaseStream.Seek( offset, SeekOrigin.Begin );

            for( int i = 0; i < 64; i++ )
            {
                rawName[ i ] = br.ReadByte();

                if( rawName[ i ] == terminatingChar )
                    break;
            }

            var str = System.Text.Encoding.ASCII.GetString( rawName );
            return str;
        }

        /**
         * Get material name
         * Load material file from zip using name
         * Parse material file to get texture name
         * Load texture file from zip using texture name
         * Extract DXT data from texture file and input into Texture2D
         * 
         */
    }
}