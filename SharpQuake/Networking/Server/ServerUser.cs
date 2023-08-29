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

using SharpQuake.Desktop;
using SharpQuake.Factories.Rendering;
using SharpQuake.Framework;
using SharpQuake.Framework.Factories.IO;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Framework.Logging;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Networking;
using SharpQuake.Networking.Server;
using SharpQuake.Sys;
using SharpQuake.Sys.Programs;
using System;
using System.Linq;

namespace SharpQuake
{
    public class ServerUser
    {
        private const Int32 MAX_FORWARD = 6;

        private Boolean _OnGround; // onground

        // world
        //static v3f angles - this must be a reference to _state.Player.v.angles
        //static v3f origin  - this must be a reference to _state.Player.v.origin
        //static Vector3 velocity - this must be a reference to _state.Player.v.velocity

        private usercmd_t _Cmd; // cmd

        private Vector3 _Forward; // forward
        private Vector3 _Right; // right
        private Vector3 _Up; // up

        private Vector3 _WishDir; // wishdir
        private Single _WishSpeed; // wishspeed

        private String[] ClientMessageCommands = new String[]
        {
            "status",
            "god",
            "notarget",
            "fly",
            "name",
            "noclip",
            "say",
            "say_team",
            "tell",
            "color",
            "kill",
            "pause",
            "spawn",
            "begin",
            "prespawn",
            "kick",
            "ping",
            "give",
            "ban"
        };

        private readonly IConsoleLogger _logger;
        private readonly ICache _cache;
        private readonly IKeyboardInput _keyboard;
        private readonly ClientVariableFactory _cvars;
        private readonly CommandFactory _commands;
        private readonly ModelFactory _models;
        private readonly snd _sound;
        private readonly Network _network;
        private readonly ServerState _state;
        private readonly ProgramsState _programsState;
        private readonly ProgramsExec _programsExec;
        private readonly ProgramsEdict _programsEdict;
        private readonly ServerWorld _world;
        private readonly ServerPhysics _physics;
        private readonly LocalHost _localHost;

        public ServerUser( IConsoleLogger logger, ClientVariableFactory cvars, CommandFactory commands,
            ModelFactory models, ICache cache, IKeyboardInput keyboard, snd sound, Network network, ServerState state,
            ProgramsState programsState, ProgramsExec programsExec, ProgramsEdict programsEdict,
            ServerWorld world, ServerPhysics physics, LocalHost localHost )
        {
            _logger = logger;
            _cache = cache;
            _keyboard = keyboard;
            _cvars = cvars;
            _commands = commands;
            _models = models;
            _sound = sound;
            _network = network;
            _state = state;
            _programsState = programsState;
            _programsExec = programsExec;
            _programsEdict = programsEdict;
            _world = world;
            _physics = physics;
            _localHost = localHost;
        }

        /// <summary>
        /// SV_RunClients
        /// </summary>
        public void RunClients( Action<client_t> onDropClient )
        {
            _localHost.HostClient = _state.StaticData.clients[0]; // Set local host

            for ( var i = 0; i < _state.StaticData.maxclients; i++ )
            {
                var client = _state.StaticData.clients[i];

                if ( !client.active )
                    continue;

                _state.Player = client.edict;

                if ( !ReadClientMessage( client ) )
                {
                    onDropClient?.Invoke( client );	// client misbehaved...
                    continue;
                }

                if ( !client.spawned )
                {
                    // clear client movement until a new packet is received
                    client.cmd.Clear( );
                    continue;
                }

                // always pause in single player if in console or menus
                if ( !_state.Data.paused && ( _state.StaticData.maxclients > 1 || _keyboard.Destination == KeyDestination.key_game ) )
                    ClientThink( client );
            }
        }

