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
using System.Runtime.InteropServices;

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public class ProgramDefinition
    {
        public UInt16 type;		// if DEF_SAVEGLOBGAL bit is set

        // the variable needs to be saved in savegames
        public UInt16 ofs;

        public string_t s_name;

        public static string_t SizeInBytes = Marshal.SizeOf( typeof( ProgramDefinition ) );

        public void SwapBytes( )
        {
            type = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) type );
            ofs = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) ofs );
            s_name = EndianHelper.LittleLong( s_name );
        }
    } // ddef_t;
}
