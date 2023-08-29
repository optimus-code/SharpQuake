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
using System.Collections.Generic;
using System.Drawing;
using SharpFont;
using System.IO;
using System.Reflection;
using SharpQuake.Framework;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.Textures;
using System.Drawing.Imaging;
using System.Linq;
using SharpQuake.Framework.Definitions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SharpQuake.Renderer
{
    public class TTFFont : IDisposable
    {
        public BaseDevice Device
        {
            get;
            private set;
        }

        public String Name
        {
            get;
            private set;
        }

        public BaseTexture Texture
        {
            get;
            private set;
        }

        public Int32 FontSize
        {
            get;
            private set;
        }

        private Int32 LetterSpacing
        {
            get;
            set;
        } = 0;

        private const Int32 FONT_SIZE = 29;

        Dictionary<uint, TTFCharacter> _characters = new Dictionary<uint, TTFCharacter>( );

        public TTFFont( BaseDevice device, String name, Int32 fontSize = FONT_SIZE, Int32 letterSpacing = 4 )
        {
            Device = device;
            Name = name;
            FontSize = fontSize;
            LetterSpacing = letterSpacing;
        }

        public virtual void Initialise( ByteArraySegment buffer )
        {
            // initialize library
            Library lib = new Library( );

            //Face face = new Face(lib, "FreeSans.ottf");

            //string[] names = assembly.GetManifestResourceNames();
            using ( var ms = new MemoryStream( buffer.Data ) )
            {
                Face face = new Face( lib, ms.ToArray( ), 0 );

                var fontSize = FontSize;

                face.SetPixelSizes( 0, ( uint ) fontSize );

                var cellSize = fontSize + 4;
                var textureSize = 1024;
                var gridRows = textureSize / cellSize;
                var gridColumns = gridRows;
                var bytesPerPixel = 4;
                var pixelsStride = textureSize * bytesPerPixel;
                var pixels = new Byte[( textureSize * textureSize ) * bytesPerPixel];

                //var tI = 0;
                //for ( var y = 0; y < textureSize; y++ )
                //{
                //    for ( var x = 0; x < textureSize; x++, tI += 4 )
                //    {
                //        var pie = ( y * textureSize ) + x;
                //        int i = ( ( y * textureSize ) + x ) * 4;
                //        pixels[i] = ( byte ) 255;
                //        pixels[i + 1] = ( byte ) 255;
                //        pixels[i + 2] = ( byte ) 255;
                //        pixels[i + 3] = ( byte ) 255;
                //    }

                //}
                //for ( var p = 0; p < pixels.Length; p++ )
                //{
                //    pixels[p] = 0x0;
                //}

                UInt32 character = 0;

                // Load first 128 characters of ASCII set
                for ( var y = 0; y < gridRows; y++ )
                {
                    for ( var x = 0; x < gridColumns; x++, character++ )
                    {
                        if ( character >= 128 )
                            break;

                        if ( character == 32 ) // space
                            continue;

                        try
                        {
                            face.LoadChar( character, LoadFlags.Render, LoadTarget.Normal );

                            GlyphSlot glyph = face.Glyph;

                            FTBitmap bitmap = glyph.Bitmap;
                            var absX = x * cellSize;
                            var absY = y * cellSize;
                            var glyphStride = bitmap.Width;// * bytesPerPixel;
                            var glyphBuffer = bitmap.BufferData;
                            //Byte[] gBuffer = new byte[bitmap.Width * bitmap.Rows];


                            //Marshal.Copy( bitmap.Buffer, gBuffer, 0, gBuffer.Length );
                            //System.Buffer.BlockCopy( bitmap.Buffer, 0, gBuffer, 0, gBuffer.Length );

                            var gridI = absY * textureSize + absX;

                            for ( var glyphY = 0; glyphY < bitmap.Rows; glyphY++ )
                            {
                                for ( var glyphX = 0; glyphX < bitmap.Width; glyphX++ )
                                {
                                    //var glyphI = glyphY * bitmap.Rows + glyphX;

                                    var glyphI = ( ( glyphY * bitmap.Width ) + glyphX );
                                    var pixelsI = ( ( ( absY + glyphY  ) * textureSize ) + ( absX + glyphX ) ) * 4;

                                    //var glyphI = ( glyphY * glyphStride ) + glyphX;
                                    //var pixelsI = ( ( absY + glyphY ) * pixelsStride ) + ( absX + glyphX );

                                    //var pixelsI = ( ( absY + glyphY ) * textureSize ) + ( glyphX );

                                    if ( pixelsI >= pixels.Length || glyphI >= bitmap.BufferData.Length )
                                        break;

                                    var val = glyphBuffer[glyphI];
                                    //pixels[pixelsI] = ( UInt32 ) System.Drawing.Color.FromArgb( 255, val, val, val ).ToArgb();

                                    pixels[pixelsI] = 255;
                                    pixels[pixelsI + 1] = 255;
                                    pixels[pixelsI + 2] = 255;
                                    pixels[pixelsI + 3] = val;
                                }
                            }

                            var data = new TTFCharacter( );
                            data.Width = bitmap.Width;
                            data.Height = bitmap.Rows;
                            data.X = absX;
                            data.Y = absY;
                            data.OffsetX = glyph.BitmapLeft;
                            data.OffsetY = glyph.BitmapTop;
                            data.AdvanceX = ( int ) glyph.Advance.X.Value;
                            _characters.Add( character, data );
                        }
                        catch ( Exception ex )
                        {
                            //Utilities.Error( ex.ToString( ) );
                        }
                    }
                }
                //for ( uint c = 0; c < 128; c++ )
                //{
                //    try
                //    {
                //        // load glyph
                //        //face.LoadGlyph(c, LoadFlags.Render, LoadTarget.Normal);
                //        face.LoadChar( c, LoadFlags.Render, LoadTarget.Normal );
                //        GlyphSlot glyph = face.Glyph;
                //        FTBitmap bitmap = glyph.Bitmap;

                //        // create glyph texture
                //        int texObj = GL.GenTexture( );
                //        GL.BindTexture( TextureTarget.Texture2D, texObj );
                //        GL.TexImage2D( TextureTarget.Texture2D, 0,
                //                      PixelInternalFormat.R8, bitmap.Width, bitmap.Rows, 0,
                //                      PixelFormat.Red, PixelType.UnsignedByte, bitmap.Buffer );


                //        // add character
                //        var data = new TTFCharacter( );
                //        data.Width = bitmap.Width;
                //        data.Height = bitmap.Rows;
                //        data.OffsetX = glyph.BitmapLeft;
                //        data.OffsetY = glyph.BitmapTop;
                //        data.AdvanceX = ( int ) glyph.Advance.X.Value;
                //        _characters.Add( c, data );
                //    }
                //    catch ( Exception ex )
                //    {
                //        Console.WriteLine( ex );
                //    }
                //}

                //Texture = BaseTexture.FromBuffer( Device, Name, pixels, textureSize, textureSize, false, true );

                //var data = new Byte[texture * 4];
                //var i = 0;

                //for ( var x = 0; x < 8; x++ )
                //{
                //    for ( var y = 0; y < 8; y++, i += 4 )
                //    {
                //        data[i] = 255;
                //        data[i + 1] = 255;
                //        data[i + 2] = 255;
                //        data[i + 3] = ( Byte ) ( ParticleDef._DotTexture[x, y] * 255 );
                //    }
                //}

                var uintData = new UInt32[pixels.Length / 4];
                Buffer.BlockCopy( pixels, 0, uintData, 0, pixels.Length );

                Texture = BaseTexture.FromBuffer( Device, Name + "_Tex", uintData, textureSize, textureSize, false, true, "GL_LINEAR", preservePixelBuffer: true );
                //Texture = BaseTexture.FromBuffer( Device, Name, buffer, 128, 128, false, true, filter: "GL_NEAREST" );

                //PixelFormat formatOutput = PixelFormat.Format32bppArgb;
                //Rectangle rect = new Rectangle( 0, 0, textureSize, textureSize );
                //Bitmap bmp = new Bitmap( textureSize, textureSize, formatOutput );
                //BitmapData bmpData = bmp.LockBits( rect, ImageLockMode.ReadOnly, formatOutput );
                //Marshal.Copy( pixels, 0, bmpData.Scan0, pixels.Length );
                //bmp.UnlockBits( bmpData );
                //bmp.Save( @"H:\Source\Repos\SharpQuake\SharpQuake\bin\Debug\net48\output.png" );
              
            }
        }

        public Int32 CharacterAdvance( )
        {
            return LetterSpacing;
        }

        public virtual Int32 Measure( UInt32 character )
        {
            var c = character;

            if ( c > 128 && c - 128 > 32 )
                c -= 128;

            if ( c == 32 )
                return Measure( 'e' );

            if ( c < 0 || c > 128 || !_characters.ContainsKey( character ) )
                return Measure( 'e' );

            var data = _characters[c];
            return data.Width;
        }

        public virtual Int32 MeasureHeight( UInt32 character )
        {
            var c = character;

            if ( c > 128 && c - 128 > 32 )
                c -= 128;

            if ( c == 32 )
                return MeasureHeight( 'T' );

            if ( c < 0 || c > 128 || !_characters.ContainsKey( character ) )
                return MeasureHeight( 'T' );

            var data = _characters[c];
            return data.Height;
        }

        public Int32 CharacterAdvanceHeight( )
        {
            var height = MeasureHeight( 'T' );

            return 16 + height;
        }

        public virtual Int32 Measure( String str )
        {
            var width = 0;
            for ( var i = 0; i < str.Length; i++ )
            {
                var c = str[i];
                width += Measure( c );
            }

            return width;
        }

        // Draw_String
        public virtual void Draw( Int32 x, Int32 y, String str, Color? color = null )
        {
            var xAdvance = x;

            for ( var i = 0; i < str.Length; i++ )
            {
                DrawCharacter( xAdvance, y, str[i], color );
                xAdvance += CharacterAdvance() + Measure( str[i] );
            }
        }

        // Draw_Character
        //
        // Draws one 8*8 graphics character with 0 being transparent.
        // It can be clipped to the top of the screen to allow the console to be
        // smoothly scrolled off.
        // Vertex color modification has no effect currently
        public virtual void DrawCharacter( Int32 x, Int32 y, Int32 num, Color? colour = null )
        {
            if ( num == 32 )
                return;		// space

            //num &= 255;

            if ( y <= -8 )
                return;			// totally off screen

            var row = num >> 4;
            var col = num & 15;

            //var frow = row * 0.0625f;
            //var fcol = col * 0.0625f;
            //var size = 0.0625f;

            var nnum = num;

            if ( nnum > 128 && nnum - 128 > 32 )
                nnum -= 128;
            //if ( nnum > 128 )
            //{
            //    return;
            //}
            // TODO - CHANGE, this is a hack to support the multiple text modes the game uses
            //if ( nnum > 128 && nnum - 128 >= 0 )
            //{
            //    nnum = num - 128;
            //}

            if ( nnum >= 128 ) // We currently dont do anything higher!
                return;

            if ( nnum == 32 || !_characters.ContainsKey( ( UInt32 ) nnum ) )
                return;		// space

            var data = _characters[( UInt32 ) nnum];
            var fcol = ( data.X ) / ( float ) Texture.Desc.Width;
            var frow = ( data.Y ) / ( float ) Texture.Desc.Height;
            var sizeX = ( data.Width / ( float ) Texture.Desc.Width );
            var sizeY = ( data.Height / ( float ) Texture.Desc.Height );


           // var yPos = y + ( ( _characters[( uint ) 'T'].OffsetY / 4 ) - ( data.OffsetY / 4 ) ) * data.Height;

            Device.Graphics.DrawTexture2D( Texture,
                   new RectangleF( fcol, frow, sizeX, sizeY ), new Rectangle( x + ( data.OffsetX ), y - ( data.OffsetY ) + FontSize, data.Width, data.Height ), colour );

            //Device.Graphics.DrawTexture2D( Texture, 0, 0, hasAlpha: true );
        }

        public virtual (Int32 X, Int32 Y) GetCharacterOffset( Int32 num )
        {
            var row = num >> 4;
            var col = num & 15;

            var nnum = num;

            if ( nnum >= 128 ) // We currently dont do anything higher!
                return (0, 0);

            if ( nnum == 32 || !_characters.ContainsKey( ( UInt32 ) nnum ) )
                return (0, 0);

            var data = _characters[( UInt32 ) nnum];

            return (data.OffsetX, data.OffsetY);
        }

        public virtual UInt32[] GetCharacterBuffer( Int32 num )
        {
            if ( num == 32 )
                return null;		// space

            var nnum = num;

            if ( nnum > 128 && nnum - 128 > 32 )
                nnum -= 128;

            if ( nnum >= 128 ) // We currently dont do anything higher!
                return null;

            if ( nnum == 32 || !_characters.ContainsKey( ( UInt32 ) nnum ) )
                return null;		// space

            var data = _characters[( UInt32 ) nnum];
            var buffer = Texture.Buffer32;
            var result = new UInt32[data.Width * data.Height];

            for ( var y = data.Y; y < data.Y + data.Height; y++ )
            {
                for ( var x = data.X; x < data.X + data.Width; x++ )
                {
                    var sourceIndex = y * Texture.Desc.Width + x;
                    var destIndex = ( y - data.Y ) * data.Width + ( x - data.X );
                    result[destIndex] = buffer[sourceIndex];
                }
            }

            return result;
        }

        public virtual void Dispose( )
        {
        }
    }

    public struct TTFCharacter
    {
        public int X 
        { 
            get; 
            set;
        }

        public int Y
        {
            get;
            set;
        }

        public int OffsetX
        {
            get;
            set;
        }

        public int OffsetY
        {
            get;
            set;
        }

        public int Width
        {
            get;
            set;
        }

        public int Height
        {
            get;
            set;
        }

        public int AdvanceX
        { 
            get;
            set;
        }
    }
}
