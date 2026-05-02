using Anamnesis.Actor;
using Anamnesis.Core.Memory;
using Anamnesis.Memory;
using Anamnesis.Services;
using Anamnesis;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Dalamud.Game.ClientState.Objects.Types;
using System.Threading;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System.Collections.Concurrent;
using System.Numerics;

namespace AnamCore
{
    /// <summary>
    /// Writes animation state via embedded Anamnesis actor memory (e.g. BaseOverride, CharacterModeRaw).
    /// Those layouts are patch-sensitive; stale offsets can corrupt actor state and cause intermittent freezes.
    /// Compare with imchillin/Anamnesis actor structures after each FFXIV update.
    /// </summary>
    public class AnamcoreManager
    {
        private static bool IsValidActorAddress(nint address) => address != nint.Zero;

        private MemoryService _memoryService;
        private SettingsService _settingService;
        private GameDataService _gameDataService;
        private AnimationService _animationService;
        private ActorService _actorService;
        private GposeService _gposeService;
        private AddressService _addressService;
        private PoseService _poseService;
        private TargetService _targetService;
        private int _defaultBaseOverride = 0;
        private int _defaultCharacterModeInput = 0;
        private int _defaultCharacterModeRaw = 0;
        ConcurrentDictionary<string, string> _currentlyEmotingCharacters = new ConcurrentDictionary<string, string>();
        public AnamcoreManager()
        {
            try
            {
                _memoryService = new MemoryService();
                _settingService = new SettingsService();
                _gameDataService = new GameDataService();
                _animationService = new AnimationService();
                _actorService = new ActorService();
                _gposeService = new GposeService();
                _addressService = new AddressService();
                _poseService = new PoseService();
                _targetService = new TargetService();
                _memoryService.Initialize();
                _memoryService.OpenProcess(Process.GetCurrentProcess());
                _settingService.Initialize();
                _gameDataService.Initialize();
                _actorService.Initialize();
                _addressService.Initialize();
                _poseService.Initialize();
                _targetService.Initialize();
                _gposeService.Initialize();
                _animationService.Initialize();
                _animationService.Start();
                _memoryService.Start();
                _addressService.Start();
                _poseService.Start();
                _targetService.Start();
                _gposeService.Start();
            }
            catch (Exception e)
            {
            }
        }
        public async void TriggerEmote(nint character, uint animationId)
        {
            if (!IsValidActorAddress(character))
            {
                return;
            }
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character);
                var animationMemory = actorMemory.Animation;
                MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)), animationId, "Base Override");
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)), ActorMemory.CharacterModes.Normal, "Animation Mode Override");
            }
            catch (Exception e)
            {

            }
        }

        public async void TriggerEmoteTimed(ICharacter character, uint animationId, int time = 2000)
        {
            if (character == null || !IsValidActorAddress(character.Address))
            {
                return;
            }
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character.Address);
                var animationMemory = actorMemory.Animation;
                if (animationMemory.BaseOverride != animationId)
                {
                    animationMemory!.BaseOverride = (ushort)animationId;
                    MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)), animationId, "Base Override");
                }
                byte originalMode = MemoryService.Read<byte>(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)));
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)), ActorMemory.CharacterModes.Normal, "Animation Mode Override");
                Task.Run(() =>
                {
                    ICharacter reference = character;
                    Thread.Sleep(time);
                    if (reference != null && IsValidActorAddress(reference.Address))
                    {
                        StopEmote(reference.Address);
                    }
                });
            }
            catch
            {

            }
        }
        public void TriggerEmoteUntilPlayerMoves(IPlayerCharacter player, ICharacter character, ushort emoteId)
        {
            if (player == null || character == null || !IsValidActorAddress(character.Address))
            {
                return;
            }
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character.Address);
                var animationMemory = actorMemory.Animation;
                if (animationMemory.BaseOverride != emoteId)
                {
                    animationMemory!.BaseOverride = emoteId;
                    MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)), emoteId, "Base Override");
                }
                byte originalMode = MemoryService.Read<byte>(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)));
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)), ActorMemory.CharacterModes.Normal, "Animation Mode Override");
                Task.Run(() =>
                {
                    string taskId = Guid.NewGuid().ToString();
                    _currentlyEmotingCharacters[character.GameObjectId.ToString()] = taskId;
                    ICharacter reference = character;
                    Vector3 startingPosition = player.Position;
                    Thread.Sleep(2000);
                    while (_currentlyEmotingCharacters[character.GameObjectId.ToString()] == taskId)
                    {
                        if (Vector3.Distance(startingPosition, player.Position) > 0.001f)
                        {
                            StopEmote(reference.Address);
                            _currentlyEmotingCharacters.Remove(reference.GameObjectId.ToString(), out var item);
                            break;
                        }
                        else
                        {
                            Thread.Sleep(1000 * _currentlyEmotingCharacters.Count);
                        }
                    }
                });
            }
            catch
            {

            }
        }
        public ushort GetCurrentAnimationId(ICharacter character)
        {
            if (character == null || !IsValidActorAddress(character.Address))
            {
                return 0;
            }
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character.Address);
                var animationMemory = actorMemory.Animation;
                return MemoryService.Read<ushort>(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)));
            }
            catch
            {

            }
            return 0;
        }

        public async void StopEmote(nint character)
        {
            if (!IsValidActorAddress(character))
            {
                return;
            }
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character);
                var animationMemory = actorMemory.Animation;

                MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)), _defaultBaseOverride, "Base Override");
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeInput)), _defaultCharacterModeInput, "Animation Mode Input Override");
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)), _defaultCharacterModeRaw, "Animation Mode Override");
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)), ActorMemory.CharacterModes.Normal, "Animation Mode Override");
                MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)), _defaultBaseOverride, "Base Override");
                // Interrupt: zero the FullBody animation slot to force-cancel the active timeline
                animationMemory.AnimationIds[(int)AnimationMemory.AnimationSlots.FullBody].Value = 0;
            }
            catch
            {

            }
        }
        /// <summary>
        /// Force-cancel an emote using Brio's native approach: SetMode + PlayTimeline.
        /// This directly tells the game engine to interrupt the animation timeline.
        /// </summary>
        public unsafe void ForceStopEmote(nint characterAddress)
        {
            if (!IsValidActorAddress(characterAddress) || characterAddress == nint.Zero)
                return;
            try
            {
                var chara = (Character*)characterAddress;
                if (chara == null) return;
                chara->Timeline.BaseOverride = 0;
                chara->SetMode(CharacterModes.Normal, 0);
                chara->Timeline.TimelineSequencer.PlayTimeline(3); // 3 = idle
            }
            catch { }
        }
        public async void StopEmote(ICharacter character, byte originalMode)
        {
            if (character == null || !IsValidActorAddress(character.Address))
            {
                return;
            }
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character.Address);
                var animationMemory = actorMemory.Animation;

                MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)), _defaultBaseOverride, "Base Override");
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeInput)), _defaultCharacterModeInput, "Animation Mode Input Override");
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)), _defaultCharacterModeRaw, "Animation Mode Override");
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)), originalMode, "Animation Mode Override");
                MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)), _defaultBaseOverride, "Base Override");
            }
            catch
            {

            }
        }
        public async void TriggerLipSync(ICharacter character, int lipSyncType)
        {
            if (character == null || !IsValidActorAddress(character.Address))
            {
                return;
            }
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character.Address);
                var animationMemory = actorMemory.Animation;
                MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.LipsOverride)),
                    630, "Lipsync");
            }
            catch (Exception e)
            {
            }
        }
        public async void SetVoice(ICharacter character, int voice)
        {
            if (character != null && IsValidActorAddress(character.Address))
            {
                try
                {
                    var actorMemory = new ActorMemory();
                    actorMemory.SetAddress(character.Address);
                    MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.Voice)), voice, "Voice");
                }
                catch (Exception e)
                {
                }
            }
        }
        public async void StopLipSync(ICharacter character)
        {
            if (character != null && IsValidActorAddress(character.Address))
            {
                try
                {
                    var actorMemory = new ActorMemory();
                    actorMemory.SetAddress(character.Address);
                    var animationMemory = actorMemory.Animation;
                    MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.LipsOverride)), 154, "Lipsync");
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}
