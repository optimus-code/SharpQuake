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
using OpenTK.Platform.MacOS;
using SharpQuake.Framework;
using SharpQuake.Framework.IO.BSP;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.Models;
using SharpQuake.Renderer.OpenGL.Textures;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Renderer.OpenGL.Models
{
    public class GLModelBuffer : BaseModelBuffer
    {
        private Int32 VertexArrayID
        {
            get;
            set;
        }

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

        private Shader Shader
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
            var vertex2 = @"#version 110

void main()
{
    gl_TexCoord[0] = gl_TextureMatrix[0] * gl_MultiTexCoord0;
    gl_TexCoord[1] = gl_TextureMatrix[1] * gl_MultiTexCoord1;
    gl_Position = gl_ProjectionMatrix * gl_ModelViewMatrix * gl_Vertex;
}";

            var frag2 = @"#version 110
uniform sampler2D tex;
uniform sampler2D lm;
void main()
{
// Fetch the base texture color
    vec4 color = texture2D(tex, gl_TexCoord[0].st);

    // Fetch the lightmap color (typically grayscale, so we use the red channel)
    vec4 lmc = texture2D(lm, gl_TexCoord[1].st);

    // Scale the lightmap intensity to match Quake's lighting model
    float lightScale = 2.0; // Adjust based on desired brightness

    // Combine the base texture with the scaled lightmap
    vec3 finalColor = color.rgb * (lmc.r * lightScale);

    // Ensure the final color is within the valid range [0, 1]
    finalColor = clamp(finalColor, 0.0, 1.0);

    gl_FragColor = vec4(finalColor, color.a);
}";

            var vertex = @"#version 460 core
layout (location = 0) in vec3 VertPosition;
layout (location = 1) in vec2 VertTexCoord;
layout (location = 2) in vec2 VertLightmap;

layout (location = 0) uniform mat4 ModelViewProjection;

out vec2 UV;
out vec2 LightmapUV;

void main()
{
    gl_Position = ModelViewProjection * vec4(VertPosition, 1.0);
    //gl_Position = gl_ProjectionMatrix * gl_ModelViewMatrix * gl_Vertex;
    UV = VertTexCoord;
    LightmapUV = VertLightmap;
}";

            var frag = @"#version 460 core
out vec4 FragColor;

in vec2 UV;
in vec2 LightmapUV;

uniform sampler2D Texture0;
uniform sampler2D Texture1;

void main()
{
    vec4 BaseColor = texture(Texture0, UV);
    vec4 LightColor = vec4(texture(Texture1, LightmapUV).rrr, 1.0);
    FragColor = (BaseColor * 2.0) * LightColor;
}";
            Shader = new Shader( vertex2, frag2 );

            GL.EnableClientState( ArrayCap.VertexArray );
            GL.EnableClientState( ArrayCap.IndexArray );

            //VertexArrayID = GL.GenVertexArray( );

            //GL.BindVertexArray( VertexArrayID );

            VertexBufferID = GL.GenBuffer( );

            GL.BindBuffer( BufferTarget.ArrayBuffer, VertexBufferID );
            GL.BufferData( BufferTarget.ArrayBuffer, Vertices.Length * Marshal.SizeOf<BufferVertex>(), Vertices, BufferUsageHint.StaticDraw );
            //GL.EnableVertexAttribArray( 0 );
            //GL.VertexAttribPointer( 0, 3, VertexAttribPointerType.Float, false, 3 * sizeof( Single ), 0 );

            IndexBufferID = GL.GenBuffer( );
            GL.BindBuffer( BufferTarget.ElementArrayBuffer, IndexBufferID );
            GL.BufferData( BufferTarget.ElementArrayBuffer, Indices.Length * sizeof( UInt32 ), Indices, BufferUsageHint.StaticDraw );


            GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
            //GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );

            GL.DisableClientState( ArrayCap.VertexArray );
            GL.DisableClientState( ArrayCap.IndexArray );

            //GL.BindVertexArray( 0 );
        }

        public override void Begin( )
        {
            GL.Enable( EnableCap.Texture2D );
            GL.EnableClientState( ArrayCap.VertexArray );

            GL.BindBuffer( BufferTarget.ArrayBuffer, VertexBufferID );
            GL.VertexPointer( 3, VertexPointerType.Float, Marshal.SizeOf<BufferVertex>( ), 0 );

            GL.ClientActiveTexture( TextureUnit.Texture0 );
            GL.EnableClientState( ArrayCap.TextureCoordArray );
            GL.TexCoordPointer( 2, TexCoordPointerType.Float, Marshal.SizeOf<BufferVertex>( ), Marshal.SizeOf<Single>( ) * 3 );

            GL.ClientActiveTexture( TextureUnit.Texture1 );
            GL.EnableClientState( ArrayCap.TextureCoordArray );
            GL.TexCoordPointer( 2, TexCoordPointerType.Float, Marshal.SizeOf<BufferVertex>( ), Marshal.SizeOf<Single>( )  * 5 );

            GL.BindBuffer( BufferTarget.ElementArrayBuffer, IndexBufferID );

            //GL.UseProgram( 0 );
            //GL.Uniform1( Uniform.Location, TextureSlot );

            Shader.Use( );

            //GL.BindVertexArray( VertexArrayID );
        }


        public override void End( )
        {
           // GL.BindVertexArray( 0 );
            GL.DisableClientState( ArrayCap.VertexArray );
            GL.DisableClientState( ArrayCap.TextureCoordArray );
            GL.Disable( EnableCap.Texture2D );
            GL.BindTexture( TextureTarget.Texture2D, 0 );
            GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );

            GL.ActiveTexture( TextureUnit.Texture0 );
            GL.UseProgram( 0 );

            //GL.ClientActiveTexture( TextureUnit.Texture0 );
        }

        private BaseTexture ActiveTex;

        public override void BeginTexture( BaseTexture texture )
        {
            ActiveTex = texture;
        }

        public override void DrawPoly( GLPoly poly, BaseTexture lightmapTexture, Byte[] lightmapData )
        {

            if ( ActiveTex == null )
                return;

            GL.ActiveTexture( TextureUnit.Texture0 );
            ActiveTex?.Bind( );
            Shader.SetInt( "tex", 0 );


            GL.ActiveTexture( TextureUnit.Texture1 );
            lightmapTexture.Bind( );

            Shader.SetInt( "lm", 1 );


            //Shader.SetInt( "Texture0", ( ( GLTexture ) texture ).GLDesc.TextureNumber );

            //Shader.SetMatrix4( "ModelViewProjection", _glDevice.WorldMatrix );

            //GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );

            //GL.DrawArrays( PrimitiveType.Polygon, poly.FirstVertex, poly.numverts );

            //GL.Enable( EnableCap.Blend );


            //GL.ActiveTexture( TextureUnit.Texture2 );

            //_device.Graphics.BeginBlendLightMap( true );



            //Shader.SetInt( "Texture1", 2 );
           // var lmNum = ( ( GLTextureDesc ) lightmapTexture.Desc ).TextureNumber + poly.LightMapTextureNum;
            //Shader.SetInt( "Texture1", lmNum );
           //lightmapTexture.BindLightmap( lmNum );


            var i = poly.LightMapTextureNum;

           // if ( lightmapTexture.LightMapModified[i] )
            //    lightmapTexture.CommitLightmap( lightmapData, i );



            //GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Blend );

            GL.DrawArrays( PrimitiveType.Polygon, poly.FirstVertex, poly.numverts );

            //_device.Graphics.EndBlendLightMap( true );
            //GL.DrawArrays( PrimitiveType.TriangleFan, poly.FirstIndex, poly.numverts );

            //GL.DrawElementsBaseVertex( PrimitiveType.Triangles, poly.numverts, DrawElementsType.UnsignedInt, ( IntPtr ) ( poly.FirstIndex * sizeof( UInt32 ) ), poly.FirstVertex);
            GL.ActiveTexture( TextureUnit.Texture0 );

            //GL.DrawArrays( PrimitiveType.Triangles, poly.FirstVertex, poly.numverts );
            //GL.Draw( PrimitiveType.TriangleFan, poly.NumFaces, DrawElementsType.UnsignedInt, poly.FirstIndex, poly.FirstVertex );
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

           // GL.DrawArrays( PrimitiveType.Polygon, 0, Vertices.Length );

            //GL.DrawElements( PrimitiveType.Triangles, Indices.Length / 3, DrawElementsType.UnsignedInt, Indices );
            //GL.DrawElements( PrimitiveType.Triangles, Indices.Length / 3, DrawElementsType.UnsignedInt, Vertices.Length );
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
