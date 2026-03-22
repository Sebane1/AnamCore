// © Anamnesis.
// Licensed under the MIT license.

namespace Anamnesis.Memory;

/// <summary>
/// Offsets relative to the live actor object (see FFXIVClientStructs Character).
/// </summary>
public static class Actor
{
    /// <summary>Offset of <c>Client::Game::Character::DrawDataContainer</c> (Character.DrawData).</summary>
    public const int DrawDataOffset = 0x6F8;
}
