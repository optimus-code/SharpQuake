﻿/// <copyright>
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using OpenTK.Graphics.OpenGL;
using SharpFont;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.OpenGL.Models;
using SharpQuake.Renderer.OpenGL.Textures;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Renderer.OpenGL
{
    public class GLDevice : BaseDevice
    {
        private OpenTK.DisplayDevice OpenTKDevice
        {
            get;
            set;
        }

        private OpenTK.GameWindow Form
        {
            get;
            set;
        }

        public OpenTK.Matrix4 Projection;
        public OpenTK.Matrix4 View;
        public OpenTK.Matrix4 WorldMatrix; // r_world_matrix

        public GLDevice( OpenTK.GameWindow form, OpenTK.DisplayDevice openTKDevice )
            : base( typeof( GLDeviceDesc ), 
                  typeof( GLGraphics ), 
                  typeof( GLTextureAtlas ),
                  typeof( GLModel ),
                  typeof( GLModelDesc ),
				  typeof( GLAliasModel ),
				  typeof( GLAliasModelDesc ),
				  typeof( GLTexture ), 
                  typeof( GLTextureDesc ),
                  typeof( GLModelBuffer ) )
        {
            Form = form;
            OpenTKDevice = openTKDevice;

            TextureFilters = new Dictionary<String, BaseTextureFilter>
            {
                { "GL_NEAREST", new GLTextureFilter( "GL_NEAREST", TextureMinFilter.Nearest, TextureMagFilter.Nearest ) },
                { "GL_LINEAR", new GLTextureFilter( "GL_LINEAR", TextureMinFilter.Linear, TextureMagFilter.Linear ) },
                { "GL_NEAREST_MIPMAP_NEAREST", new GLTextureFilter( "GL_NEAREST_MIPMAP_NEAREST", TextureMinFilter.NearestMipmapNearest, TextureMagFilter.Nearest ) },
                { "GL_LINEAR_MIPMAP_NEAREST", new GLTextureFilter( "GL_LINEAR_MIPMAP_NEAREST", TextureMinFilter.LinearMipmapNearest, TextureMagFilter.Linear ) },
                { "GL_NEAREST_MIPMAP_LINEAR", new GLTextureFilter( "GL_NEAREST_MIPMAP_LINEAR", TextureMinFilter.NearestMipmapLinear, TextureMagFilter.Nearest ) },
                { "GL_LINEAR_MIPMAP_LINEAR", new GLTextureFilter( "GL_LINEAR_MIPMAP_LINEAR", TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear ) }
            };

            BlendModes = new Dictionary<String, BaseTextureBlendMode>
            {
                { "GL_MODULATE", new GLTextureBlendMode( "GL_MODULATE", TextureEnvMode.Modulate ) },
                { "GL_ADD", new GLTextureBlendMode( "GL_ADD", TextureEnvMode.Add ) },
                { "GL_REPLACE", new GLTextureBlendMode( "GL_REPLACE", TextureEnvMode.Replace ) },
                { "GL_DECAL", new GLTextureBlendMode( "GL_DECAL",  TextureEnvMode.Decal ) },
                { "GL_REPLACE_EXT", new GLTextureBlendMode( "GL_REPLACE_EXT", TextureEnvMode.ReplaceExt ) },
                { "GL_TEXTURE_ENV_BIAS_SGIX", new GLTextureBlendMode( "GL_TEXTURE_ENV_BIAS_SGIX", TextureEnvMode.TextureEnvBiasSgix ) },
                { "GL_COMBINE", new GLTextureBlendMode( "GL_COMBINE", TextureEnvMode.Combine ) }
            };

            PixelFormats = new GLPixelFormat[]
            {
                new GLPixelFormat( "GL_LUMINANCE", PixelFormat.Luminance ),
                new GLPixelFormat( "GL_RGBA", PixelFormat.Rgba ),
                new GLPixelFormat( "GL_RGB", PixelFormat.Rgb ),
                new GLPixelFormat( "GL_BGR", PixelFormat.Bgr ),
                new GLPixelFormat( "GL_BGRA", PixelFormat.Bgra ),
                new GLPixelFormat( "GL_ALPHA", PixelFormat.Alpha )
            };
        }

        /// <summary>
        /// GL_Init
        /// </summary>
        public override void Initialise( Byte[] palette )
        {
            base.Initialise( palette );

            GL.ClearColor( 1, 0, 0, 0 );
            GL.CullFace( CullFaceMode.Front );
            GL.Enable( EnableCap.Texture2D );

            GL.Enable( EnableCap.AlphaTest );
            GL.AlphaFunc( AlphaFunction.Greater, 0.666f );

            GL.PolygonMode( MaterialFace.FrontAndBack, PolygonMode.Fill );
            GL.ShadeModel( ShadingModel.Flat );

            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ( Int32 ) TextureMinFilter.Nearest );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( Int32 ) TextureMagFilter.Nearest );

            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ( Int32 ) TextureWrapMode.Repeat );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ( Int32 ) TextureWrapMode.Repeat );
            GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );
        }

        public void SetTextureFilters( TextureMinFilter min, TextureMagFilter mag )
        {
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ( Int32 ) min );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( Int32 ) mag );
        }

        public override void SetTextureFilters( String name )
        {
            var filter = ( GLTextureFilter ) GetTextureFilters( name );

            if ( filter != null )
                SetTextureFilters( filter.Minimise, filter.Maximise );
        }

        public void SetBlendMode( TextureEnvMode mode )
        {
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) mode );
        }

        public override void SetBlendMode( String name )
        {
            var mode = ( GLTextureBlendMode ) GetBlendMode( name );

            if ( mode != null )
                SetBlendMode( mode.Mode );
        }

        protected override void GetAvailableModes( )
        {
            var tmp = new List<VideoMode>( OpenTKDevice.AvailableResolutions.Count );

            foreach ( var res in OpenTKDevice.AvailableResolutions )
            {
                if ( res.BitsPerPixel <= 8 )
                    continue;

                Predicate<VideoMode> SameMode = delegate ( VideoMode m )
                {
                    return ( m.Width == res.Width && m.Height == res.Height && m.BitsPerPixel == res.BitsPerPixel );
                };

                if ( tmp.Exists( SameMode ) )
                    continue;

                var mode = new VideoMode( );
                mode.Width = res.Width;
                mode.Height = res.Height;
                mode.BitsPerPixel = res.BitsPerPixel;
                mode.RefreshRate = res.RefreshRate;
                tmp.Add( mode );
            }

            AvailableModes = tmp.ToArray( );

            FirstAvailableMode = new VideoMode( );
            FirstAvailableMode.Width = OpenTKDevice.Width;
            FirstAvailableMode.Height = OpenTKDevice.Height;
            FirstAvailableMode.BitsPerPixel = OpenTKDevice.BitsPerPixel;
            FirstAvailableMode.RefreshRate = OpenTKDevice.RefreshRate;
            FirstAvailableMode.FullScreen = true;
        }

        protected override void ChangeMode( VideoMode mode )
        {
            try
            {
                OpenTKDevice.ChangeResolution( mode.Width, mode.Height,
                    mode.BitsPerPixel, mode.RefreshRate );
            }
            catch ( Exception ex )
            {
                Utilities.Error( $"Couldn't set video mode: {ex.Message}" );
            }

            if ( Desc.IsFullScreen )
            {
                Form.WindowState = OpenTK.WindowState.Fullscreen;
                Form.WindowBorder = OpenTK.WindowBorder.Hidden;
            }
            else
            {
                Form.WindowState = OpenTK.WindowState.Normal;
                Form.WindowBorder = OpenTK.WindowBorder.Fixed;
            }

            Desc.ActualWidth = Form.ClientSize.Width;
            Desc.ActualHeight = Form.ClientSize.Height;
        }

        public override void BeginScene( )
        {
            base.BeginScene( );

            GL.Color3( 1f, 1, 1 );
        }

        public override void EndScene( )
        {
            base.EndScene( );
        }

        public override void ResetMatrix( )
        {
            GL.LoadMatrix( ref WorldMatrix );
        }

        public override void PushMatrix( )
        {
            GL.PushMatrix( );
        }

        public override void PopMatrix( )
        {
            GL.PopMatrix( );
        }

        protected override void Present( )
        {
            Form?.SwapBuffers( );
        }

        public override void SetZWrite( System.Boolean enable )
        {
            GL.DepthMask( enable );
        }

        public override void SetViewport( Int32 x, Int32 y, Int32 width, Int32 height )
        {
            GL.Viewport( x, y, width, height );
        }

        public override void Begin2DScene( )
        {
            SetViewport( Desc.ViewRect );

            GL.MatrixMode( MatrixMode.Projection );
            GL.LoadIdentity( );
            GL.Ortho( 0, Desc.Width, Desc.Height, 0, -99999, 99999 );

            GL.MatrixMode( MatrixMode.Modelview );
            GL.LoadIdentity( );

            GL.Disable( EnableCap.DepthTest );
            GL.Disable( EnableCap.CullFace );
            GL.Disable( EnableCap.Blend );
            GL.Enable( EnableCap.AlphaTest );

            GL.Color4( 1.0f, 1.0f, 1.0f, 1.0f );
        }

        public override void End2DScene( )
        {

        }

        float rads = 0.0174533f;

        public override void Setup3DScene( System.Boolean cull, refdef_t renderDef, System.Boolean isEnvMap )
        {
            //GL.ClearColor( Color.Black );
            //GL.Clear( ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit );

            //
            // set up viewpoint
            //
            GL.MatrixMode( MatrixMode.Projection );
            GL.LoadIdentity( );
            var x = renderDef.vrect.x * Desc.ActualWidth / Desc.Width;
            var x2 = ( renderDef.vrect.x + renderDef.vrect.width ) * Desc.ActualWidth / Desc.Width;
            var y = ( Desc.Height - renderDef.vrect.y ) * Desc.ActualHeight / Desc.Height;
            var y2 = ( Desc.Height - ( renderDef.vrect.y + renderDef.vrect.height ) ) * Desc.ActualHeight / Desc.Height;

            // fudge around because of frac screen scale
            if ( x > 0 )
                x--;
            if ( x2 < Desc.ActualWidth )
                x2++;
            if ( y2 < 0 )
                y2--;
            if ( y < Desc.ActualHeight )
                y++;

            var w = x2 - x;
            var h = y - y2;

            if ( isEnvMap )
            {
                x = y2 = 0;
                w = h = 256;
            }

            GL.Viewport( x, y2, w, h );

            var screenaspect = ( Single ) renderDef.vrect.width / renderDef.vrect.height;

            BuildProjectionMatrix( renderDef.fov_y, screenaspect, 4, 4096 );

            GL.CullFace( CullFaceMode.Front );

            GL.MatrixMode( MatrixMode.Modelview );
            GL.LoadIdentity( );

            BuildViewMatrix( renderDef );

            WorldMatrix = View * Projection;

            // Apply our custom matrix instead of doing transforms via deprecated calls
            GL.MatrixMode( MatrixMode.Projection );
            GL.LoadMatrix( ref Projection );

            GL.MatrixMode( MatrixMode.Modelview );
            GL.LoadMatrix( ref View );

            // This is the old code which introduces additional latency as GetFloat communicates with GPU
            //GL.Rotate( -90f, 1, 0, 0 );	    // put Z going up
            //GL.Rotate( 90f, 0, 0, 1 );	    // put Z going up
            //GL.Rotate( -renderDef.viewangles.Z, 1, 0, 0 );
            //GL.Rotate( -renderDef.viewangles.X, 0, 1, 0 );
            //GL.Rotate( -renderDef.viewangles.Y, 0, 0, 1 );
            //GL.Translate( -renderDef.vieworg.X, -renderDef.vieworg.Y, -renderDef.vieworg.Z );
            //GL.GetFloat( GetPName.ModelviewMatrix, out WorldMatrix );

            //
            // set drawing parms
            //
            if ( cull )
                GL.Enable( EnableCap.CullFace );
            else
                GL.Disable( EnableCap.CullFace );

            GL.Disable( EnableCap.Blend );
            GL.Disable( EnableCap.AlphaTest );
            GL.Enable( EnableCap.DepthTest );
        }

        /// <summary>
        /// Build view matrix
        /// </summary>
        /// <remarks>
        /// (Used instead of legacy GL calls as glGetFloat adds unneeded overhead on CPU and GPU, GL transforms are deprecated in newer GL)
        /// </remarks>
        /// <param name="renderDef"></param>
        private void BuildViewMatrix( refdef_t renderDef )
        {
            View =
                OpenTK.Matrix4.CreateTranslation( new OpenTK.Vector3( -renderDef.vieworg.X, -renderDef.vieworg.Y, -renderDef.vieworg.Z ) ) *
                OpenTK.Matrix4.CreateRotationZ( MathHelper.DegreesToRadians( -renderDef.viewangles.Y ) ) *
                OpenTK.Matrix4.CreateRotationY( MathHelper.DegreesToRadians( -renderDef.viewangles.X ) ) *
                OpenTK.Matrix4.CreateRotationX( MathHelper.DegreesToRadians( -renderDef.viewangles.Z ) ) *
                OpenTK.Matrix4.CreateRotationZ( MathHelper.DegreesToRadians( 90f ) ) * // put Z going up
                OpenTK.Matrix4.CreateRotationX( MathHelper.DegreesToRadians( -90f ) ); // put Z going up
        }

        private void BuildProjectionMatrix( Single fovy, Single aspect, Single zNear, Single zFar )
        {
            // Use Matrix operations instead of the deprecated glFrustum function
            Projection = OpenTK.Matrix4.CreatePerspectiveFieldOfView( MathHelper.DegreesToRadians( fovy ), aspect, zNear, zFar );

            //var ymax = zNear * Math.Tan( fovy * Math.PI / 360.0 );
            //var ymin = -ymax;

            //var xmin = ymin * aspect;
            //var xmax = ymax * aspect;

            //GL.Frustum( xmin, xmax, ymin, ymax, zNear, zFar );
        }

        public override void Clear( System.Boolean zTrick, Single clear )
        {
            if ( zTrick )
            {
                if ( clear != 0 )
                    GL.Clear( ClearBufferMask.ColorBufferBit );

                Desc.TrickFrame++;
                if ( ( Desc.TrickFrame & 1 ) != 0 )
                {
                    Desc.DepthMinimum = 0;
                    Desc.DepthMaximum = 0.49999f;
                    GL.DepthFunc( DepthFunction.Lequal );
                }
                else
                {
                    Desc.DepthMinimum = 1;
                    Desc.DepthMaximum = 0.5f;
                    GL.DepthFunc( DepthFunction.Gequal );
                }
            }
            else
            {
                if ( clear != 0 )
                {
                    GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );
                    // Uze
                    //Host.StatusBar.Changed( );
                }
                else
                    GL.Clear( ClearBufferMask.DepthBufferBit );

                Desc.DepthMinimum = 0;
                Desc.DepthMaximum = 1;
                GL.DepthFunc( DepthFunction.Lequal );
            }

            SetDepth( Desc.DepthMinimum, Desc.DepthMaximum );
        }

        public override void SetDepth( Single minimum, Single maximum )
        {
            GL.DepthRange( minimum, maximum );
        }

        public override void SetDrawBuffer( System.Boolean isFront )
        {
            if ( isFront )
                GL.DrawBuffer( DrawBufferMode.Front );
            else
                GL.DrawBuffer( DrawBufferMode.Back );
        }

        ///<summary>
        /// Needed probably for GL only
        ///</summary>
        public override void Finish( )
        {
            GL.Finish( );
        }

        public override void SelectTexture( MTexTarget target )
        {
            if ( !Desc.SupportsMultiTexture )
                return;

            switch ( target )
            {
                case MTexTarget.TEXTURE0_SGIS:
                    GL.Arb.ActiveTexture( TextureUnit.Texture0 );
                    break;

                case MTexTarget.TEXTURE1_SGIS:
                    GL.Arb.ActiveTexture( TextureUnit.Texture1 );
                    break;

                default:
                    Utilities.Error( "GL_SelectTexture: Unknown target\n" );
                    break;
            }
        }

        public override void DisableMultitexture( )
        {
            if ( Desc.MultiTexturing )
            {
                GL.Disable( EnableCap.Texture2D );
                SelectTexture( MTexTarget.TEXTURE0_SGIS );
                Desc.MultiTexturing = false;
            }
        }

        /// <summary>
        /// GL_EnableMultitexture
        /// </summary>
        public override void EnableMultitexture( )
        {
            if ( Desc.SupportsMultiTexture )
            {
                SelectTexture( MTexTarget.TEXTURE1_SGIS );
                GL.Enable( EnableCap.Texture2D );
                Desc.MultiTexturing = true;
            }
        }

        public override void ScreenShot( out String path )
        {
            base.ScreenShot( out path );

            var fs = FileSystem.OpenWrite( path, true );

            if ( fs == null )
            {
                ConsoleWrapper.Print( "SCR_ScreenShot_f: Couldn't create a file\n" );
                return;
            }

            using ( var bmp = new Bitmap( Desc.ActualWidth, Desc.ActualHeight ) )
            {
                var data = bmp.LockBits( new Rectangle( 0, 0, Desc.ActualWidth, Desc.ActualHeight ), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb );

                GL.ReadPixels( 0, 0, Desc.ActualWidth, Desc.ActualHeight, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0 );

                bmp.UnlockBits( data );

                bmp.RotateFlip( RotateFlipType.RotateNoneFlipY );

                var encoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders( ).First( c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid );
                var encParams = new System.Drawing.Imaging.EncoderParameters( ) { Param = new[] { new System.Drawing.Imaging.EncoderParameter( System.Drawing.Imaging.Encoder.Quality, 100L ) } };

                bmp.Save( fs, encoder, encParams );
            }

            ConsoleWrapper.Print( "Wrote {0}\n", Path.GetFileName( path ) );
        }

        /// <summary>
        /// R_RotateForEntity
        /// </summary>
        public override void RotateForEntity( Vector3 origin, Vector3 angles )
        {
            GL.Translate( origin.X, origin.Y, origin.Z );

            GL.Rotate( angles.Y, 0, 0, 1 );
            GL.Rotate( -angles.X, 0, 1, 0 );
            GL.Rotate( angles.Z, 1, 0, 0 );
        }

		/// <summary>
		/// R_BlendedRotateForEntity
		/// </summary>
		public override void BlendedRotateForEntity( Vector3 origin, Vector3 angles, Double realTime, ref Vector3 origin1, ref Vector3 origin2, ref Single translateStartTime, ref Vector3 angles1, ref Vector3 angles2, ref Single rotateStartTime )
		{
			// positional interpolation

			var blend = 0f;
			var timepassed = realTime - translateStartTime;

			if ( translateStartTime == 0 || timepassed > 1 )
			{
				translateStartTime = ( Single ) realTime;

				origin1 = new Vector3( origin );
				origin2 = new Vector3( origin );
				blend = 0f;
			}
			if ( origin != origin2 )
			{
				translateStartTime = ( Single ) realTime;
				origin1 = new Vector3( origin2 );
				origin2 = new Vector3( origin );
				blend = 0;
			}
			else
			{
				blend = ( Single ) ( timepassed / 0.1f );

				if ( /*cl.paused || */blend > 1 )
					blend = 1;
			}

			var d = origin2 - origin1;

			GL.Translate( origin1.X + ( blend * d[0] ), origin1.Y + ( blend * d[1] ), origin1.Z + ( blend * d[2] ) );

			// orientation interpolation (Euler angles, yuck!)

			timepassed = realTime - rotateStartTime;

			if ( rotateStartTime == 0 || timepassed > 1 )
			{
				rotateStartTime = ( Single ) realTime;
				angles1 = new Vector3( angles );
				angles2 = new Vector3( angles );
			}

			if ( angles != angles2 )
			{
				rotateStartTime = ( Single ) realTime;
				angles1 = new Vector3( angles2 );
				angles2 = new Vector3( angles );
				blend = 0;
			}
			else
			{
				blend = ( Single ) ( timepassed / 0.1 );

				if ( /*cl.paused ||*/ blend > 1 ) blend = 1;
			}

			d = angles2 - angles1;

			// always interpolate along the shortest path
			for ( var i = 0; i < 3; i++ )
			{
				if ( d[i] > 180 )
				{
					d[i] -= 360;
				}
				else if ( d[i] < -180 )
				{
					d[i] += 360;
				}
			}

			GL.Rotate( angles1.Y + ( blend * d[1] ), 0, 0, 1 );
			GL.Rotate( -angles1.X + ( -blend * d[0] ), 0, 1, 0 );
			GL.Rotate( angles1.Z + ( blend * d[2] ), 1, 0, 0 );
		}
	}
}
