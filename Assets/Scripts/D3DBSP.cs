using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;

namespace Potion {

    public struct Lump {
        public string name;

        public uint offset;
        public uint length;
    };

    public struct TriangleSoup {
        public ushort materialID;

        public ushort drawOrder;

        public uint vertexOffset;
        public ushort vertexLength;

        public uint triangleOffset;
        public ushort triangleLength;
    };

    public class Triangle {
        public Triangle() {
            indices = new ushort[ 3];
        }

        public ushort[] indices;
    }

    public class Vertex {
        public Vertex() {
            position = new float[3];
            normal = new float[3];
            rgba = new byte[4];

            uv = new float[2];
            st = new float[2];

            unknown = new float[6];
        }

        public Vector3 PositionToVector3() {
            Vector3 pos = new Vector3( 
                position[0],
                position[1],
                position[2]
            );

            return pos;
        }

        public Vector2 UVToVector2() {
            Vector2 _uv = new Vector2(
                uv[ 0 ],
                uv[ 1 ]
            );

            return _uv;
        }

        public float[] position;
        public float[] normal;
        public byte[] rgba;

        public float[] uv;
        public float[] st;

        public float[] unknown;
    }

    public class MapMaterial {
        public string name;
        public long flags;
    }

    public class D3DBSP : MonoBehaviour {
        public GameObject root;

        Dictionary<int, string> lumpNames;

        List<Lump> lumps;
        List<TriangleSoup> triangleSoups;

        List<Vertex> vertices;
        List<Triangle> triangles;

        List<MapMaterial> materials;

        FileStream fs;
        BinaryReader br;

        MaterialCreator materialCreator;

        void Start()
        {
            Load();
        }

        void Load() {
            lumpNames = new Dictionary<int, string>(){
                { 0, "Materials" },
                { 1, "Lightmaps" },
                { 2, "Light Grid Hash" },
                { 3, "Light Grid Values" },
                { 4, "Planes" },
                { 5, "Brushsides" },
                { 6, "Brushes" },
                { 7, "TriangleSoups" },
                { 8, "Vertices" },
                { 9, "Triangles" },
                { 10, "Cull Groups" },
                { 11, "Cull Group Indexes" },
                { 17, "Portal Verts" },
                { 18, "Occluder" },
                { 19, "Occluder Planes" },
                { 20, "Occluder Edges" },
                { 21, "Occluder Indexes" },
                { 22, "AABB Trees" },
                { 23, "Cells" },
                { 24, "Portals" },
                { 25, "Nodes" },
                { 26, "Leafs" },
                { 27, "Leaf Brushes" },
                { 29, "Collision Verts" },
                { 30, "Collision Edges" },
                { 31, "Collision Tris" },
                { 32, "Collision Borders" },
                { 33, "Collision Parts" },
                { 34, "Collision AABBs" },
                { 35, "Models" },
                { 36, "Visibility" },
                { 37, "Entities" },
                { 38, "Paths" },
            };

            lumps = new List<Lump>();
            materials = new List<MapMaterial>();
            vertices = new List<Vertex>();
            triangles = new List<Triangle>();
            triangleSoups = new List<TriangleSoup>();

            materialCreator = new MaterialCreator();

            using( fs = new FileStream( Utils.CoD2Path + "main\\maps\\mp\\mp_carentan.d3dbsp", FileMode.Open, FileAccess.Read ) ) 
            {
                using( br = new BinaryReader( fs, new ASCIIEncoding() ) ) 
                {
                    if( fs.CanRead )
                        StartReading();
                }
            }
        }

        private void StartReading() {
            string ident = GetHeaderIdentifier();

            string mapNameWithExt = fs.Name.Substring( fs.Name.LastIndexOf( '/' ) + 1 );
            string mapName = mapNameWithExt.Substring( 0, mapNameWithExt.IndexOf( '.' ) );

            Debug.Log( "File format " + ident + " has been detected on " + mapName );

            if( ident != "IBSP4" )
                return;

            FillLumpList();
        }

        string GetHeaderIdentifier()
        {
            byte[] chunk;
            chunk = br.ReadBytes( 5 );

            StringBuilder ident = new StringBuilder();

            for( int i = 0; i < 4; i++ )
            {
                ident.Append( (char) chunk[ i ] );
            }
            ident.Append( (int) chunk[ 4 ] );

            return ident.ToString();
        }

        private void FillLumpList() {
            br.BaseStream.Seek( 8, SeekOrigin.Begin );

            for( int i = 0; i < 39; i++ ) {
                string lumpName;

                if( !lumpNames.ContainsKey( i ) )
                    lumpName = "UNKNOWN";
                else
                    lumpName = lumpNames[i];

                Lump l = new Lump();

                l.name = lumpName;
                l.length = br.ReadUInt32();
                l.offset = br.ReadUInt32();

                lumps.Insert( i, l );

                Debug.Log( "Lump[" + l.name + "] Length: " + l.length + " bytes | Offset: " + l.offset + " bytes" );
            }

            CreateMeshMagic();
        }

