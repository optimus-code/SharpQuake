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
using System.Collections.Generic;
using System.Linq;
using SharpQuake.Framework;
using SharpQuake.Framework.Factories;
using SharpQuake.Framework.Factories.IO;
using SharpQuake.Framework.Factories.IO.WAD;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.WAD;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.Rendering.Textures;
using SharpQuake.Renderer.Textures;
using SharpQuake.Rendering;

// gl_model.c -- model loading and caching

// models are the only shared resource between a client and server running
// on the same machine.

namespace SharpQuake.Factories.Rendering
{
	/// <summary>
	/// Mod_functions
	/// </summary>
	public class ModelFactory : BaseFactory<String, ModelData>
    {
        public Single SubdivideSize
        {
            get
            {
                return _glSubDivideSize.Get<Int32>( );
            }
        }

        private AliasModelBuilder AliasModelBuilder
		{
            get;
            set;
		}

        public List<BaseTexture> SkinTextures
        {
            get;
            private set;
        }

        public List<BaseTexture> SpriteTextures
        {
            get;
            private set;
        }

        private ClientVariable _glSubDivideSize
        {
            get;
            set;
        }

        private ModelData CurrentModel
        {
            get;
            set;
        }

        private readonly ICache _cache;
        private readonly ClientVariableFactory _cvars;
        private readonly WadFactory _wads;
        private readonly Vid _video;
        private readonly IGameRenderer _gameRenderer;

        public ModelFactory( ICache cache, WadFactory wads, ClientVariableFactory cvars, Vid video, IGameRenderer gameRenderer )
        {
            _cache = cache;
            _cvars = cvars;
            _wads = wads;
            _video = video;
            _gameRenderer = gameRenderer;
        }

        /// <summary>
        /// Mod_Init
        /// </summary>
        public void Initialise( )
        {
            SkinTextures = new List<BaseTexture>();
            SpriteTextures = new List<BaseTexture>();
            AliasModelBuilder = new AliasModelBuilder();

            if ( _glSubDivideSize == null )
                _glSubDivideSize = _cvars.Add( "gl_subdivide_size", 128, ClientVariableFlags.Archive );
        }

        /// <summary>
        /// Mod_ClearAll
        /// </summary>
        public void ClearAll( )
        {
            foreach ( var model in DictionaryItems )
            {
                if ( model.Value.Type != ModelType.Alias )
                    model.Value.IsLoadRequired = true;
            }
        }

        /// <summary>
        /// Mod_ForName
        /// Loads in a model for the given name
        /// </summary>
        public ModelData ForName( String name, Boolean crash, ModelType type, Boolean isWorld )
        {
            var mod = FindName( name, type, isWorld );

            return Load( mod, crash, type, isWorld );
        }

        /// <summary>
        /// Mod_Extradata
        /// handles caching
        /// </summary>
        public aliashdr_t GetExtraData( ModelData mod )
        {
            var r = _cache.Check( mod.cache );

            if ( r != null )
                return ( aliashdr_t ) r;

            Load( mod, true, ModelType.Alias, false );

            if ( mod.cache.data == null )
                Utilities.Error( "Mod_Extradata: caching failed" );

            return ( aliashdr_t ) mod.cache.data;
        }

        /// <summary>
        /// Mod_TouchModel
        /// </summary>
        public void TouchModel( String name )
        {
            ModelType type;

            var n = name.ToLower( );

            if ( n.StartsWith( "*" ) && !n.Contains( ".mdl" ) || n.Contains( ".bsp" ) )
                type = ModelType.Brush;
            else if ( n.Contains( ".mdl" ) )
                type = ModelType.Alias;
            else
                type = ModelType.Sprite;

            var mod = FindName( name, type, name.StartsWith( "maps/" ) && !name.StartsWith( "maps/b_" ) );

            if ( !mod.IsLoadRequired )
            {
                if ( mod.Type == ModelType.Alias )
                    _cache.Check( mod.cache );
            }
        } 

        // Mod_Print
        public void Print( CommandMessage msg )
        {
            var names = String.Join( "\n", DictionaryItems.Select( m => m.Key ) );
            ConsoleWrapper.Print( $"Cached models:\n{names}\n" );
        }

        /// <summary>
        /// Mod_FindName
        /// </summary>
        private ModelData FindName( String name, ModelType type, Boolean isWorld )
        {
            ModelData result = null;

            if ( String.IsNullOrEmpty( name ) )
                Utilities.Error( "Mod_ForName: NULL name" );

            if ( !Contains( name ) )
            {
                if ( DictionaryItems.Count == ModelDef.MAX_MOD_KNOWN )
                    Utilities.Error( "mod_numknown == MAX_MOD_KNOWN" );

                switch ( type )
                {
                    case ModelType.Brush:
                        result = new BrushModelData( _video.Device, SubdivideSize, _gameRenderer.NoTextureMip, isWorld );
                        break;

                    case ModelType.Sprite:
                        result = new AliasModelData( _gameRenderer.NoTextureMip );
                        break;

                    case ModelType.Alias:
                        result = new SpriteModelData( _gameRenderer.NoTextureMip );
                        break;
                }

                result.Name = name;
                result.IsLoadRequired = true;
                Add( result.Name, result );
            }
            else
                result = Get( name );

            return result;
        }