        /// <summary>
        /// SV_SetIdealPitch
        /// </summary>
        public void SetIdealPitch( )
        {
            if ( ( ( Int32 ) _state.Player.v.flags & EdictFlags.FL_ONGROUND ) == 0 )
                return;

            var angleval = _state.Player.v.angles.y * Math.PI * 2 / 360;
            var sinval = Math.Sin( angleval );
            var cosval = Math.Cos( angleval );
            var z = new Single[MAX_FORWARD];
            for ( var i = 0; i < MAX_FORWARD; i++ )
            {
                var top = _state.Player.v.origin;
                top.x += ( Single ) ( cosval * ( i + 3 ) * 12 );
                top.y += ( Single ) ( sinval * ( i + 3 ) * 12 );
                top.z += _state.Player.v.view_ofs.z;

                var bottom = top;
                bottom.z -= 160;

                var tr = _physics.Move( ref top, ref Utilities.ZeroVector3f, ref Utilities.ZeroVector3f, ref bottom, 1, _state.Player );
                if ( tr.allsolid )
                    return;	// looking at a wall, leave ideal the way is was

                if ( tr.fraction == 1 )
                    return;	// near a dropoff

                z[i] = top.z + tr.fraction * ( bottom.z - top.z );
            }

            Single dir = 0; // Uze: int in original code???
            var steps = 0;
            for ( var j = 1; j < MAX_FORWARD; j++ )
            {
                var step = z[j] - z[j - 1]; // Uze: int in original code???
                if ( step > -QDef.ON_EPSILON && step < QDef.ON_EPSILON ) // Uze: comparing int with ON_EPSILON (0.1)???
                    continue;

                if ( dir != 0 && ( step - dir > QDef.ON_EPSILON || step - dir < -QDef.ON_EPSILON ) )
                    return;		// mixed changes

                steps++;
                dir = step;
            }

            if ( dir == 0 )
            {
                _state.Player.v.idealpitch = 0;
                return;
            }

            if ( steps < 2 )
                return;
            _state.Player.v.idealpitch = -dir * Cvars.IdealPitchScale.Get<Single>( );
        }

        private Int32 GetClientMessageCommand( client_t client, String s )
        {
            Int32 ret;

            if ( client.privileged )
                ret = 2;
            else
                ret = 0;

            var cmdName = s.Split( ' ' )[0];

            if ( ClientMessageCommands.Contains( cmdName ) )
                ret = 1;

            return ret;
        }

        /// <summary>
        /// SV_ReadClientMessage
        /// Returns false if the client should be killed
        /// </summary>
        private Boolean ReadClientMessage( client_t client )
        {
            while ( true )
            {
                var ret = _network.GetMessage( client.netconnection );
                if ( ret == -1 )
                {
                    _logger.DPrint( "SV_ReadClientMessage: NET_GetMessage failed\n" );
                    return false;
                }
                if ( ret == 0 )
                    return true;

                _network.Reader.Reset( );

                var flag = true;
                while ( flag )
                {
                    if ( !client.active )
                        return false;	// a command caused an error

                    if ( _network.Reader.IsBadRead )
                    {
                        _logger.DPrint( "SV_ReadClientMessage: badread\n" );
                        return false;
                    }

                    var cmd = _network.Reader.ReadChar( );
                    switch ( cmd )
                    {
                        case -1:
                            flag = false; // end of message
                            ret = 1;
                            break;

                        case ProtocolDef.clc_nop:
                            break;

                        case ProtocolDef.clc_stringcmd:
                            var s = _network.Reader.ReadString( );
                            ret = GetClientMessageCommand( client, s );
                            if ( ret == 2 )
                                _commands.Buffer.Insert( s );
                            else if ( ret == 1 )
                                _commands.ExecuteString( s, CommandSource.Client, client );
                            else
                                _logger.DPrint( "{0} tried to {1}\n", client.name, s );
                            break;

                        case ProtocolDef.clc_disconnect:
                            return false;

                        case ProtocolDef.clc_move:
                            ReadClientMove( client );
                            break;

                        default:
                            _logger.DPrint( "SV_ReadClientMessage: unknown command char\n" );
                            return false;
                    }
                }

                if ( ret != 1 )
                    break;
            }

            return true;
        }

        /// <summary>
        /// SV_ReadClientMove
        /// </summary>
        private void ReadClientMove( client_t client )
        {
            // read ping time
            client.ping_times[client.num_pings % ServerDef.NUM_PING_TIMES] = ( Single ) ( _state.Data.time - _network.Reader.ReadFloat( ) );
            client.num_pings++;

            // read current angles
            var angles = _network.Reader.ReadAngles( );
            MathLib.Copy( ref angles, out client.edict.v.v_angle );

            // read movement
            client.cmd.forwardmove = _network.Reader.ReadShort( );
            client.cmd.sidemove = _network.Reader.ReadShort( );
            client.cmd.upmove = _network.Reader.ReadShort( );

            // read buttons
            var bits = _network.Reader.ReadByte( );
            client.edict.v.button0 = bits & 1;
            client.edict.v.button2 = ( bits & 2 ) >> 1;

            var i = _network.Reader.ReadByte( );

            if ( i != 0 )
                client.edict.v.impulse = i;
        }

