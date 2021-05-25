﻿namespace BinarySerializer.Ray1
{
    /// <summary>
    /// Common image descriptor data
    /// </summary>
    public class Sprite : BinarySerializable
    {
        /// <summary>
        /// The image buffer offset. In final PS1 versions this is always 0 except for backgrounds.
        /// </summary>
        public uint ImageBufferOffset { get; set; }

        /// <summary>
        /// Index of the image? Doesn't always match. Is 0 for dummy sprites.
        /// </summary>
        public ushort Index { get; set; }
        
        /// <summary>
        /// The outer image width
        /// </summary>
        public ushort Width { get; set; }

        /// <summary>
        /// The outer image height
        /// </summary>
        public ushort Height { get; set; }

        /// <summary>
        /// The inner image width
        /// </summary>
        public byte HitBoxWidth { get; set; }

        /// <summary>
        /// The inner image height
        /// </summary>
        public byte HitBoxHeight { get; set; }

        /// <summary>
        /// Image type (JP versions).
        /// 3: 8-bit
        /// 2: 4-bit
        /// 1: Null?
        /// </summary>
        public ushort ImageType { get; set; }

        // Some of these are hitbox related
        public byte HitBoxOffsetX { get; set; }
        public byte HitBoxOffsetY { get; set; }
        public byte Unknown1 { get; set; }
        public byte Unknown2 { get; set; }
        public byte Unknown3 { get; set; }
        public byte Unknown4 { get; set; }

        public ushort PaletteInfo { get; set; }
        public ushort TexturePageInfo { get; set; }
        public byte ImageOffsetInPageX { get; set; }
        public byte ImageOffsetInPageY { get; set; }
        public ushort Unknown6 { get; set; }

        public byte JAG_Byte_03 { get; set; }
        public byte JAG_Byte_04 { get; set; }
        public ushort JAG_Byte_05 { get; set; }
        public ushort JAG_Ushort_07 { get; set; }

        // Four bits from offset 1 are palette offset for 4-bit sprites
        public byte JAG_Byte_0A { get; set; }
        public byte[] JAG_Bytes_0B { get; set; }

        // Flags - bit 4 indicates if it's 8-bit (otherwise 4-bit)
        public byte JAG_Byte_0E { get; set; }

        public bool IsDummySprite()
        {
            // Get the settings
            var settings = Context.GetSettings<Ray1Settings>();

            if (settings.EngineBranch == Ray1EngineBranch.Jaguar)
                return Height == 0 || Width == 0 || Index == 0xFF;

            // Rayman 2 doesn't have dummy sprites
            if (settings.EngineVersion == Ray1EngineVersion.R2_PS1)
                return false;

            return Index == 0;
        }

        /// <summary>
        /// Handles the data serialization
        /// </summary>
        /// <param name="s">The serializer object</param>
        public override void SerializeImpl(SerializerObject s) 
        {
            var settings = s.GetSettings<Ray1Settings>();

            if (settings.EngineBranch == Ray1EngineBranch.Jaguar)
            {
                ImageBufferOffset = s.Serialize<UInt24>((UInt24)ImageBufferOffset, name: nameof(ImageBufferOffset));
                JAG_Byte_03 = s.Serialize<byte>(JAG_Byte_03, name: nameof(JAG_Byte_03));
                JAG_Byte_04 = s.Serialize<byte>(JAG_Byte_04, name: nameof(JAG_Byte_04));
                
                s.SerializeBitValues<ushort>(bitFunc =>
                {
                    JAG_Byte_05 = (byte)bitFunc(JAG_Byte_05, 6, name: nameof(JAG_Byte_05));
                    Height = (ushort)bitFunc(Height, 10, name: nameof(Height));
                });

                JAG_Ushort_07 = s.Serialize<ushort>(JAG_Ushort_07, name: nameof(JAG_Ushort_07));
                Width = s.Serialize<byte>((byte)Width, name: nameof(Width));
                JAG_Byte_0A = s.Serialize<byte>(JAG_Byte_0A, name: nameof(JAG_Byte_0A));
                JAG_Bytes_0B = s.SerializeArray<byte>(JAG_Bytes_0B, 3, name: nameof(JAG_Bytes_0B));
                JAG_Byte_0E = s.Serialize<byte>(JAG_Byte_0E, name: nameof(JAG_Byte_0E));
                Index = s.Serialize<byte>((byte)Index, name: nameof(Index));
            }
            else
            {
                if (settings.EngineVersion != Ray1EngineVersion.R2_PS1)
                    ImageBufferOffset = s.Serialize<uint>(ImageBufferOffset, name: nameof(ImageBufferOffset));

                // PS1
                if (settings.EngineBranch == Ray1EngineBranch.PS1)
                {
                    if (settings.EngineVersion == Ray1EngineVersion.R2_PS1)
                    {
                        Width = s.Serialize<byte>((byte)Width, name: nameof(Width));
                        Height = s.Serialize<byte>((byte)Height, name: nameof(Height));
                        HitBoxWidth = s.Serialize<byte>(HitBoxWidth, name: nameof(HitBoxWidth));
                        HitBoxHeight = s.Serialize<byte>(HitBoxHeight, name: nameof(HitBoxHeight));
                        s.SerializeBitValues<byte>(bitFunc =>
                        {
                            HitBoxOffsetX = (byte)bitFunc(HitBoxOffsetX, 4, name: nameof(HitBoxOffsetX));
                            HitBoxOffsetY = (byte)bitFunc(HitBoxOffsetY, 4, name: nameof(HitBoxOffsetY));
                        });
                        Unknown4 = s.Serialize<byte>(Unknown4, name: nameof(Unknown4));
                        ImageOffsetInPageX = s.Serialize<byte>(ImageOffsetInPageX, name: nameof(ImageOffsetInPageX));
                        ImageOffsetInPageY = s.Serialize<byte>(ImageOffsetInPageY, name: nameof(ImageOffsetInPageY));
                        PaletteInfo = s.Serialize<ushort>(PaletteInfo, name: nameof(PaletteInfo));
                        TexturePageInfo = s.Serialize<ushort>(TexturePageInfo, name: nameof(TexturePageInfo));
                    }
                    else if (settings.EngineVersion == Ray1EngineVersion.R1_PS1_JP || 
                             settings.EngineVersion == Ray1EngineVersion.R1_PS1_JPDemoVol3 ||
                             settings.EngineVersion == Ray1EngineVersion.R1_PS1_JPDemoVol6)
                    {
                        Index = s.Serialize<ushort>(Index, name: nameof(Index));
                        ImageType = s.Serialize<ushort>(ImageType, name: nameof(ImageType));
                        Width = s.Serialize<ushort>(Width, name: nameof(Width));
                        Height = s.Serialize<ushort>(Height, name: nameof(Height));

                        // Which value is this?
                        Unknown1 = s.Serialize<byte>(Unknown1, name: nameof(Unknown1));
                        Unknown2 = s.Serialize<byte>(Unknown2, name: nameof(Unknown2));
                    }
                    else if (settings.EngineVersion == Ray1EngineVersion.R1_Saturn)
                    {
                        Index = s.Serialize<ushort>(Index, name: nameof(Index));

                        ImageType = s.Serialize<ushort>(ImageType, name: nameof(ImageType));

                        Width = s.Serialize<ushort>(Width, name: nameof(Width));
                        Height = s.Serialize<ushort>(Height, name: nameof(Height));

                        PaletteInfo = s.Serialize<ushort>(PaletteInfo, name: nameof(PaletteInfo));

                        Unknown1 = s.Serialize<byte>(Unknown1, name: nameof(Unknown1));
                        Unknown2 = s.Serialize<byte>(Unknown2, name: nameof(Unknown2));
                    }
                    else
                    {
                        Index = s.Serialize<byte>((byte)Index, name: nameof(Index));
                        Width = s.Serialize<byte>((byte)Width, name: nameof(Width));
                        Height = s.Serialize<byte>((byte)Height, name: nameof(Height));
                        HitBoxWidth = s.Serialize<byte>(HitBoxWidth, name: nameof(HitBoxWidth));
                        HitBoxHeight = s.Serialize<byte>(HitBoxHeight, name: nameof(HitBoxHeight));
                        s.SerializeBitValues<byte>(bitFunc =>
                        {
                            HitBoxOffsetX = (byte)bitFunc(HitBoxOffsetX, 4, name: nameof(HitBoxOffsetX));
                            HitBoxOffsetY = (byte)bitFunc(HitBoxOffsetY, 4, name: nameof(HitBoxOffsetY));
                        });
                    }

                    if (settings.EngineVersion != Ray1EngineVersion.R1_Saturn &&
                        settings.EngineVersion != Ray1EngineVersion.R2_PS1)
                    {
                        Unknown3 = s.Serialize<byte>(Unknown3, name: nameof(Unknown3));
                        Unknown4 = s.Serialize<byte>(Unknown4, name: nameof(Unknown4));
                        PaletteInfo = s.Serialize<ushort>(PaletteInfo, name: nameof(PaletteInfo));

                        TexturePageInfo = s.Serialize<ushort>(TexturePageInfo, name: nameof(TexturePageInfo));
                        ImageOffsetInPageX = s.Serialize<byte>(ImageOffsetInPageX, name: nameof(ImageOffsetInPageX));
                        ImageOffsetInPageY = s.Serialize<byte>(ImageOffsetInPageY, name: nameof(ImageOffsetInPageY));
                        Unknown6 = s.Serialize<ushort>(Unknown6, name: nameof(Unknown6));
                    }
                }
                // PC
                else if (settings.EngineBranch == Ray1EngineBranch.PC || settings.EngineBranch == Ray1EngineBranch.GBA)
                {
                    Index = s.Serialize<byte>((byte)Index, name: nameof(Index));
                    Width = s.Serialize<byte>((byte)Width, name: nameof(Width));
                    Height = s.Serialize<byte>((byte)Height, name: nameof(Height));
                    HitBoxWidth = s.Serialize<byte>(HitBoxWidth, name: nameof(HitBoxWidth));
                    HitBoxHeight = s.Serialize<byte>(HitBoxHeight, name: nameof(HitBoxHeight));
                    s.SerializeBitValues<byte>(bitFunc =>
                    {
                        HitBoxOffsetX = (byte)bitFunc(HitBoxOffsetX, 4, name: nameof(HitBoxOffsetX));
                        HitBoxOffsetY = (byte)bitFunc(HitBoxOffsetY, 4, name: nameof(HitBoxOffsetY));
                    });
                    Unknown3 = s.Serialize<byte>(Unknown3, name: nameof(Unknown3));
                    Unknown4 = s.Serialize<byte>(Unknown4, name: nameof(Unknown4));
                }
            }
        }
    }
}