using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;

namespace Potion {

    public struct Lump {
        public string Name;

        public UInt32 Offset;
        public UInt32 Length;
    };

    public struct TriangleSoup {
        public UInt16 material_id;

        public UInt16 draw_order;

        public UInt32 vertex_offset;
        public UInt16 vertex_length;

        public UInt16 triangle_length;
        public UInt32 triangle_offset;
    };

    public class Triangle {
        public Triangle() {
            indexes = new UInt16[3];
        }

        public UInt16[] indexes;
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
                1 - uv[ 1 ]
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
        public string Name;
        public long flags;
    }

    public class Bla : MonoBehaviour {

        Dictionary<int, string> lumpNames;

        List<Lump> Lumps;
        List<TriangleSoup> TriangleSoups;

        List<Vertex> Vertices;
        List<Triangle> Triangles;

        List<MapMaterial> Materials;

        FileStream fs;
        BinaryReader br;

        void Start() {
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

            Lumps = new List<Lump>();
            Materials = new List<MapMaterial>();
            Vertices = new List<Vertex>();
            Triangles = new List<Triangle>();
            TriangleSoups = new List<TriangleSoup>();

            using( fs = new FileStream( "Assets/Resources/mp_toujane.d3dbsp", FileMode.Open, FileAccess.Read ) ) 
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

        private void FillLumpList() {
            br.BaseStream.Seek( 8, SeekOrigin.Begin );

            for( int i = 0; i < 39; i++ ) {
                string lumpName;

                if( !lumpNames.ContainsKey( i ) )
                    lumpName = "UNKNOWN";
                else
                    lumpName = lumpNames[i];

                Lump l = new Lump();

                l.Name = lumpName;
                l.Length = br.ReadUInt32();
                l.Offset = br.ReadUInt32();

                Lumps.Insert( i, l );

                Debug.Log( "Lump[" + l.Name + "] Length: " + l.Length + " bytes | Offset: " + l.Offset + " bytes" );
            }

            CreateMeshMagic();
        }

        string GetHeaderIdentifier() {
            byte[] chunk;
            chunk = br.ReadBytes( 5 );

            StringBuilder ident = new StringBuilder();

            for( int i = 0; i < 4; i++ ) {
                ident.Append( (char) chunk[i] );
            }
            ident.Append( (int) chunk[4] );

            return ident.ToString();
        }

        private void FillMaterialList() {
            br.BaseStream.Seek( Lumps[0].Offset, SeekOrigin.Begin );

            for( int i = 0; i < Lumps[0].Length; i++ ) {
                MapMaterial m = new MapMaterial();

                m.Name = Encoding.ASCII.GetString( br.ReadBytes(64) );
                m.flags = br.ReadInt64();

                Materials.Add( m );
            }
        }

        void FillSoupsList() {
            br.BaseStream.Seek( Lumps[7].Offset, SeekOrigin.Begin );

            for( int i = 0; i < Lumps[7].Length / 16; i++ ) {
                TriangleSoup t = new TriangleSoup();

                t.material_id = br.ReadUInt16();
                t.draw_order = br.ReadUInt16();

                t.vertex_offset = br.ReadUInt32();
                t.vertex_length = br.ReadUInt16();

                t.triangle_length = br.ReadUInt16();
                t.triangle_offset = br.ReadUInt32();

                TriangleSoups.Add( t );
            }
        }

        void FillVerticesList() {
            br.BaseStream.Seek( Lumps[8].Offset, SeekOrigin.Begin );

            for( int i = 0; i < Lumps[8].Length / 68; i++ ) {
                Vertex v = new Vertex();

                v.position[0] = br.ReadSingle();
                v.position[2] = br.ReadSingle(); // switch Y and Z, diffrent engine
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

                Vertices.Add( v );
            }
        }

        void FillTrianglesList() {
            br.BaseStream.Seek( Lumps[9].Offset, SeekOrigin.Begin );

            for( int i = 0; i < Lumps[9].Length / 6; i++ ) {
                Triangle t = new Triangle();

                t.indexes[0] = br.ReadUInt16();
                t.indexes[1] = br.ReadUInt16();
                t.indexes[2] = br.ReadUInt16();

                Triangles.Add( t );
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

            for( int i = 0; i < this.TriangleSoups.Count; i++ )
            {
                TriangleSoup currentSoup = this.TriangleSoups[i];

                GameObject go = GameObject.CreatePrimitive( PrimitiveType.Cube );

                Mesh m = new Mesh();
                go.GetComponent<MeshFilter>().mesh = m;

                int tri_count = (int) currentSoup.triangle_length / 3;
                for( int j = 0; j < tri_count; j++ )
                {
                    Triangle tri = this.Triangles[(int) currentSoup.triangle_offset / 3 + j];

                    for( int vert_loop = 0; vert_loop < 3; vert_loop++ )
                    {
                        int offset = (int) tri.indexes[vert_loop];

                        Vector3 pos = this.Vertices[(int) currentSoup.vertex_offset + offset].PositionToVector3();
                        Vector2 uv = this.Vertices[ (int) currentSoup.vertex_offset + offset ].UVToVector2();

                        triangleIndices.Add( vertices.Count );
                        vertices.Add( pos );
                        uvs.Add( uv );
                    }

                    #region obsolete?
                    /*if( vertices.Count > 30000 )
                    {
                        // Get texture from folder. Get material component. Set texture to component
                        Renderer r = go.GetComponent<Renderer>();
                        string path = "Textures/images/";
                        r.material.mainTexture = Resources.Load<Texture>( path + Materials[currentSoup.material_id].Name );

                        m.vertices = vertices.ToArray();
                        m.triangles = triangleIndices.ToArray();
                        m.uv = uvs.ToArray();

                        m.RecalculateNormals();

                        vertices.Clear();
                        triangleIndices.Clear();
                        uvs.Clear();

                        go = GameObject.CreatePrimitive( PrimitiveType.Cube );
                        go.AddComponent<MeshCollider>();

                        m = new Mesh();
                        go.GetComponent<MeshFilter>().mesh = m;
                    }*/
                    
                    #endregion
                }

                if( vertices.Count > 0 )
                {
                    // Get texture from folder. Get material component. Set texture to component
                    string path = "Textures/images/";

                    Renderer r = go.GetComponent<Renderer>();
                    string matName = Materials[currentSoup.material_id].Name;

                    Texture possibleT = Resources.Load<Texture>( path + matName );

                    if( possibleT == null )
                    {
                        Debug.Log( "WTF... " + Materials[currentSoup.material_id].Name );
                    }

                    r.material.mainTexture = possibleT;
                    r.material.renderQueue = currentSoup.draw_order;

                                                                        // noDraw
                    if( ( Materials[currentSoup.material_id].flags & 0x0000000100000080 ) == 0 )
                        go.SetActive( false );

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
                    GameObject.Destroy( go );
                }

            }

            GameObject.Destroy( gameObject );
        }
    }
}