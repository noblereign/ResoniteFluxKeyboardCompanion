using System.Runtime.CompilerServices;

using FrooxEngine;
using FrooxEngine.CommonAvatar;
using FrooxEngine.ProtoFlux;

using HarmonyLib;

using ResoniteModLoader;

namespace FluxKeyboardCompanion;

public class FluxKeyboardCompanion : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0"; //Changing the version here updates it in all locations needed
	public override string Name => "FluxKeyboardCompanion";
	public override string Author => "Noble";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/noblereign/ResoniteFluxKeyboardCompanion/";

	const string harmonyId = "dog.glacier.FluxKeyboardCompanion";

	public static ModConfiguration? Config;

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<bool> Enabled = new("Enabled", "Enables the mod", () => true);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> ApiKey = new("API Key", "Flux Polymath API Key", () => "", true);

	private static PolymathClient? wsClient;

	public override void OnEngineInit() {
		Config = GetConfiguration()!;
		Config!.Save(true);

		Engine.Current.OnReady += InitPolymath;

		Setup();

		Engine.Current.OnShutdown += OnEngineShutdown;
	}

	private static void SendInitialContext() {
		if (Engine.Current.WorldManager != null && Engine.Current.WorldManager.FocusedWorld != null) {
			FluxKeyboardCompanion.UpdateContext(Engine.Current.WorldManager.FocusedWorld.LocalUser.GetInteractionHandler(Engine.Current.WorldManager.FocusedWorld.LocalUser.primaryHand));
		} else {
			FluxKeyboardCompanion.UpdateContext(Userspace.UserspaceWorld.LocalUser.GetInteractionHandler(Userspace.UserspaceWorld.LocalUser.primaryHand));
		}
	}

	private static void InitPolymath() { // TODO: On first run, auto install .flux file
		if (Config!.GetValue(Enabled)) {
			Msg("Initializing Flux Polymath client...");
			wsClient = new PolymathClient();
			Task.Run(async () => {
				await wsClient.ConnectAsync();
				SendInitialContext();
			});
		}
	}

	private static void OnEngineShutdown() {
		Engine.Current.RegisterShutdownTask(Task.Run(async () => {
			Msg("Shutting down Flux Polymath client...");
			wsClient?.Stop();
			await Task.Delay(250);
		}));
	}

	private static void OnConfigurationChanged(ConfigurationChangedEvent @event) {
		if (@event.Key == Enabled) {
			if (Config!.GetValue(Enabled)) {
				Msg("Initializing Flux Polymath client...");
				wsClient = new PolymathClient();
				Task.Run(async () => {
					await wsClient.ConnectAsync();
					SendInitialContext();
				});
			} else {
				Msg("Shutting down Flux Polymath client...");
				wsClient?.Stop();
			}
		}
	}


	static void Setup() {
		Harmony harmony = new Harmony(harmonyId);
		harmony.PatchAll();
	}

	private static bool HandlerValid(InteractionHandler? handler) {
		if (!Config!.GetValue(Enabled)) return false;
		if (wsClient == null) return false;
		if (handler == null) return false;
		if (!handler.IsOwnedByLocalUser) return false;
		if (handler.World != Engine.Current.WorldManager.FocusedWorld) return false;
		if (handler.Side.Value != handler.LocalUser.primaryHand.Value) return false;

		return true;
	}

	public static void UpdateContext(InteractionHandler handler) {
		if (wsClient == null) {
			Warn("Websocket not connected, can't set tool context!");
			return;
		}

		if (Userspace.HasFocus || handler.LocalUser.HasActiveFocus()) {
			wsClient.ChangeContext("Input Focused");
			return;
		}

		if (handler.IsHoldingObjects) {
			wsClient.ChangeContext("Grabbing");
			return;
		}

		ITool tool = handler.ActiveTool;
		if (tool is ToolMultiplexer multiplexer) {
			tool = multiplexer.ActiveTool;
		}

		switch (tool) {
			case DevTool:
				wsClient.ChangeContext("Dev Tool");
				break;
			case ProtoFluxTool:
				wsClient.ChangeContext("ProtoFlux Tool");
				break;
			case MaterialTool:
				wsClient.ChangeContext("Material Tool");
				break;
			case ShapeTool:
				wsClient.ChangeContext("Shape Tool");
				break;
			case LightTool:
				wsClient.ChangeContext("Light Tool");
				break;
			case GrabbableSetterTool:
				wsClient.ChangeContext("Grabbable Setter Tool");
				break;
			case CharacterColliderSetterTool:
				wsClient.ChangeContext("Character Collider Setter Tool");
				break;
			case MicrophoneTool:
				wsClient.ChangeContext("Microphone Tool");
				break;
			case GlueTool:
				wsClient.ChangeContext("Glue Tool");
				break;
			case ComponentCloneTool:
				wsClient.ChangeContext("Component Clone Tool");
				break;


			case CameraControlTool:
				wsClient.ChangeContext("Camera Control Tool");
				break;
			case ColorTool:
				wsClient.ChangeContext("Color Tool");
				break;
			case ControllerDiagnosticTool:
				wsClient.ChangeContext("Controller Diagnostic Tool");
				break;
			case DuplicatorTool:
				wsClient.ChangeContext("Duplicator Tool");
				break;
			case GaussianSplatTool:
				wsClient.ChangeContext("Gaussian Splat Tool");
				break;
			case GeometryLineBrushTool:
				wsClient.ChangeContext("Geometry Line Brush Tool");
				break;
			case InspectorTool:
				wsClient.ChangeContext("Inspector Tool");
				break;
			case InteractiveCameraObjectExcludeTool:
				wsClient.ChangeContext("Camera Object Hider Tool");
				break;
			case LabelerTool:
				wsClient.ChangeContext("Labeler Tool");
				break;
			case MeshPlaneBrushTool:
				wsClient.ChangeContext("Mesh Plane Brush Tool");
				break;
			case MeshTool:
				wsClient.ChangeContext("Mesh Tool");
				break;
			case MeshVisibilityToggleTool:
				wsClient.ChangeContext("Mesh Visibility Tool");
				break;
			case MeterTool:
				wsClient.ChangeContext("Meter Tool");
				break;
			case ObjectSlicerTool:
				wsClient.ChangeContext("Object Slicer Tool");
				break;
			case ParticleSpray:
				wsClient.ChangeContext("Particle Spray Tool");
				break;
			case BrushTool:
				wsClient.ChangeContext("Brush Tool");
				break;
			case RigMeshTransferTool:
				wsClient.ChangeContext("Rig Mesh Transfer Tool");
				break;
			case RigTool:
				wsClient.ChangeContext("Rig Tool");
				break;
			case SpotLightTool:
				wsClient.ChangeContext("Spot Light Tool");
				break;
			case SunMover:
				wsClient.ChangeContext("Sun Mover Tool");
				break;
			case WigglerApplier:
				wsClient.ChangeContext("Wiggler Applier Tool");
				break;
			case RawDataTool:
				wsClient.ChangeContext("Raw Data Tool");
				break;
			default:
				ScreenController screen = handler.World.GetScreen();
				if (screen != null) {
					if (screen.ActiveTargetting.Target == screen._freeformCamera.Target) {
						wsClient.ChangeContext("Freecam");
						break;
					}
				}
				IAvatarAnchor currentAnchor = handler.LocalUser.GetCurrentAnchor();
				if (currentAnchor != null) {
					if (currentAnchor.Slot.GetComponent<AvatarAnchorLocomotionRelease>() != null) {
						wsClient.ChangeContext("In Anchor");
						break;
					}
				}
				wsClient.ChangeContext("General");
				break;
		}
	}


	[HarmonyPatch(typeof(InteractionHandler))]
	class InteractionHandler_Patch {
		[HarmonyPatch("StartGrab")]
		[HarmonyPostfix]
		static void StartGrabPostfix(InteractionHandler __instance) {
			if (!HandlerValid(__instance)) return;

			UpdateContext(__instance);
		}

		[HarmonyPatch("EndGrab")]
		[HarmonyPostfix]
		static void EndGrabPostfix(InteractionHandler __instance) {
			if (!HandlerValid(__instance)) return;

			UpdateContext(__instance);
		}

		[HarmonyPatch("Equip")]
		[HarmonyPostfix]
		static void EquipPostfix(InteractionHandler __instance, ITool tool, bool lockEquip, ref bool __result) {
			if (!__result) return;
			if (!HandlerValid(__instance)) return;

			UpdateContext(__instance);
		}

		[HarmonyPatch("ToolDequipped")]
		[HarmonyPostfix]
		static void ToolDequippedPostfix(InteractionHandler __instance, ITool tooltip, bool popOff) {
			if (!HandlerValid(__instance)) return;

			// wait an update in case the user switched tools instantly, which can cause it to swap back to general context
			__instance.World.RunInUpdates(1, () => { UpdateContext(__instance); });
		}
	}

	[HarmonyPatch(typeof(ToolMultiplexer))]
	class ToolMultiplexer_Patch {
		[HarmonyPatch("RegisterActiveTool")]
		[HarmonyPostfix]
		static void RegisterActiveToolPostfix(ToolMultiplexer __instance) {
			if (!__instance.IsUnderLocalUser) return;
			InteractionHandler handler = __instance.LocalUser.GetInteractionHandler(__instance.LocalUser.Primaryhand);
			if (!HandlerValid(handler)) return;

			UpdateContext(handler);
		}
	}

	[HarmonyPatch(typeof(ScreenController))]
	class ScreenController_Patch {
		[HarmonyPatch("ActivateFreeformCamera")]
		[HarmonyPostfix]
		static void ActivateFreeformCameraPostfix(ScreenController __instance) {
			if (!__instance.IsUnderLocalUser) return;
			InteractionHandler handler = __instance.LocalUser.GetInteractionHandler(__instance.LocalUser.Primaryhand);
			if (!HandlerValid(handler)) return;

			UpdateContext(handler);
		}

		private static readonly ConditionalWeakTable<ScreenController, IViewTargettingController> _lastCameras = [];

		[HarmonyPatch("BeforeInputUpdate")]
		[HarmonyPostfix]
		static void BeforeInputUpdatePostfix(ScreenController __instance, IViewTargettingController ____activatedTargetting, SyncRef<FreeformTargettingController> ____freeformCamera) {
			if (!__instance.IsUnderLocalUser) return;
			InteractionHandler handler = __instance.LocalUser.GetInteractionHandler(__instance.LocalUser.Primaryhand);
			if (!HandlerValid(handler)) return;
			if (____freeformCamera?.Target == null) return;
			_lastCameras.TryGetValue(__instance, out IViewTargettingController? lastCamera);
			if (lastCamera == ____freeformCamera.Target && ____activatedTargetting != ____freeformCamera.Target) {
				UpdateContext(handler);
			}
			_lastCameras.AddOrUpdate(__instance, ____activatedTargetting);
		}
	}

	[HarmonyPatch(typeof(AvatarManager))]
	class AvatarManager_Patch {
		[HarmonyPatch("SetAnchor")]
		[HarmonyPostfix]
		static void SetAnchorPostfix(AvatarManager __instance) {
			if (!__instance.IsUnderLocalUser) return;
			InteractionHandler handler = __instance.LocalUser.GetInteractionHandler(__instance.LocalUser.Primaryhand);
			if (!HandlerValid(handler)) return;

			UpdateContext(handler);
		}

		[HarmonyPatch("ClearAnchor")]
		[HarmonyPostfix]
		static void ClearAnchorPostfix(AvatarManager __instance) {
			if (!__instance.IsUnderLocalUser) return;
			InteractionHandler handler = __instance.LocalUser.GetInteractionHandler(__instance.LocalUser.Primaryhand);
			if (!HandlerValid(handler)) return;

			__instance.World.RunInUpdates(1, () => { UpdateContext(handler); });
		}
	}

	[HarmonyPatch(typeof(FocusManager))]
	class FocusManager_Patch {
		[HarmonyPatch("FocusChanged")]
		[HarmonyPostfix]
		static void FocusChangedPostfix(FocusManager __instance, SyncRef<IFocusable> reference) {
			if (!(__instance.User.IsLocalUser || Userspace.thisobj._userspaceFocusManager == __instance)) return;
			__instance.RunSynchronously(() => {
				InteractionHandler handler = __instance.LocalUser.GetInteractionHandler(__instance.LocalUser.Primaryhand);
				if (!HandlerValid(handler)) return;

				UpdateContext(handler);
			});
		}
	}
}
