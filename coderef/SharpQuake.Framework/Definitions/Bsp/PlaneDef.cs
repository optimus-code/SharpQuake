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


namespace SharpQuake.Framework
{
    public static class PlaneDef
    {
        // 0-2 are axial planes
        public const System.Int32 PLANE_X = 0;

        public const System.Int32 PLANE_Y = 1;
        public const System.Int32 PLANE_Z = 2;

        // 3-5 are non-axial planes snapped to the nearest
        public const System.Int32 PLANE_ANYX = 3;

        public const System.Int32 PLANE_ANYY = 4;
        public const System.Int32 PLANE_ANYZ = 5;
    }
}
