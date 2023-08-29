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

namespace SharpQuake.Renderer.Textures
{
    public class BaseTextureDesc
    {
        public virtual String Name
        {
            get;
            set;
        }

        public virtual String Owner
        {
            get;
            set;
        }

        public virtual String Filter
        {
            get;
            set;
        }

        public virtual String BlendMode
        {
            get;
            set;
        }

        public virtual Int32 Width
        {
            get;
            set;
        }

        public virtual Int32 Height
        {
            get;
            set;
        }

        public virtual Int32 ScaledWidth
        {
            get;
            set;
        }

        public virtual Int32 ScaledHeight
        {
            get;
            set;
        }

        public virtual Boolean HasMipMap
        {
            get;
            set;
        }

        public virtual Boolean HasAlpha
        {
            get;
            set;
        }

        public virtual Boolean IsLightMap
        {
            get;
            set;
        }

        public virtual String LightMapFormat
        {
            get;
            set;
        }

        public virtual Int32 LightMapBytes
        {
            get;
            set;
        }

        public virtual Boolean PreservePixelBuffer
        {
            get;
            set;
        }
    }
}
