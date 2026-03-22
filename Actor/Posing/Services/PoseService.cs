// © Anamnesis.
// Licensed under the MIT license.

namespace Anamnesis.Actor;

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Anamnesis.Core.Memory;
using Anamnesis.Files;
using Anamnesis.Memory;
using Anamnesis.Services;
public class PoseService : ServiceBase<PoseService>
{
	private NopHookViewModel? freezeRot1;
	private NopHookViewModel? freezeRot2;
	private NopHookViewModel? freezeRot3;
	private NopHookViewModel? freezeScale1;
	private NopHookViewModel? freezePosition;
	private NopHookViewModel? freezePosition2;
	private NopHookViewModel? freeseScale2;
	private NopHookViewModel? freezePhysics1;
	private NopHookViewModel? freezePhysics2;
	private NopHookViewModel? freezePhysics3;
	private NopHookViewModel? freezeWorldPosition;
	private NopHookViewModel? freezeWorldRotation;
	private NopHookViewModel? freezeGposeTargetPosition;

	private bool isEnabled;

	public delegate void PoseEvent(bool value);

	public static event PoseEvent? EnabledChanged;
	public static event PoseEvent? FreezeWorldPositionsEnabledChanged;

	public static string? SelectedBoneName { get; set; }

	public bool IsEnabled
	{
		get
		{
			return this.isEnabled;
		}

		set
		{
			if (this.IsEnabled == value)
				return;

			this.SetEnabled(value);
		}
	}

	public bool FreezePhysics
	{
		get
		{
			return this.freezePhysics1?.Enabled ?? false;
		}
		set
		{
			this.freezePhysics1?.SetEnabled(value);
			this.freezePhysics2?.SetEnabled(value);
		}
	}

	public bool FreezePositions
	{
		get
		{
			return this.freezePosition?.Enabled ?? false;
		}
		set
		{
			this.freezePosition?.SetEnabled(value);
			this.freezePosition2?.SetEnabled(value);
		}
	}

	public bool FreezeScale
	{
		get
		{
			return this.freezeScale1?.Enabled ?? false;
		}
		set
		{
			this.freezeScale1?.SetEnabled(value);
			this.freezePhysics3?.SetEnabled(value);
			this.freeseScale2?.SetEnabled(value);
		}
	}

	public bool FreezeRotation
	{
		get
		{
			return this.freezeRot1?.Enabled ?? false;
		}
		set
		{
			this.freezeRot1?.SetEnabled(value);
			this.freezeRot2?.SetEnabled(value);
			this.freezeRot3?.SetEnabled(value);
		}
	}

	public bool WorldPositionNotFrozen => !this.FreezeWorldPosition;

	public bool FreezeWorldPosition
	{
		get
		{
			return this.freezeWorldPosition?.Enabled ?? false;
		}
		set
		{
			this.freezeWorldPosition?.SetEnabled(value);
			this.freezeWorldRotation?.SetEnabled(value);
			this.freezeGposeTargetPosition?.SetEnabled(value);
			this.RaisePropertyChanged(nameof(PoseService.FreezeWorldPosition));
			this.RaisePropertyChanged(nameof(PoseService.WorldPositionNotFrozen));
			FreezeWorldPositionsEnabledChanged?.Invoke(this.IsEnabled);
		}
	}

	public bool EnableParenting { get; set; } = true;

	public bool CanEdit { get; set; }

