// © Anamnesis.
// Licensed under the MIT license.

namespace Anamnesis.Memory;

using System;
using System.Threading.Tasks;
using Anamnesis.Utils;


public class ActorMemory : ActorBasicMemory
{
	private readonly FuncQueue refreshQueue;
	private readonly FuncQueue backupQueue;

	public ActorMemory()
	{
		this.refreshQueue = new(this.RefreshAsync, 250);
		this.backupQueue = new(this.BackupAsync, 250);
	}

	public enum CharacterModes : byte
	{
		None = 0,
		Normal = 1,
		EmoteLoop = 3,
		Mounted = 4,
		AnimLock = 8,
		Carrying = 9,
		InPositionLoop = 11,
		Performance = 16,
	}

    // Offsets aligned with FFXIVClientStructs Character / imchillin/Anamnesis master.
    [Bind(0x01CA)] public byte ClassJob { get; set; } // Source: CharacterData; Calculated using GameObject size + ClassJob offset
    [Bind(0x0680, BindFlags.Pointer)] public ActorMemory? Mount { get; set; } // Targets object within MountContainer
    [Bind(0x0688)] public ushort MountId { get; set; }
    [Bind(0x06E8, BindFlags.Pointer)] public ActorMemory? Companion { get; set; } // Targets object within CompanionContainer
    [Bind(Actor.DrawDataOffset)] public DrawDataMemory DrawData { get; set; } = new();
    [Bind(0x0970, BindFlags.Pointer)] public ActorMemory? Ornament { get; set; } // Targets object within OrnamentContainer
    [Bind(0x0978)] public ushort OrnamentId { get; set; }
    [Bind(0x0A30)] public AnimationMemory? Animation { get; set; }
    [Bind(0x1A58)] public byte Voice { get; set; }
    [Bind(0x1B38, BindFlags.ActorRefresh)] public int ModelType { get; set; }
    [Bind(0x1BA4)] public bool IsMotionDisabled { get; set; }
    [Bind(0x22E8)] public float Transparency { get; set; }
    [Bind(0x2364)] public byte CharacterModeRaw { get; set; }
    [Bind(0x2365)] public byte CharacterModeInput { get; set; }
    [Bind(0x2384)] public byte AttachmentPoint { get; set; } // Part of Ornament


    public PinnedActor? Pinned { get; set; }

	public History History { get; private set; } = new();

	public bool AutomaticRefreshEnabled { get; set; } = true;
	public bool IsRefreshing { get; set; } = false;
	public bool IsWeaponDirty { get; set; } = false;
	public bool PendingRefresh => this.refreshQueue.Pending;

	//[DependsOn(nameof(IsValid), nameof(IsOverworldActor), nameof(Name), nameof(RenderMode))]
	public bool CanRefresh => ActorService.Instance.CanRefreshActor(this);


	//[DependsOn(nameof(ModelType))]
	public bool IsChocobo => this.ModelType == 1;

	//[DependsOn(nameof(CharacterModeRaw))]
	public CharacterModes CharacterMode
	{
		get
		{
			return (CharacterModes)this.CharacterModeRaw;
		}
		set
		{
			this.CharacterModeRaw = (byte)value;
		}
	}

	//[DependsOn(nameof(MountId), nameof(Mount))]
	public bool IsMounted => this.MountId != 0 && this.Mount != null;

	//[DependsOn(nameof(OrnamentId), nameof(Ornament))]
	public bool IsUsingOrnament => this.Ornament != null && this.OrnamentId != 0;

	//[DependsOn(nameof(Companion))]
	public bool HasCompanion => this.Companion != null;

	public bool VisorToggled
	{
		get => this.DrawData.CharacterFlags.HasFlag(DrawDataMemory.CharacterFlagDefs.VisorToggled);
		set
		{
			if (value)
			{
				this.DrawData.CharacterFlags |= DrawDataMemory.CharacterFlagDefs.VisorToggled;
			}
			else
			{
				this.DrawData.CharacterFlags &= ~DrawDataMemory.CharacterFlagDefs.VisorToggled;
			}
		}
	}

