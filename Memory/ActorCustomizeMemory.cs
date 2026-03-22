// © Anamnesis.
// Licensed under the MIT license.

using System;

namespace Anamnesis.Memory;

public class ActorCustomizeMemory : MemoryBase
{
    public enum Genders : byte
    {
        Masculine = 0,
        Feminine = 1,
    }

    public enum Races : byte
    {
        Hyur = 1,
        Elezen = 2,
        Lalafel = 3,
        Miqote = 4,
        Roegadyn = 5,
        AuRa = 6,
        Hrothgar = 7,
        Viera = 8,
    }

    public enum Tribes : byte
    {
        Midlander = 1,
        Highlander = 2,
        Wildwood = 3,
        Duskwight = 4,
        Plainsfolk = 5,
        Dunesfolk = 6,
        SeekerOfTheSun = 7,
        KeeperOfTheMoon = 8,
        SeaWolf = 9,
        Hellsguard = 10,
        Raen = 11,
        Xaela = 12,
        Helions = 13,
        TheLost = 14,
        Rava = 15,
        Veena = 16,
    }

    public enum Ages : byte
    {
        None = 0,
        Normal = 1,
        Old = 3,
        Young = 4,
    }

    [Flags]
    public enum FacialFeature : byte
    {
        None = 0,
        First = 1,
        Second = 2,
        Third = 4,
        Fourth = 8,
        Fifth = 16,
        Sixth = 32,
        Seventh = 64,
        LegacyTattoo = 128,
    }

    [Bind(0x000, BindFlags.ActorRefresh)] public Races Race { get; set; }
    [Bind(0x001, BindFlags.ActorRefresh)] public Genders Gender { get; set; }
    [Bind(0x002, BindFlags.ActorRefresh)] public Ages Age { get; set; }
    [Bind(0x003, BindFlags.ActorRefresh)] public byte Height { get; set; }
    [Bind(0x004, BindFlags.ActorRefresh)] public Tribes Tribe { get; set; }
    [Bind(0x005, BindFlags.ActorRefresh)] public byte Head { get; set; }
    [Bind(0x006, BindFlags.ActorRefresh)] public byte Hair { get; set; }
    [Bind(0x007, BindFlags.ActorRefresh)] public byte HighlightType { get; set; }
    [Bind(0x008, BindFlags.ActorRefresh)] public byte Skintone { get; set; }
    [Bind(0x009, BindFlags.ActorRefresh)] public byte REyeColor { get; set; }
    [Bind(0x00A, BindFlags.ActorRefresh)] public byte HairTone { get; set; }
    [Bind(0x00B, BindFlags.ActorRefresh)] public byte Highlights { get; set; }
    [Bind(0x00C, BindFlags.ActorRefresh)] public FacialFeature FacialFeatures { get; set; }
    [Bind(0x00D, BindFlags.ActorRefresh)] public byte FacialFeatureColor { get; set; }
    [Bind(0x00E, BindFlags.ActorRefresh)] public byte Eyebrows { get; set; }
    [Bind(0x00F, BindFlags.ActorRefresh)] public byte LEyeColor { get; set; }
    [Bind(0x010, BindFlags.ActorRefresh)] public byte Eyes { get; set; }
    [Bind(0x011, BindFlags.ActorRefresh)] public byte Nose { get; set; }
    [Bind(0x012, BindFlags.ActorRefresh)] public byte Jaw { get; set; }
    [Bind(0x013, BindFlags.ActorRefresh)] public byte Mouth { get; set; }
    [Bind(0x014, BindFlags.ActorRefresh)] public byte LipsToneFurPattern { get; set; }
    [Bind(0x015, BindFlags.ActorRefresh)] public byte EarMuscleTailSize { get; set; }
    [Bind(0x016, BindFlags.ActorRefresh)] public byte TailEarsType { get; set; }
    [Bind(0x017, BindFlags.ActorRefresh)] public byte Bust { get; set; }
    [Bind(0x018, BindFlags.ActorRefresh)] public byte FacePaint { get; set; }
    [Bind(0x019, BindFlags.ActorRefresh)] public byte FacePaintColor { get; set; }
}
