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
using System.IO;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework
{
    // This mirrors trivert_t in trilib.h, is present so Quake knows how to
    // load this data

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct trivertx_t
    {
        //[MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        //public Byte[] v; // [3];
        public Byte X;
        public Byte Y;
        public Byte Z;
        public Byte lightnormalindex;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( trivertx_t ) );

        /// <summary>
        /// Call only for manually created instances
        /// </summary>
        public void Init( )
        {
            //if ( v == null )
            //    v = new Byte[3];
        }

        public static trivertx_t FromBR( BinaryReader br )
        {
            var vert = new trivertx_t( );
            vert.X = br.ReadByte( );
            vert.Y = br.ReadByte( );
            vert.Z = br.ReadByte( );
            vert.lightnormalindex = br.ReadByte( );
            return vert;
        }
    } // trivertx_t;
}
