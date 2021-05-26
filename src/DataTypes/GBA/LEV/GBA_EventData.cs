﻿using System.Linq;

namespace BinarySerializer.Ray1
{
    /// <summary>
    /// Event instance data
    /// </summary>
    public class GBA_EventData : BinarySerializable
    {
        public Pointer ETAPointer { get; set; }

        public Pointer CommandsPointer { get; set; }

        public short XPosition { get; set; }
        public short YPosition { get; set; }

        public ushort DisplayPrio { get; set; }

        public ushort LinkGroup { get; set; }

        public byte Etat { get; set; }
        public byte SubEtat { get; set; }
        public byte OffsetBX { get; set; }
        public byte OffsetBY { get; set; }
        public byte OffsetHY { get; set; }

        public bool FollowEnabled { get; set; }

        public byte FollowSprite { get; set; }
        public byte HitPoints { get; set; }

        public ObjType Type { get; set; }

        public byte HitSprite { get; set; }

        public ObjState[][] ETA { get; set; }

        /// <summary>
        /// The event commands
        /// </summary>
        public CommandCollection Commands { get; set; }

        /// <summary>
        /// Handles the data serialization
        /// </summary>
        /// <param name="s">The serializer object</param>
        public override void SerializeImpl(SerializerObject s)
        {
            var settings = s.GetSettings<Ray1Settings>();

            ETAPointer = s.SerializePointer(ETAPointer, name: nameof(ETAPointer));
            CommandsPointer = s.SerializePointer(CommandsPointer, name: nameof(CommandsPointer));
            XPosition = s.Serialize<short>(XPosition, name: nameof(XPosition));
            YPosition = s.Serialize<short>(YPosition, name: nameof(YPosition));
            DisplayPrio = s.Serialize<ushort>(DisplayPrio, name: nameof(DisplayPrio));
            LinkGroup = s.Serialize<ushort>(LinkGroup, name: nameof(LinkGroup));
            Etat = s.Serialize<byte>(Etat, name: nameof(Etat));
            SubEtat = s.Serialize<byte>(SubEtat, name: nameof(SubEtat));
            OffsetBX = s.Serialize<byte>(OffsetBX, name: nameof(OffsetBX));
            OffsetBY = s.Serialize<byte>(OffsetBY, name: nameof(OffsetBY));
            OffsetHY = s.Serialize<byte>(OffsetHY, name: nameof(OffsetHY));
            FollowEnabled = s.Serialize<bool>(FollowEnabled, name: nameof(FollowEnabled));
            FollowSprite = s.Serialize<byte>(FollowSprite, name: nameof(FollowSprite));
            HitPoints = s.Serialize<byte>(HitPoints, name: nameof(HitPoints));
            Type = s.Serialize<ObjType>(Type, name: nameof(Type));
            HitSprite = s.Serialize<byte>(HitSprite, name: nameof(HitSprite));
            s.SerializePadding(1);

            // Serialize data from pointers

            // Serialize the current state
            if(ETAPointer != null) 
            {
                do
                {
                    var hasSerialized = ETA != null && s is BinaryDeserializer;

                    int etatCount;

                    if (!hasSerialized)
                    {
                        etatCount = Etat + 1;

                        // Get correct etat count on GBA
                        if (settings.EngineVersion == Ray1EngineVersion.GBA)
                        {
                            s.DoAt(ETAPointer, () => {
                                int curEtatCount = 0;
                                Pointer off_prev = null;
                                while (true)
                                {
                                    Pointer off_next = s.SerializePointer(null, allowInvalid: true, name: "TestPointer");
                                    if (curEtatCount < etatCount 
                                        || (off_next != null 
                                        && off_next != ETAPointer
                                        && (off_prev == null 
                                        || ((off_next.AbsoluteOffset - off_prev.AbsoluteOffset > 0) && (off_next.AbsoluteOffset - off_prev.AbsoluteOffset < 0x10000))
                                        || (off_next.File != off_prev.File && off_next.File != Offset.File)
                                        )))
                                    {
                                        curEtatCount++;
                                        off_prev = off_next;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                etatCount = curEtatCount;
                            });
                        }
                    }
                    else
                    {
                        etatCount = ETA.Length;
                    }

                    // Get max linked etat if we've already serialized ETA
                    if (hasSerialized)
                    {
                        var maxLinked = ETA.SelectMany(x => x).Where(x => x != null).Max(x => x.LinkedEtat) + 1;

                        if (etatCount < maxLinked)
                            etatCount = maxLinked;
                    }

                    // Serialize etat pointers
                    Pointer[] EtatPointers = s.DoAt(ETAPointer, () => s.SerializePointerArray(default, etatCount, name: $"{nameof(EtatPointers)}"));

                    // Serialize subetats
                    var prevETA = ETA;
                    ETA = new ObjState[EtatPointers.Length][];
                    for (int j = 0; j < EtatPointers.Length; j++)
                    {
                        int count;

                        if (!hasSerialized || prevETA.Length <= j)
                        {
                            if (settings.EngineVersion == Ray1EngineVersion.DSi)
                            {
                                count = j == Etat ? (SubEtat + 1) : 1;
                            }
                            else
                            {
                                if (EtatPointers[j].File is MemoryMappedByteArrayFile && EtatPointers[j].FileOffset == 0) {
                                    count = (int)(((MemoryMappedByteArrayFile)(EtatPointers[j].File)).Length / 8);
                                } else {
                                    Pointer nextPointer = j < EtatPointers.Length - 1 ? EtatPointers[j + 1] : ETAPointer;
                                    count = (int)((nextPointer - EtatPointers[j]) / 8);
                                }
                            }
                        }
                        else
                        {
                            count = prevETA[j].Length;

                            // Get max linked subetat
                            var validLinks = prevETA.SelectMany(x => x).Where(x => x?.LinkedEtat == j).ToArray();
                            var maxLinked = validLinks.Any() ? validLinks.Max(x => x.LinkedSubEtat) + 1 : -1;

                            if (count < maxLinked)
                                count = maxLinked;
                        }

                        // Serialize all states
                        s.DoAt(EtatPointers[j], () => ETA[j] = s.SerializeObjectArray<ObjState>(ETA[j], count, name: $"{nameof(ETA)}[{j}]"));

                        ETA[j] ??= new ObjState[count];
                    }
                } while (!ETA.SelectMany(x => x).Where(x => x != null).All(eta => ETA.Length > eta.LinkedEtat && ETA[eta.LinkedEtat].Length > eta.LinkedSubEtat));
            }

            if (CommandsPointer != null)
                s.DoAt(CommandsPointer, () => Commands = s.SerializeObject<CommandCollection>(Commands, name: nameof(Commands)));
        }
    }
}