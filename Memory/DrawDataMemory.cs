// © Anamnesis.
// Licensed under the MIT license.
// Layout matches Client::Game::Character::DrawDataContainer (FFXIVClientStructs).

using System;

namespace Anamnesis.Memory;

public class DrawDataMemory : MemoryBase
{
    [Flags]
    public enum CharacterFlagDefs : byte
    {
        None = 0,
        WeaponsVisible = 1 << 0,
        WeaponsDrawn = 1 << 2,
        VisorToggled = 1 << 4,
        HeadgearEarsHidden = 1 << 5,
    }

    [Bind(0x010)] public WeaponMemory? MainHand { get; set; }
    [Bind(0x080)] public WeaponMemory? OffHand { get; set; }
    [Bind(0x1D0)] public ActorEquipmentMemory? Equipment { get; set; }
    [Bind(0x220)] public ActorCustomizeMemory? Customize { get; set; }
    [Bind(0x23E, BindFlags.ActorRefresh)] public bool HatHidden { get; set; }
    [Bind(0x23F, BindFlags.ActorRefresh)] public CharacterFlagDefs CharacterFlags { get; set; }
    [Bind(0x240)] public GlassesMemory? Glasses { get; set; }
}