        /// <summary>
        /// SV_ClientThink
        /// the move fields specify an intended velocity in pix/sec
        /// the angle fields specify an exact angular motion in degrees
        /// </summary>
        private void ClientThink( client_t client )
        {
            if ( _state.Player.v.movetype == Movetypes.MOVETYPE_NONE )
                return;

            _OnGround = ( ( Int32 ) _state.Player.v.flags & EdictFlags.FL_ONGROUND ) != 0;

            DropPunchAngle( );

            //
            // if dead, behave differently
            //
            if ( _state.Player.v.health <= 0 )
                return;

            //
            // angles
            // show 1/3 the pitch angle and all the roll angle
            _Cmd = client.cmd;

            Vector3f v_angle;
            MathLib.VectorAdd( ref _state.Player.v.v_angle, ref _state.Player.v.punchangle, out v_angle );
            var pang = Utilities.ToVector( ref _state.Player.v.angles );
            var pvel = Utilities.ToVector( ref _state.Player.v.velocity );
            _state.Player.v.angles.z = CalculateRoll( ref pang, ref pvel ) * 4;
            if ( _state.Player.v.fixangle == 0 )
            {
                _state.Player.v.angles.x = -v_angle.x / 3;
                _state.Player.v.angles.y = v_angle.y;
            }

            if ( ( ( Int32 ) _state.Player.v.flags & EdictFlags.FL_WATERJUMP ) != 0 )
            {
                WaterJump( );
                return;
            }
            //
            // walk
            //
            if ( ( _state.Player.v.waterlevel >= 2 ) && ( _state.Player.v.movetype != Movetypes.MOVETYPE_NOCLIP ) )
            {
                WaterMove( );
                return;
            }

            AirMove( );
        }

        /// <summary>
		/// V_CalcRoll - Copied here for now as it removes dependency on VIEW
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
        private void DropPunchAngle( )
        {
            var v = Utilities.ToVector( ref _state.Player.v.punchangle );
            var len = MathLib.Normalize( ref v ) - 10 * Time.Delta;
            if ( len < 0 )
                len = 0;
            v *= ( Single ) len;
            MathLib.Copy( ref v, out _state.Player.v.punchangle );
        }

        /// <summary>
        /// SV_WaterJump
        /// </summary>
        private void WaterJump( )
        {
            if ( _state.Data.time > _state.Player.v.teleport_time || _state.Player.v.waterlevel == 0 )
            {
                _state.Player.v.flags = ( Int32 ) _state.Player.v.flags & ~EdictFlags.FL_WATERJUMP;
                _state.Player.v.teleport_time = 0;
            }
            _state.Player.v.velocity.x = _state.Player.v.movedir.x;
            _state.Player.v.velocity.y = _state.Player.v.movedir.y;
        }

        /// <summary>
        /// SV_WaterMove
        /// </summary>
        private void WaterMove( )
        {
            //
            // user intentions
            //
            var pangle = Utilities.ToVector( ref _state.Player.v.v_angle );
            MathLib.AngleVectors( ref pangle, out _Forward, out _Right, out _Up );
            var wishvel = _Forward * _Cmd.forwardmove + _Right * _Cmd.sidemove;

            if ( _Cmd.forwardmove == 0 && _Cmd.sidemove == 0 && _Cmd.upmove == 0 )
                wishvel.Z -= 60;		// drift towards bottom
            else
                wishvel.Z += _Cmd.upmove;

            var wishspeed = wishvel.Length;
            var maxSpeed = Cvars.MaxSpeed.Get<Single>( );
            if ( wishspeed > maxSpeed )
            {
                wishvel *= maxSpeed / wishspeed;
                wishspeed = maxSpeed;
            }
            wishspeed *= 0.7f;

            //
            // water friction
            //
            Single newspeed, speed = MathLib.Length( ref _state.Player.v.velocity );
            if ( speed != 0 )
            {
                newspeed = ( Single ) ( speed - Time.Delta * speed * Cvars.Friction.Get<Single>( ) );
                if ( newspeed < 0 )
                    newspeed = 0;
                MathLib.VectorScale( ref _state.Player.v.velocity, newspeed / speed, out _state.Player.v.velocity );
            }
            else
                newspeed = 0;

            //
            // water acceleration
            //
            if ( wishspeed == 0 )
                return;

            var addspeed = wishspeed - newspeed;
            if ( addspeed <= 0 )
                return;

            MathLib.Normalize( ref wishvel );
            var accelspeed = ( Single ) ( Cvars.Accelerate.Get<Single>( ) * wishspeed * Time.Delta );
            if ( accelspeed > addspeed )
                accelspeed = addspeed;

            wishvel *= accelspeed;
            _state.Player.v.velocity.x += wishvel.X;
            _state.Player.v.velocity.y += wishvel.Y;
            _state.Player.v.velocity.z += wishvel.Z;
        }

