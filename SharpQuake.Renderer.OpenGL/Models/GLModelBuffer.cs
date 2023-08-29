/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.Models;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Renderer.OpenGL.Models
{
    public class GLModelBuffer : BaseModelBuffer
    {
        private Int32 VertexBufferID
        {
            get;
            set;
        }

        private Int32 IndexBufferID
        {
            get;
            set;
        }

        private readonly GLDevice _glDevice;

        public GLModelBuffer( BaseDevice device, BufferVertex[] vertices, UInt32[] indices ) 
            : base( device, vertices, indices )
        {
            _glDevice = ( GLDevice ) device;
            Initialise( );
        }

        private void Initialise()
        {
            GL.EnableClientState( ArrayCap.VertexArray );
            GL.EnableClientState( ArrayCap.IndexArray );

            VertexBufferID = GL.GenBuffer( );

            GL.BindBuffer( BufferTarget.ArrayBuffer, VertexBufferID );
            GL.BufferData( BufferTarget.ArrayBuffer, Vertices.Length * Marshal.SizeOf<BufferVertex>(), Vertices, BufferUsageHint.StaticDraw );
            //GL.EnableVertexAttribArray( 0 );
            //GL.VertexAttribPointer( 0, 3, VertexAttribPointerType.Float, false, 3 * sizeof( Single ), 0 );

            IndexBufferID = GL.GenBuffer( );
            GL.BindBuffer( BufferTarget.ElementArrayBuffer, IndexBufferID );
            GL.BufferData( BufferTarget.ElementArrayBuffer, Indices.Length * sizeof( UInt32 ), Indices, BufferUsageHint.StaticDraw );


            GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
            GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );

            GL.DisableClientState( ArrayCap.VertexArray );
            GL.DisableClientState( ArrayCap.IndexArray );
        }

        public override void Begin( )
        {
            GL.Enable( EnableCap.Texture2D );
            GL.EnableClientState( ArrayCap.VertexArray );
            GL.EnableClientState( ArrayCap.TextureCoordArray );

            GL.BindBuffer( BufferTarget.ArrayBuffer, VertexBufferID );
            GL.VertexPointer( 3, VertexPointerType.Float, Marshal.SizeOf<BufferVertex>( ), 0 );
            GL.TexCoordPointer( 2, TexCoordPointerType.Float, Marshal.SizeOf<BufferVertex>( ), Marshal.SizeOf<Vector3>( ) );

        }


        public override void End( )
        {
            GL.DisableClientState( ArrayCap.VertexArray );
            GL.DisableClientState( ArrayCap.TextureCoordArray );
            GL.Disable( EnableCap.Texture2D );
            GL.BindTexture( TextureTarget.Texture2D, 0 );
            GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
        }

        public override void BeginTexture( BaseTexture texture )
        {
            texture?.Bind( );
        }

        public override void DrawPoly( GLPoly poly )
        {
            GL.DrawArrays( PrimitiveType.Triangles, poly.FirstVertexIndex, poly.numverts );
            //GL.DrawArrays( PrimitiveType.Polygon, poly.FirstVertexIndex, poly.numverts / 3 );
            //if ( poly.numverts == 3 )
            //    GL.DrawArrays( PrimitiveType.Polygon, poly.FirstVertexIndex, 1 );
            //else if ( poly.numverts == 4 )
            //    GL.DrawArrays( PrimitiveType.Polygon, poly.FirstVertexIndex, 2 );
            //else
            //    GL.DrawArrays( PrimitiveType.Polygon, poly.FirstVertexIndex, poly.numverts / 3 );
        }

        public override void Draw( )
        {

            GL.DrawArrays( PrimitiveType.TriangleStrip, 0, Vertices.Length );


            //GL.DrawArrays( PrimitiveType.Polygon, 0, Vertices.Length / 4 );

            //GL.BindBuffer( BufferTarget.ArrayBuffer, VertexBufferID );
            //GL.VertexPointer( 3, VertexPointerType.Float, 0, 0 );


            //GL.BindVertexArray( VertexBufferID );
            ///GL.BindBuffer( BufferTarget.ElementArrayBuffer, IndexBufferID );




            ///GL.EnableClientState( ArrayCap.VertexArray );

            //glBindBuffer( GL_ARRAY_BUFFER, BufferName[COLOR_OBJECT] );
            //glBufferData( GL_ARRAY_BUFFER, ColorSize, ColorData, GL_STREAM_DRAW );
            //glColorPointer( 3, GL_UNSIGNED_BYTE, 0, 0 );

            //glBindBuffer( GL_ARRAY_BUFFER, BufferName[POSITION_OBJECT] );
            //glBufferData( GL_ARRAY_BUFFER, PositionSize, PositionData, GL_STREAM_DRAW );
            //glVertexPointer( 2, GL_FLOAT, 0, 0 );

            //glEnableClientState( GL_VERTEX_ARRAY );
            //glEnableClientState( GL_COLOR_ARRAY );




            //GL.DrawElements( PrimitiveType.Quads, Indices.Length / 3, DrawElementsType.UnsignedInt, Indices.Length );

            //GL.BindVertexArray( 0 );
            //GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );




            //glDrawArrays( GL_TRIANGLES, 0, VertexCount );

            //GL.DisableClientState( ArrayCap.VertexArray );

            //glDisableClientState( GL_COLOR_ARRAY );
            //glDisableClientState( GL_VERTEX_ARRAY );
        }

        public override void Dispose( )
        {
            base.Dispose( );

            GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
            GL.DeleteBuffer( VertexBufferID );

            GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );
            GL.DeleteBuffer( IndexBufferID );
        }
    }
}