        private void FillMaterialList() {
            br.BaseStream.Seek( lumps[0].offset, SeekOrigin.Begin );

            for( int i = 0; i < lumps[0].length; i++ ) {
                MapMaterial m = new MapMaterial();

                m.name = Encoding.ASCII.GetString( br.ReadBytes( 64 ) ).Replace( "\0", string.Empty ).Trim();
                m.flags = br.ReadInt64();

                materials.Add( m );
            }
        }

        void FillSoupsList() {
            br.BaseStream.Seek( lumps[7].offset, SeekOrigin.Begin );

            for( int i = 0; i < lumps[7].length / 16; i++ ) {
                TriangleSoup t = new TriangleSoup();

                t.materialID = br.ReadUInt16();
                t.drawOrder = br.ReadUInt16();

                t.vertexOffset = br.ReadUInt32();
                t.vertexLength = br.ReadUInt16();

                t.triangleLength = br.ReadUInt16();
                t.triangleOffset = br.ReadUInt32();

                triangleSoups.Add( t );
            }
        }

        void FillVerticesList() {
            br.BaseStream.Seek( lumps[8].offset, SeekOrigin.Begin );

            for( int i = 0; i < lumps[8].length / 68; i++ ) {
                Vertex v = new Vertex();

                v.position[0] = br.ReadSingle();
                v.position[2] = br.ReadSingle(); // switch Y and Z, different engine
                v.position[1] = br.ReadSingle();

                v.normal[0] = br.ReadSingle();
                v.normal[1] = br.ReadSingle();
                v.normal[2] = br.ReadSingle();

                v.rgba[0] = br.ReadByte();
                v.rgba[1] = br.ReadByte();
                v.rgba[2] = br.ReadByte();
                v.rgba[3] = br.ReadByte();

                v.uv[0] = br.ReadSingle();
                v.uv[1] = br.ReadSingle();

                v.st[0] = br.ReadSingle();
                v.st[1] = br.ReadSingle();

                // Unknown.. skip. Texture rotation?
                br.BaseStream.Seek( 24, SeekOrigin.Current );

                vertices.Add( v );
            }
        }

        void FillTrianglesList() {
            br.BaseStream.Seek( lumps[9].offset, SeekOrigin.Begin );

            for( int i = 0; i < lumps[9].length / 6; i++ ) {
                Triangle t = new Triangle();

                t.indices[0] = br.ReadUInt16();
                t.indices[1] = br.ReadUInt16();
                t.indices[2] = br.ReadUInt16();

                triangles.Add( t );
            }
        }

        void CreateMeshMagic() {

            FillSoupsList();
            FillVerticesList();
            FillTrianglesList();
            FillMaterialList();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<int> triangleIndices = new List<int>();

            // 1 soup per material/mesh
            // Each mesh has a triangle_length and triangle_offset
            // Triangle_length is how many triangles the mesh uses
            // Triangle_offset is the offset into the triangle array

            // Each soup also defines a vertex_offset
            // First you look up the current triangle
            // Then you use the index pointed to by the triangle, plus the vertex_offset, to find the correct vertex

            for( int i = 0; i < this.triangleSoups.Count; i++ )
            {
                TriangleSoup currentSoup = this.triangleSoups[i];

                GameObject go = GameObject.CreatePrimitive( PrimitiveType.Cube );
                go.transform.parent = root.transform;

                Mesh m = new Mesh();
                go.GetComponent<MeshFilter>().mesh = m;

                int tri_count = (int) currentSoup.triangleLength / 3;
                for( int j = 0; j < tri_count; j++ )
                {
                    Triangle tri = this.triangles[(int) currentSoup.triangleOffset / 3 + j];

                    for( int vert_loop = 0; vert_loop < 3; vert_loop++ )
                    {
                        int offset = (int) tri.indices[vert_loop];

                        Vector3 pos = this.vertices[(int) currentSoup.vertexOffset + offset].PositionToVector3();
                        Vector2 uv = this.vertices[ (int) currentSoup.vertexOffset + offset ].UVToVector2();

                        triangleIndices.Add( vertices.Count );
                        vertices.Add( pos );
                        uvs.Add( uv );
                    }
                }

                if( vertices.Count > 0 )
                {
                    // Load required material here
                    Material newMat = materialCreator.CreateMaterial( materials[currentSoup.materialID].name );

                                                                     // noDraw
                    if( ( materials[currentSoup.materialID].flags & 0x0000000100000080 ) == 0 )
                        go.SetActive( false );

                    go.GetComponent<Renderer>().material = newMat;

                    m.vertices = vertices.ToArray();
                    m.triangles = triangleIndices.ToArray();
                    m.uv = uvs.ToArray();
                    
                    m.RecalculateNormals();

                    vertices.Clear();
                    triangleIndices.Clear();
                    uvs.Clear();
                }
                else
                {
                    Destroy( go );
                }

            }

            root.transform.localScale = new Vector3( 0.1f, 0.1f, 0.1f );

            Destroy( gameObject );
        }
    }
}