
using SharpQuake.Framework.IO.Sound;
using SharpQuake.Framework.Logging;
using SharpQuake.Logging;
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
namespace SharpQuake
{
    internal class NullSoundController : ISoundController
    {
        #region ISoundController Members

        public System.Boolean IsInitialised
        {
            get
            {
                return false;
            }
        }

        private readonly snd _sound;

        public NullSoundController( snd sound )
        {
            _sound = sound;
        }

        public void Initialise( )
        {
            _sound.shm.channels = 2;
            _sound.shm.samplebits = 16;
            _sound.shm.speed = 11025;
        }

        public void Shutdown()
        {
        }

        public void ClearBuffer()
        {
        }

        public System.Byte[] LockBuffer()
        {
            return _sound.shm.buffer;
        }

        public void UnlockBuffer( System.Int32 bytes )
        {
        }

        public System.Int32 GetPosition()
        {
            return 0;
        }

        #endregion ISoundController Members
    }
}
