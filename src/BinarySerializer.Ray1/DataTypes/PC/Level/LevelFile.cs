﻿namespace BinarySerializer.Ray1.PC
{
    /// <summary>
    /// Level data for PC
    /// </summary>
    public class LevelFile : BinarySerializable
    {
        #region Public Properties

        public GameVersion GameVersion { get; set; }

        /// <summary>
        /// The pointer to the object block. The game uses this to skip the texture block if the game should use the rough textures.
        /// </summary>
        public Pointer ObjDataBlockPointer { get; set; }

        /// <summary>
        /// The pointer to the texture block. The game uses this to skip the rough the textures if the game should use the normal textures.
        /// </summary>
        public Pointer TextureBlockPointer { get; set; }

        public LevelDefine LevelDefine { get; set; }
        public BackgroundDefine BackgroundDefineNormal { get; set; }
        public BackgroundDefine BackgroundDefineDiff { get; set; }

        /// <summary>
        /// The map data
        /// </summary>
        public LevelMap Map { get; set; }

        /// <summary>
        /// The index of the background image
        /// </summary>
        public byte FNDIndex { get; set; }

        /// <summary>
        /// The index of the parallax background image
        /// </summary>
        public byte ScrollDiffFNDIndex { get; set; }

        /// <summary>
        /// The DES for the background sprites when parallax scrolling is enabled
        /// </summary>
        public int ScrollDiffSprites { get; set; }

        /// <summary>
        /// The rough tile texture data
        /// </summary>
        public RoughTileTextureBlock RoughTileTextureData { get; set; }

        // Leftover data for the rough textures in versions which don't use it. In RayKit there is still a lot of data there, but it doesn't appear to match with how it's being parsed.
        public byte[] LeftoverRoughTextureBlock { get; set; }

        public TileTextureBlock TileTextureData { get; set; }

        public LevelObjects ObjData { get; set; }

        public ProfileDefine ProfileDefine { get; set; }

        public byte[][] Alpha { get; set; }

        #endregion

        #region Public Methods

        public override void SerializeImpl(SerializerObject s) 
        {
            Ray1Settings settings = s.GetRequiredSettings<Ray1Settings>();

            // Serialize the header
            if (settings.IsVersioned)
                GameVersion = s.SerializeObject<GameVersion>(GameVersion, name: nameof(GameVersion));

            var pointersOffset = s.CurrentPointer;

            // Serialize the pointers
            bool allowInvalid = settings.PCVersion == Ray1PCVersion.PocketPC || 
                                settings.PCVersion == Ray1PCVersion.Android ||
                                settings.PCVersion == Ray1PCVersion.iOS;
            ObjDataBlockPointer = s.SerializePointer(ObjDataBlockPointer, allowInvalid: allowInvalid, name: nameof(ObjDataBlockPointer));
            TextureBlockPointer = s.SerializePointer(TextureBlockPointer, allowInvalid: allowInvalid, name: nameof(TextureBlockPointer));

            // Serialize the level defines
            if (settings.EngineVersion == Ray1EngineVersion.PC_Kit || 
                settings.EngineVersion == Ray1EngineVersion.PC_Edu || 
                settings.EngineVersion == Ray1EngineVersion.PC_Fan)
            {
                LevelDefine = s.SerializeObject<LevelDefine>(LevelDefine, name: nameof(LevelDefine));
                BackgroundDefineNormal = s.SerializeObject<BackgroundDefine>(BackgroundDefineNormal, name: nameof(BackgroundDefineNormal));
                BackgroundDefineDiff = s.SerializeObject<BackgroundDefine>(BackgroundDefineDiff, name: nameof(BackgroundDefineDiff));
            }

            // Serialize the map data
            Map = s.SerializeObject<LevelMap>(Map, name: nameof(Map));

            // Serialize the background data
            if (settings.EngineVersion == Ray1EngineVersion.PC || settings.EngineVersion == Ray1EngineVersion.PocketPC)
            {
                // Serialize the background data
                FNDIndex = s.Serialize<byte>(FNDIndex, name: nameof(FNDIndex));
                ScrollDiffFNDIndex = s.Serialize<byte>(ScrollDiffFNDIndex, name: nameof(ScrollDiffFNDIndex));
                ScrollDiffSprites = s.Serialize<int>(ScrollDiffSprites, name: nameof(ScrollDiffSprites));
            }

            // Serialize the rough tile textures
            if (settings.EngineVersion == Ray1EngineVersion.PC)
                RoughTileTextureData = s.SerializeObject<RoughTileTextureBlock>(RoughTileTextureData, name: nameof(RoughTileTextureData));
            else
                LeftoverRoughTextureBlock = s.SerializeArray<byte>(LeftoverRoughTextureBlock, TextureBlockPointer.FileOffset - s.CurrentPointer.FileOffset, name: nameof(LeftoverRoughTextureBlock));

            // At this point the stream position should match the texture block offset
            if (s.CurrentPointer != TextureBlockPointer)
                s.Context.SystemLogger?.LogWarning("Texture block offset is incorrect");

            // Serialize the tile textures
            TileTextureData = s.SerializeObject<TileTextureBlock>(TileTextureData, name: nameof(TileTextureData));

            // At this point the stream position should match the obj block offset (ignore the Pocket PC version here since it uses leftover pointers from PC version)
            if (settings.EngineVersion != Ray1EngineVersion.PocketPC && s.CurrentPointer != ObjDataBlockPointer)
                s.Context.SystemLogger?.LogWarning("Object block offset is incorrect");

            // Serialize the object data
            ObjData = s.SerializeObject<LevelObjects>(ObjData, name: nameof(ObjData));

            // Serialize the profile define data (only on By his Fans and 60 Levels)
            if (settings.EngineVersion == Ray1EngineVersion.PC_Fan)
                ProfileDefine = s.SerializeObject<ProfileDefine>(ProfileDefine, name: nameof(ProfileDefine));

            // Serialize alpha data (only on EDU)
            if (settings.EngineVersion == Ray1EngineVersion.PC_Edu)
            {
                s.DoProcessed(new Checksum8Processor(), p =>
                {
                    p.Serialize<byte>(s, "AlphaChecksum");

                    Alpha = s.InitializeArray(Alpha, 480);
                    s.DoArray(Alpha, (x, name) => s.SerializeArray<byte>(x, 256, name: name), name: nameof(Alpha));
                });
            }

            // Correct pointers
            s.DoAt(pointersOffset, () =>
            {
                s.SerializePointer(ObjData.Offset, allowInvalid: allowInvalid, name: nameof(ObjDataBlockPointer));
                s.SerializePointer(TileTextureData.Offset, allowInvalid: allowInvalid, name: nameof(TextureBlockPointer));
            });
        }

        #endregion
    }
}