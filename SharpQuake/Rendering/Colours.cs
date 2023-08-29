/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019-2023
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
using System.Drawing;

namespace SharpQuake.Rendering
{
    public static class Colours
    {
        public static Color White = Color.White;
        public static Color Quake = Color.FromArgb( 132, 79, 59 );
        public static Color Grey = Color.FromArgb( 180, 180, 180 );

        /// <summary>
        /// Multiply two colours
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Color Multiply( this Color target, Color source )
        {
            var r = ( Byte ) ( target.R * source.R );
            var g = ( Byte ) ( target.G * source.G );
            var b = ( Byte ) ( target.B * source.B );

            return Color.FromArgb( target.A, r, g, b );
        }

        /// <summary>
        /// Small hack to swap to BGR encoding by flipping channels of a color
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Color ToBGR( this Color target )
        {
            return Color.FromArgb( target.A, target.B, target.G, target.R );
        }

        public static Color FromCode( Int32 code )
        {
            return FromCode( code, White );
        }

        public static Color FromCode( Int32 code, Color? defaultColour )
        {
            return FromCode( code, defaultColour.HasValue ? defaultColour.Value : White );
        }

        public static Color FromCode( Int32 code, Color defaultColour )
        {
            var colour = defaultColour;

            switch ( code )
            {
                case 0:
                    colour = defaultColour;
                    break;

                case 1:
                    colour = Quake;
                    break;

                case 2:
                    colour = Color.Red;
                    break;

                case 3:
                    colour = Color.Green;
                    break;

                case 4:
                    colour = Color.Yellow;
                    break;

                case 5:
                    colour = Color.Blue;
                    break;

                case 6:
                    colour = Color.Cyan;
                    break;

                case 7:
                    colour = Color.Pink;
                    break;

                case 8:
                    colour = Color.White;
                    break;

                case 9:
                    colour = Grey;
                    break;
            }

            return colour;
        }
    }
}