	public override async Task Initialize()
	{
		await base.Initialize();

		this.freezePosition = AddressService.SkeletonFreezePosition != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezePosition, 5) : null;
		this.freezePosition2 = AddressService.SkeletonFreezePosition2 != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezePosition2, 5) : null;
		this.freezeRot1 = AddressService.SkeletonFreezeRotation != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezeRotation, 6) : null;
		this.freezeRot2 = AddressService.SkeletonFreezeRotation2 != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezeRotation2, 6) : null;
		this.freezeRot3 = AddressService.SkeletonFreezeRotation3 != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezeRotation3, 4) : null;
		this.freezeScale1 = AddressService.SkeletonFreezeScale != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezeScale, 6) : null;
		this.freeseScale2 = AddressService.SkeletonFreezeScale2 != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezeScale2, 6) : null;
		this.freezePhysics1 = AddressService.SkeletonFreezePhysics != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezePhysics, 4) : null;
		this.freezePhysics2 = AddressService.SkeletonFreezePhysics2 != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezePhysics2, 3) : null;
		this.freezePhysics3 = AddressService.SkeletonFreezePhysics3 != IntPtr.Zero ? new NopHookViewModel(AddressService.SkeletonFreezePhysics3, 4) : null;
		this.freezeWorldPosition = AddressService.WorldPositionFreeze != IntPtr.Zero ? new NopHookViewModel(AddressService.WorldPositionFreeze, 16) : null;
		this.freezeWorldRotation = AddressService.WorldRotationFreeze != IntPtr.Zero ? new NopHookViewModel(AddressService.WorldRotationFreeze, 4) : null;
		this.freezeGposeTargetPosition = AddressService.GPoseCameraTargetPositionFreeze != IntPtr.Zero ? new NopHookViewModel(AddressService.GPoseCameraTargetPositionFreeze, 5) : null;

		GposeService.GposeStateChanged += this.OnGposeStateChanged;

		//_ = Task.Run(ExtractStandardPoses);
	}

	public override async Task Shutdown()
	{
		await base.Shutdown();
		this.SetEnabled(false);
		this.FreezeWorldPosition = false;
	}

	public void SetEnabled(bool enabled)
	{
		// Don't try to enable posing unless we are in gpose
		if (enabled && !GposeService.Instance.IsGpose)
			throw new Exception("Attempt to enable posing outside of gpose");

		if (this.isEnabled == enabled)
			return;

		this.isEnabled = enabled;
		this.FreezePhysics = enabled;
		this.FreezeRotation = enabled;
		this.FreezePositions = false;
		this.FreezeScale = false;
		this.EnableParenting = true;

		/*if (enabled)
		{
			this.FreezeWorldPosition = true;
			AnimationService.Instance.PausePinnedActors();
		}*/

		EnabledChanged?.Invoke(enabled);

		this.RaisePropertyChanged(nameof(this.IsEnabled));
	}

	private static async Task ExtractStandardPoses()
	{
		try
		{
			DirectoryInfo standardPoseDir = FileService.StandardPoseDirectory.Directory;
			string verFile = standardPoseDir.FullName + "\\ver.txt";

			if (standardPoseDir.Exists)
			{
				if (File.Exists(verFile))
				{
					try
					{
						string verText = await File.ReadAllTextAsync(verFile);
						DateTime standardPoseVersion = DateTime.Parse(verText, CultureInfo.InvariantCulture);

						//if (standardPoseVersion == VersionInfo.Date)
						//{
						//	//Log.Information($"Standard pose library up to date");
						//	return;
						//}
					}
					catch (Exception ex)
					{
						//Log.Warning(ex, "Failed to read standard pose library version file");
					}
				}

				standardPoseDir.Delete(true);
			}

			standardPoseDir.Create();
			//await File.WriteAllTextAsync(verFile, VersionInfo.Date.ToString(CultureInfo.InvariantCulture));

			string[] poses = EmbeddedFileUtility.GetAllFilesInDirectory("\\Data\\StandardPoses\\");
			foreach (string posePath in poses)
			{
				string destPath = posePath;
				destPath = destPath.Replace('.', '\\');
				destPath = destPath.Replace('_', ' ');
				destPath = destPath.Replace("Data\\StandardPoses\\", string.Empty);

				// restore file extensions
				destPath = destPath.Replace("\\pose", ".pose");
				destPath = destPath.Replace("\\txt", ".txt");

				destPath = standardPoseDir.FullName + destPath;

				string? destDir = Path.GetDirectoryName(destPath);

				if (destDir == null)
					throw new Exception($"Failed to get directory name from path: {destPath}");

				if (!Directory.Exists(destDir))
					Directory.CreateDirectory(destDir);

				using Stream contents = EmbeddedFileUtility.Load(posePath);
				using FileStream fileStream = new FileStream(destPath, FileMode.Create);
				await contents.CopyToAsync(fileStream);
			}

			//Log.Information($"Extracted standard pose library");
		}
		catch (Exception ex)
		{
			//Log.Error(ex, "Failed to extract standard pose library");
		}
	}

	private void OnGposeStateChanged(bool isGPose)
	{
		if (!isGPose)
		{
			this.SetEnabled(false);
			this.FreezeWorldPosition = false;
		}
	}
}
