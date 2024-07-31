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

using SharpQuake.Framework;
using SharpQuake.Framework.Factories;
using SharpQuake.Framework.Factories.IO;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Networking.Client;
using SharpQuake.Sys;
using System;

namespace SharpQuake.Rendering.Cameras
{
    public class Camera : BaseFactory<String, ICameraTransform>
	{
		private Vector3 _Forward; // vec3_t forward
		public Vector3 Forward
		{
			get
			{
				return _Forward;
			}
			private set
			{
				_Forward = value;
			}
		}

		private Vector3 _Right; // vec3_t right
		public Vector3 Right
		{
			get
			{
				return _Right;
			}
			private set
			{
				_Right = value;
			}
		}

		private Vector3 _Up; // vec3_t up
		public Vector3 Up
		{
			get
            {
				return _Up;
            }
			private set
            {
				_Up = value;
            }
		}


		private Single _DmgTime; // v_dmg_time
		public Single DmgTime
		{
			get
			{
				return _DmgTime;
			}
			set
			{
				_DmgTime = value;
			}
		}

		private Single _DmgRoll; // v_dmg_roll
		public Single DmgRoll
		{
			get
			{
				return _DmgRoll;
			}
			set
			{
				_DmgRoll = value;
			}
		}

		private Single _DmgPitch; // v_dmg_pitch
		public Single DmgPitch
		{
			get
			{
				return _DmgPitch;
			}
			set
			{
				_DmgPitch = value;
			}
		}

		private readonly ClientVariableFactory _cvars;
		private readonly ClientState _clientState;
		private readonly RenderState _renderState;

		public Camera( ClientVariableFactory cvars, ClientState clientState, RenderState renderState )
        {
			_cvars = cvars;
			_clientState = clientState;
			_renderState = renderState;

			ConfigureTransforms( );
		}

		private void ConfigureTransforms()
        {
			AddTransform<BobCameraTransform>( );
			AddTransform<SwayCameraTransform>( );
		}

		private void AddTransform<T>()
			where T : ICameraTransform
		{
			var type = typeof( T );
			var typeName = type.Name;
			Add( typeName, ( T ) Activator.CreateInstance( type, _clientState, _renderState ) );
		}

		public void ApplyTransform<T>( )
			where T : ICameraTransform
		{
			var type = typeof( T ).Name;
			Get( type )?.Apply( );
		}

		public T GetTransform<T>( )
			where T : ICameraTransform
		{
			var type = typeof( T ).Name;
			return ( T ) Get( type );
		}

		public  void Initialise()
        {
			InitialiseClientVariables( );
		}

		private void InitialiseClientVariables( )
        {
			if ( Cvars.ClRollSpeed != null )
				return;

			Cvars.ClRollSpeed = _cvars.Add( "cl_rollspeed", 200f );
			Cvars.ClRollAngle = _cvars.Add( "cl_rollangle", 2.0f );

			Cvars.ClBob = _cvars.Add( "cl_bob", 0.02f );
			Cvars.ClBobCycle = _cvars.Add( "cl_bobcycle", 0.6f );
			Cvars.ClBobUp = _cvars.Add( "cl_bobup", 0.5f );

		}

		// V_CalcViewRoll
		//
		// Roll is induced by movement and damage
		public void CalculateViewRoll( )
		{
			var cl = _clientState.Data;
			var rdef = _renderState.Data;
			var side = CalculateRoll( ref _clientState.ViewEntity.angles, ref cl.velocity );
			rdef.viewangles.Z += side;

			if ( _DmgTime > 0 )
			{
				rdef.viewangles.Z += _DmgTime / Cvars.KickTime.Get<Single>( ) * _DmgRoll;
				rdef.viewangles.X += _DmgTime / Cvars.KickTime.Get<Single>( ) * _DmgPitch;
				_DmgTime -= ( Single ) Time.Delta;
			}

			if ( cl.stats[QStatsDef.STAT_HEALTH] <= 0 )
			{
				rdef.viewangles.Z = 80; // dead view angle
				return;
			}
		}

		/// <summary>
		/// V_CalcRoll
		/// Used by view and sv_user
		/// </summary>
		public Single CalculateRoll( ref Vector3 angles, ref Vector3 velocity )
		{
			MathLib.AngleVectors( ref angles, out _Forward, out _Right, out _Up );
			var side = Vector3.Dot( velocity, _Right );
			Single sign = side < 0 ? -1 : 1;
			side = Math.Abs( side );

			var value = Cvars.ClRollAngle.Get<Single>( );
			if ( side < Cvars.ClRollSpeed.Get<Single>( ) )
				side = side * value / Cvars.ClRollSpeed.Get<Single>( );
			else
				side = value;

			return side * sign;
		}
	}
}