	//[DependsOn(nameof(ObjectIndex), nameof(CharacterMode))]
	public bool CanAnimate => (this.CharacterMode == CharacterModes.Normal || this.CharacterMode == CharacterModes.AnimLock) || !ActorService.Instance.IsLocalOverworldPlayer(this.ObjectIndex);

	//[DependsOn(nameof(CharacterMode))]
	public bool IsAnimationOverridden => this.CharacterMode == CharacterModes.AnimLock;

	/// <summary>
	/// Refresh the actor to force the game to load any changed values for appearance.
	/// </summary>
	public void Refresh()
	{
		this.refreshQueue.Invoke();
	}

	public override void Tick()
	{
		this.History.Tick();

		// Since writing is immadiate from poperties, we don't want to tick (read) anything
		// during a refresh.
		if (this.IsRefreshing || this.PendingRefresh)
			return;

		base.Tick();
	}

	/// <summary>
	/// Refresh the actor to force the game to load any changed values for appearance.
	/// </summary>
	public async Task RefreshAsync()
	{
		if (this.IsRefreshing)
			return;

		//if (!this.CanRefresh)
		//	return;

		if (this.Address == IntPtr.Zero)
			return;

		try
		{
			//Log.Information($"Attempting actor refresh for actor address: {this.Address}");

			this.IsRefreshing = true;

			if(await ActorService.Instance.RefreshActor(this))
			{
				//Log.Information($"Completed actor refresh for actor address: {this.Address}");
			}
			else
			{
				//Log.Information($"Could not refresh actor: {this.Address}");
			}
		}
		catch (Exception ex)
		{
			//Log.Error(ex, $"Error refreshing actor: {this.Address}");
		}
		finally
		{
			this.IsRefreshing = false;
			this.IsWeaponDirty = false;
			this.WriteDelayedBinds();
		}

		//this.RaisePropertyChanged(nameof(this.IsHuman));
		await Task.Delay(150);
		////this.RaisePropertyChanged(nameof(this.IsHuman));
	}

	public async Task BackupAsync()
	{
		while (this.IsRefreshing)
			await Task.Delay(10);

		this.Pinned?.CreateCharacterBackup(PinnedActor.BackupModes.Gpose);
	}

	public void RaiseRefreshChanged()
	{
		this.RaisePropertyChanged(nameof(this.CanRefresh));
	}

	protected override void HandlePropertyChanged(PropertyChange change)
	{
		this.History.Record(change);

		if (change.Origin != PropertyChange.Origins.Game)
			this.backupQueue.Invoke();

		if (!this.AutomaticRefreshEnabled)
			return;

		if (this.IsRefreshing)
		{
			// dont refresh because of a refresh!
			if (change.TerminalPropertyName == nameof(this.ObjectKind) || change.TerminalPropertyName == nameof(this.RenderMode))
			{
				return;
			}
		}

		if (change.OriginBind.Flags.HasFlag(BindFlags.ActorRefresh) && change.Origin != PropertyChange.Origins.Game)
		{
			if (change.OriginBind.Flags.HasFlag(BindFlags.WeaponRefresh))
				this.IsWeaponDirty = true;

			this.Refresh();
		}
	}

	protected override bool CanWrite(BindInfo bind)
	{
		if (this.IsRefreshing)
		{
			if (bind.Memory != this)
			{
				//Log.Warning("Skipping Bind " + bind);

				// Do not allow writing of any properties form sub-memory while we are refreshing
				return false;
			}
			else
			{
				// do not allow writing of any properties except the ones needed for refresh during a refresh.
				return bind.Name == nameof(this.ObjectKind) || bind.Name == nameof(this.RenderMode);
			}
		}

		return base.CanWrite(bind);
	}
}
