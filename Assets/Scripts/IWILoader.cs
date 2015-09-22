using UnityEngine;
using System.Collections.Generic;

using System.IO;
using System.Text;

using System;

namespace Potion
{
    public class IWILoader
    {
        enum Usage
        {
            Color = 0x00,
            Default = 0x01, // Fallback texture for engine
            Skybox = 0x05,
        }

        enum Format
        {
            ARGB32 = 0x01,
            RGB24 = 0x02,
            GA16 = 0x03,
            A8 = 0x04,
            DXT1 = 0x0B,
            DXT3 = 0x0C,
            DXT5 = 0x0D
        }

        struct STexture
        {
            public string name;
            public ushort width, height;

            public uint fileSize;

            public Usage usage;
            public Format format;

            public uint textureOffset, mipMap1Offset, mipMap2Offset;

            public bool mipMapped;

            public List<byte> rawTextureData;
            public List<byte> rawMipMapData_01;
            public List<byte> rawMipMapData_02;
        };

        FileStream fs;
        BinaryReader br;

        STexture currentTexture;

        public Texture2D CreateTexture( string textureName ) 
        {
            currentTexture = new STexture();
            currentTexture.name = textureName;

            Texture2D ret = null;

            using( fs = new FileStream( Utils.CoD2Path + "main\\images\\" + textureName + ".iwi", FileMode.Open, FileAccess.Read ) ) {
                using( br = new BinaryReader( fs, new ASCIIEncoding() ) ) {
                    if( fs.CanRead )
                        ret = StartReading();
                }
            }

            return ret;
        }

        private Texture2D StartReading() 
        {
            br.BaseStream.Seek( 0, SeekOrigin.Begin );
            string ident = GetHeaderIdentifier(); // IWi5

            if( ident != "IWi5" )
                throw new Exception( "File is not of a valid type. Needs to be IWi v5" );

            ReadHeader();
            SetRawTextureData();

            return ConstructUnityTextureFromIWI();
        }

        string GetHeaderIdentifier() 
        {
            byte[] chunk;
            chunk = br.ReadBytes( 4 );

            StringBuilder ident = new StringBuilder();

            for( int i = 0; i < 3; i++ ) {
                ident.Append( (char) chunk[i] );
            }
            ident.Append( (int) chunk[3] );

            return ident.ToString();
        }

        private void ReadHeader() 
        {
            br.BaseStream.Seek( 4, SeekOrigin.Begin ); // Skip identifier (4 DWORDs)

            currentTexture.format = (Format) br.ReadByte();
            currentTexture.usage = (Usage) br.ReadByte();

            currentTexture.width = br.ReadUInt16();
            currentTexture.height = br.ReadUInt16();

            br.BaseStream.Seek( 2, SeekOrigin.Current ); // Skip unknown data

            currentTexture.fileSize = br.ReadUInt32();

            currentTexture.textureOffset = br.ReadUInt32();
            currentTexture.mipMap1Offset = br.ReadUInt32();
            currentTexture.mipMap2Offset = br.ReadUInt32();

            if( currentTexture.textureOffset == currentTexture.mipMap1Offset ) {
                currentTexture.mipMapped = true;
            } else {
                currentTexture.mipMapped = false;
            }
        }

        private void SetRawTextureData() 
        {
            br.BaseStream.Seek( currentTexture.textureOffset, SeekOrigin.Begin );

            byte[] texData = br.ReadBytes( (int) ( currentTexture.fileSize - currentTexture.textureOffset ) );
            currentTexture.rawTextureData = new List<byte>( texData );
        }

        private Texture2D ConstructUnityTextureFromIWI() 
        {
            TextureFormat f = TextureFormat.DXT5;

            switch( currentTexture.format ) {
                case Format.DXT1:
                    f = TextureFormat.DXT1;
                    break;

                case Format.DXT3:
                    // :( lets hope DXT3 works the same as DXT5 lol
                    break;

                case Format.DXT5:
                    f = TextureFormat.DXT5;
                    break;

                case Format.RGB24:
                    f = TextureFormat.RGB24;
                    break;
            }

            Texture2D ret = new Texture2D( currentTexture.width, currentTexture.height, f, /*currentTexture.mipMapped*/false );
            ret.LoadRawTextureData( currentTexture.rawTextureData.ToArray() );

            ret.Apply();

            return ret;
        }
    }

    public class MaterialCreator
    {
        private FileStream fs;
        private BinaryReader br;

        IWILoader iwiLoader;

        public MaterialCreator() {
            iwiLoader = new IWILoader();
        }

        public Material CreateMaterial( string materialName )
        {
            Material ret = null;

            using( fs = new FileStream( Utils.CoD2Path + "main\\materials\\" + materialName, FileMode.Open, FileAccess.Read ) )
            {
                using( br = new BinaryReader( fs, new ASCIIEncoding() ) )
                {
                    if( fs.CanRead )
                        ret = StartReading();
                }
            }

            ret.name = materialName;
            return ret;
        }

        private Material StartReading()
        {
            string materialName = GetMaterialName();
            string textureName = GetColorMapName();

            //Debug.Log( "Opening... " + textureName + " from material file " + materialName );

            Texture2D tex = iwiLoader.CreateTexture( textureName );

            Material ret = new Material( Resources.Load<Material>("main") );
            ret.mainTexture = tex;

            return ret;
        }

        private string GetMaterialName()
        {
            // DWORD 0 = Material name offset
            br.BaseStream.Seek( 0, SeekOrigin.Begin );
            uint offset = br.ReadUInt32();

            br.BaseStream.Seek( offset, SeekOrigin.Begin );
            return br.ReadStringTerminated( 0x00 );
        }

        private string GetColorMapName()
        {
            // DWORD 1 = Texture name offset
            br.BaseStream.Seek( 4, SeekOrigin.Begin );
            uint offset = br.ReadUInt32();

            br.BaseStream.Seek( offset, SeekOrigin.Begin );
            return br.ReadStringTerminated( 0x00 );
        }
    }
}