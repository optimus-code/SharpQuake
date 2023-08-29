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
using SharpQuake.Framework;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Renderer.Models
{
    public class BaseModelBuffer : IDisposable
    {
        public BufferVertex[] Vertices
        {
            get;
            protected set;
        }

        public UInt32[] Indices
        {
            get;
            protected set;
        }

        protected readonly BaseDevice _device;

        public BaseModelBuffer( BaseDevice device, BufferVertex[] vertices, UInt32[] indices )
        {
            _device = device;
            Vertices = vertices;
            Indices = indices;
        }

        public virtual void Begin( )
        {
        }


        public virtual void End( )
        {
        }

        public virtual void DrawPoly( GLPoly poly )
        {
        }

        public virtual void Draw( )
        {
        }

        public virtual void BeginTexture( BaseTexture texture )
        {
        }

        public virtual void Dispose( )
        {
            Vertices = null;
            Indices = null;
        }

        public static BaseModelBuffer New( BaseDevice device, BufferVertex[] vertices, UInt32[] indices )
        {
            return ( BaseModelBuffer ) Activator.CreateInstance( device.ModelBufferType, device, vertices, indices );
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BufferVertex
    {
        public Vector3 Position;
        public Vector2 UV;
    }
}