        /// <summary>
        /// Mod_LoadModel
        /// Loads a model into the cache
        /// </summary>
        private ModelData Load( ModelData mod, Boolean crash, ModelType type, Boolean isWorld )
        {
            var name = mod.Name;

            if ( mod.Type != type )
            {
                ModelData newMod = null;

                switch ( type )
                {
                    case ModelType.Brush:
                        newMod = new BrushModelData( _video.Device, SubdivideSize, _gameRenderer.NoTextureMip, isWorld );
                        newMod.CopyFrom( mod );
                        break;

                    case ModelType.Alias:
                        newMod = new AliasModelData( _gameRenderer.NoTextureMip );
                        newMod.CopyFrom( mod );
                        break;

                    case ModelType.Sprite:
                        newMod = new SpriteModelData( _gameRenderer.NoTextureMip );
                        newMod.CopyFrom( mod );
                        break;
                }

                newMod.Name = mod.Name;

                Remove( name );

                mod = newMod;

                Add( mod.Name, mod );
            }

            if ( !mod.IsLoadRequired )
            {
                if ( mod.Type == ModelType.Alias )
                {
                    if ( _cache.Check( mod.cache ) != null )
                        return mod;
                }
                else
                    return mod;		// not cached at all
            }

            // Load the file
            var buf = FileSystem.LoadFile( mod.Name );

            if ( buf == null )
            {
                if ( crash )
                    Utilities.Error( "Mod_NumForName: {0} not found", mod.Name );

                return null;
            }

            // Allocate a new model
            Allocate( mod, buf );

            return mod;
        }

        private void Allocate( ModelData mod, Byte[] buf )
        {
            CurrentModel = mod;

            mod.IsLoadRequired = false;

            switch ( BitConverter.ToUInt32( buf, 0 ) )
            {
                case ModelDef.IDPOLYHEADER:
                    LoadAlias( ( AliasModelData ) mod, buf );
                    break;

                case ModelDef.IDSPRITEHEADER:
                    LoadSprite( ( SpriteModelData ) mod, buf );
                    break;

                default:
                    LoadBrush( ( BrushModelData ) mod, buf );
                    break;
            }
        }

        /// <summary>
        /// Mod_LoadAliasModel
        /// </summary>
        private void LoadAlias( AliasModelData mod, Byte[] buffer )
        {
            mod.Load( _video.Device.Palette.Table8to24, mod.Name, buffer, LoadSkinTexture, LoadAliasModelDisplayLists );
        }

        private Int32 LoadSkinTexture( String name, ByteArraySegment buffer, aliashdr_t header )
        {
            var texture = ( Renderer.OpenGL.Textures.GLTexture ) BaseTexture.FromBuffer( _video.Device, name,
                    buffer, header.skinwidth, header.skinheight, true, false );

            SkinTextures.Add( texture );

            return texture.GLDesc.TextureNumber;
        }

        private void LoadAliasModelDisplayLists( AliasModelData model, aliashdr_t header )
        {
            // Build the draw lists
            AliasModelBuilder.MakeDisplayLists( model );

            // Move the complete, relocatable alias model to the cache
            model.cache = _cache.Alloc( aliashdr_t.SizeInBytes * header.frames.Length * maliasframedesc_t.SizeInBytes, null );

            if ( model.cache == null )
                return;

            model.cache.data = header;
        }

        /// <summary>
        /// Mod_LoadSpriteModel
        /// </summary>
        private void LoadSprite( SpriteModelData mod, Byte[] buffer )
        {
            mod.Load( mod.Name, buffer, LoadSpriteTexture );
        }

        private Int32 LoadSpriteTexture( String name, ByteArraySegment buffer, Int32 width, Int32 height )
        {
            var texture = ( Renderer.OpenGL.Textures.GLTexture ) BaseTexture.FromBuffer( _video.Device, name,
                    buffer, width, height, hasMipMap: true, hasAlpha: true );

            SpriteTextures.Add( texture );

            return texture.GLDesc.TextureNumber;
        }

        /// <summary>
        /// Mod_LoadBrushModel
        /// </summary>
        private void LoadBrush( BrushModelData mod, Byte[] buffer )
        {
            mod.Load( mod.Name, buffer, LoadBrushTexture, LoadWadTexture );

            //
            // set up the submodels (FIXME: this is confusing)
            //
            for ( var i = 0; i < mod.NumSubModels; i++ )
            {
                mod.SetupSubModel( ref mod.SubModels[i] );

                if ( i < mod.NumSubModels - 1 )
                {
                    // duplicate the basic information
                    var name = "*" + ( i + 1 ).ToString( );
                    CurrentModel = FindName( name, ModelType.Brush, false );
                    CurrentModel.CopyFrom( mod ); // *loadmodel = *mod;
                    CurrentModel.Name = name; //strcpy (loadmodel->name, name);
                    mod = ( BrushModelData ) CurrentModel; //mod = loadmodel;
                }
            }
        }

        private void LoadBrushTexture( ModelTexture tx )
        {
            if ( tx.name != null && tx.name.StartsWith( "sky" ) )// !Q_strncmp(mt->name,"sky",3))
                _gameRenderer.WarpableTextures.InitSky( tx );
            else
            {
                var diskVersion = BaseTexture.FromFile( _video.Device, "textures/" + tx.name + ".tga", true, false, ignorePool: true );

                if ( diskVersion != null )
                    tx.texture = diskVersion;
                else
                    tx.texture = BaseTexture.FromBuffer( _video.Device, tx.name, new ByteArraySegment( tx.pixels ), ( Int32 ) tx.width, ( Int32 ) tx.height, true, true );
            }
        }

        private WadLumpBuffer LoadWadTexture( String textureFile )
        {
            return _wads.LoadTexture( textureFile );
        }
    }
}