        /// <summary>
        /// SV_AirMove
        /// </summary>
        private void AirMove( )
        {
            var pangles = Utilities.ToVector( ref _state.Player.v.angles );
            MathLib.AngleVectors( ref pangles, out _Forward, out _Right, out _Up );

            var fmove = _Cmd.forwardmove;
            var smove = _Cmd.sidemove;

            // hack to not let you back into teleporter
            if ( _state.Data.time < _state.Player.v.teleport_time && fmove < 0 )
                fmove = 0;

            var wishvel = _Forward * fmove + _Right * smove;

            if ( ( Int32 ) _state.Player.v.movetype != Movetypes.MOVETYPE_WALK )
                wishvel.Z = _Cmd.upmove;
            else
                wishvel.Z = 0;

            _WishDir = wishvel;
            _WishSpeed = MathLib.Normalize( ref _WishDir );
            var maxSpeed = Cvars.MaxSpeed.Get<Single>( );
            if ( _WishSpeed > maxSpeed )
            {
                wishvel *= maxSpeed / _WishSpeed;
                _WishSpeed = maxSpeed;
            }

            if ( _state.Player.v.movetype == Movetypes.MOVETYPE_NOCLIP )
            {
                // noclip
                MathLib.Copy( ref wishvel, out _state.Player.v.velocity );
            }
            else if ( _OnGround )
            {
                UserFriction( );
                Accelerate( );
            }
            else
            {	// not on ground, so little effect on velocity
                AirAccelerate( wishvel );
            }
        }

        /// <summary>
        /// SV_UserFriction
        /// </summary>
        private void UserFriction( )
        {
            var speed = MathLib.LengthXY( ref _state.Player.v.velocity );
            if ( speed == 0 )
                return;

            // if the leading edge is over a dropoff, increase friction
            Vector3 start, stop;
            start.X = stop.X = _state.Player.v.origin.x + _state.Player.v.velocity.x / speed * 16;
            start.Y = stop.Y = _state.Player.v.origin.y + _state.Player.v.velocity.y / speed * 16;
            start.Z = _state.Player.v.origin.z + _state.Player.v.mins.z;
            stop.Z = start.Z - 34;

            var trace = _world.Move( ref start, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref stop, 1, _state.Player );
            var friction = Cvars.Friction.Get<Single>( );
            if ( trace.fraction == 1.0 )
                friction *= Cvars.EdgeFriction.Get<Single>( );

            // apply friction
            var control = speed < Cvars.StopSpeed.Get<Single>( ) ? Cvars.StopSpeed.Get<Single>( ) : speed;
            var newspeed = ( Single ) ( speed - Time.Delta * control * friction );

            if ( newspeed < 0 )
                newspeed = 0;
            newspeed /= speed;

            MathLib.VectorScale( ref _state.Player.v.velocity, newspeed, out _state.Player.v.velocity );
        }

        /// <summary>
        /// SV_Accelerate
        /// </summary>
        private void Accelerate( )
        {
            var currentspeed = Vector3.Dot( Utilities.ToVector( ref _state.Player.v.velocity ), _WishDir );
            var addspeed = _WishSpeed - currentspeed;
            if ( addspeed <= 0 )
                return;

            var accelspeed = ( Single ) ( Cvars.Accelerate.Get<Single>( ) * Time.Delta * _WishSpeed );
            if ( accelspeed > addspeed )
                accelspeed = addspeed;

            _state.Player.v.velocity.x += _WishDir.X * accelspeed;
            _state.Player.v.velocity.y += _WishDir.Y * accelspeed;
            _state.Player.v.velocity.z += _WishDir.Z * accelspeed;
        }

        /// <summary>
        /// SV_AirAccelerate
        /// </summary>
        private void AirAccelerate( Vector3 wishveloc )
        {
            var wishspd = MathLib.Normalize( ref wishveloc );
            if ( wishspd > 30 )
                wishspd = 30;
            var currentspeed = Vector3.Dot( Utilities.ToVector( ref _state.Player.v.velocity ), wishveloc );
            var addspeed = wishspd - currentspeed;
            if ( addspeed <= 0 )
                return;
            var accelspeed = ( Single ) ( Cvars.Accelerate.Get<Single>( ) * _WishSpeed * Time.Delta );
            if ( accelspeed > addspeed )
                accelspeed = addspeed;

            wishveloc *= accelspeed;
            _state.Player.v.velocity.x += wishveloc.X;
            _state.Player.v.velocity.y += wishveloc.Y;
            _state.Player.v.velocity.z += wishveloc.Z;
        }
    }
}
