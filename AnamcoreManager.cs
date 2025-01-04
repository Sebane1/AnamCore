using Anamnesis.Actor;
using Anamnesis.Core.Memory;
using Anamnesis.Memory;
using Anamnesis.Services;
using Anamnesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SamplePlugin;
using System.Diagnostics;
using Dalamud.Game.ClientState.Objects.Types;
using System.Threading;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System.Collections.Concurrent;
using System.Numerics;

namespace AnamCore
{
    public class AnamcoreManager
    {
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
        public Plugin Plugin { get; private set; }
        public AnamcoreManager(Plugin plugin)
        {
            Plugin = plugin;
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
                Plugin.PluginLog.Error(e, e.Message);
            }
        }
        public async void TriggerEmote(nint character, ushort animationId)
        {
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
                Plugin.PluginLog.Warning(e, e.Message);
            }
        }

        public async void TriggerEmoteTimed(ICharacter character, ushort animationId, int time = 2000)
        {
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character.Address);
                var animationMemory = actorMemory.Animation;
                if (animationMemory.BaseOverride != animationId)
                {
                    animationMemory!.BaseOverride = animationId;
                    MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.BaseOverride)), animationId, "Base Override");
                }
                byte originalMode = MemoryService.Read<byte>(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)));
                MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.CharacterModeRaw)), ActorMemory.CharacterModes.Normal, "Animation Mode Override");
                Task.Run(() =>
                {
                    ICharacter reference = character;
                    Thread.Sleep(time);
                    StopEmote(reference.Address);
                });
            }
            catch
            {

            }
        }
        public void TriggerEmoteUntilPlayerMoves(IPlayerCharacter player, ICharacter character, ushort emoteId)
        {
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
            }
            catch
            {

            }
        }
        public async void StopEmote(ICharacter character, byte originalMode)
        {
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
            try
            {
                var actorMemory = new ActorMemory();
                actorMemory.SetAddress(character.Address);
                var animationMemory = actorMemory.Animation;
                MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.LipsOverride)),
                    630, "Lipsync");
                await Task.Run(delegate
                {
                    Thread.Sleep(10000);
                    StopLipSync(character);
                });
                Plugin.PluginLog.Debug("Lipsync Succeeded.");
            }
            catch (Exception e)
            {
                Plugin.PluginLog.Error(e, e.Message);
            }
        }
        public async void SetVoice(ICharacter character, int voice)
        {
            if (character != null)
            {
                try
                {
                    var actorMemory = new ActorMemory();
                    actorMemory.SetAddress(character.Address);
                    MemoryService.Write(actorMemory.GetAddressOfProperty(nameof(ActorMemory.Voice)), voice, "Voice");
                }
                catch (Exception e)
                {
                    Plugin.PluginLog.Error(e, e.Message);
                }
            }
        }
        public async void StopLipSync(ICharacter character)
        {
            if (character != null)
            {
                try
                {
                    var actorMemory = new ActorMemory();
                    actorMemory.SetAddress(character.Address);
                    var animationMemory = actorMemory.Animation;
                    MemoryService.Write(animationMemory.GetAddressOfProperty(nameof(AnimationMemory.LipsOverride)), 154, "Lipsync");
                    Plugin.PluginLog.Debug("Lipsync Stop Succeeded.");
                }
                catch (Exception e)
                {
                    Plugin.PluginLog.Error(e, e.Message);
                }
            }
        }
    }
}
