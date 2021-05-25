﻿namespace BinarySerializer.Ray1
{
    /// <summary>
    /// DES item data for PC
    /// </summary>
    public class PC_DES : BinarySerializable 
    {
        public Type Pre_FileType { get; set; }

        // TODO: Is that what this property is for? It seems it's true for all DES except the parallax ones.
        /// <summary>
        /// Indicates if the sprite has some gradation and requires clearing
        /// </summary>
        public bool RequiresBackgroundClearing { get; set; }

        public uint WldETAIndex { get; set; }
        public uint RaymanExeSize { get; set; }
        public uint RaymanExeCheckSum1 { get; set; }

        /// <summary>
        /// The length of the image data
        /// </summary>
        public uint ImageDataLength { get; set; }

        /// <summary>
        /// The image data
        /// </summary>
        public byte[] ImageData { get; set; }

        /// <summary>
        /// The checksum for <see cref="ImageData"/>
        /// </summary>
        public byte ImageDataChecksum { get; set; }

        public uint RaymanExeCheckSum2 { get; set; }

        /// <summary>
        /// The amount of sprites
        /// </summary>
        public ushort SpritesCount { get; set; }

        /// <summary>
        /// The sprites
        /// </summary>
        public Sprite[] Sprites { get; set; }

        /// <summary>
        /// The amount of animations
        /// </summary>
        public byte AnimationsCount { get; set; }

        /// <summary>
        /// The animations
        /// </summary>
        public PC_Animation[] Animations { get; set; }

        /// <summary>
        /// Handles the data serialization
        /// </summary>
        /// <param name="s">The serializer object</param>
        public override void SerializeImpl(SerializerObject s) 
        {
            var settings = s.GetSettings<Ray1Settings>();

            if (Pre_FileType == Type.World)
                RequiresBackgroundClearing = s.Serialize<bool>(RequiresBackgroundClearing, name: nameof(RequiresBackgroundClearing));
            else
                RequiresBackgroundClearing = true;

            if (Pre_FileType == Type.AllFix)
            {
                WldETAIndex = s.Serialize<uint>(WldETAIndex, name: nameof(WldETAIndex));
                RaymanExeSize = s.Serialize<uint>(RaymanExeSize, name: nameof(RaymanExeSize));
                RaymanExeCheckSum1 = s.Serialize<uint>(RaymanExeCheckSum1, name: nameof(RaymanExeCheckSum1));
            }

            ImageDataLength = s.Serialize<uint>(ImageDataLength, name: nameof(ImageDataLength));

            var isChecksumBefore = Pre_FileType == Type.World && (settings.EngineVersion == Ray1EngineVersion.R1_PC_Kit || 
                                                              settings.EngineVersion == Ray1EngineVersion.R1_PC_Edu);
            var hasChecksum = isChecksumBefore || Pre_FileType != Type.BigRay;

            ImageDataChecksum = s.DoChecksum(new Checksum8Calculator(false), () =>
            {
                s.DoXOR(0x8F, () =>
                {
                    ImageData = s.SerializeArray<byte>(ImageData, ImageDataLength, name: nameof(ImageData));
                });
            }, isChecksumBefore ? ChecksumPlacement.Before : ChecksumPlacement.After, calculateChecksum: hasChecksum, name: nameof(ImageDataChecksum));

            if (Pre_FileType == Type.AllFix)
                RaymanExeCheckSum2 = s.Serialize<uint>(RaymanExeCheckSum2, name: nameof(RaymanExeCheckSum2));

            SpritesCount = s.Serialize<ushort>(SpritesCount, name: nameof(SpritesCount));
            Sprites = s.SerializeObjectArray<Sprite>(Sprites, SpritesCount, name: nameof(Sprites));
            AnimationsCount = s.Serialize<byte>(AnimationsCount, name: nameof(AnimationsCount));
            Animations = s.SerializeObjectArray<PC_Animation>(Animations, AnimationsCount, name: nameof(Animations));
        }

        public enum Type
        {
            World,
            AllFix,
            BigRay
        }
    }
}