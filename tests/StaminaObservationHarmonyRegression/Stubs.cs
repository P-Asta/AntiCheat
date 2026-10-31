// Game boundary doubles; production patches are installed and executed by real HarmonyX.
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    public class Object { }
    public class Transform : Object { public Transform parent; }
    public struct Vector3
    {
        public float x, y, z;
        public float sqrMagnitude => x * x + y * y + z * z;
    }
    public static class Time { public static float time, deltaTime = .25f, timeScale = 1; }
    public class Animator
    {
        public bool Sprinting, isActiveAndEnabled = true;
        public bool IsInTransition(int layer) => false;
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layer) => new AnimatorStateInfo { Sprinting = Sprinting };
    }
    public struct AnimatorStateInfo
    {
        public bool Sprinting;
        public bool IsTag(string tag) => tag == "Sprinting" && Sprinting;
    }
    public class AudioSource { public bool isPlaying; }
}
namespace Unity.Netcode
{
    public class NetworkManager
    {
        public static NetworkManager Singleton = new NetworkManager();
        public bool IsHost = true, IsListening = true;
    }
}
public class SelectableLevel { }
public class GrabbableObject { }
public class TetraChemicalItem : GrabbableObject { }
public class StartOfRound
{
    public static StartOfRound Instance;
    public bool IsHost = true, shipHasLanded = true, shipIsLeaving, inShipPhase, newGameIsLoading, suckingPlayersOutOfShip;
    public float timeSinceRoundStarted = 30;
    public SelectableLevel currentLevel = new SelectableLevel();
    public UnityEngine.Transform playersContainer = new UnityEngine.Transform();
    public Dictionary<ulong, int> ClientPlayerList = new Dictionary<ulong, int>();
    public GameNetcodeStuff.PlayerControllerB[] allPlayerScripts;
    public int OriginalEventCalls;
#if STAMINA_EVENTS_ALL || STAMINA_EVENTS_MIXED
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void OnPlayerDC(int playerObjectNumber, ulong clientId) { OriginalEventCalls++; }
#endif
#if STAMINA_EVENTS_ALL
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EndOfGame() { OriginalEventCalls++; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void OnDestroy() { OriginalEventCalls++; }
#endif
}
namespace GameNetcodeStuff
{
    public class PlayerControllerB
    {
        public bool IsSpawned = true, IsOwner, isHostPlayerObject, isPlayerControlled = true, isPlayerDead;
        public ulong actualClientId = 7, OwnerClientId = 7, playerClientId;
        public int health = 100;
        public bool criticallyInjured, isExhausted;
        public float sprintTime = 5, movementSpeed = 1;
        public float timeSinceTakingDamage, timeSincePlayerMoving, drunkness, drunknessInertia, poison, slipperyFloor, slimeSlipAudioVolumeSync;
        public bool increasingDrunknessThisFrame;
        public bool isInElevator, isInHangarShipRoom, inVehicleAnimation, isClimbingLadder, inSpecialInteractAnimation;
        public bool enteringSpecialAnimation, disableSyncInAnimation, teleportingThisFrame, teleportedLastFrame;
        public bool jetpackControls, disablingJetpackControls, isPlayerSliding, isUnderwater, isSinking;
        public UnityEngine.Object inAnimationWithEnemy;
        public UnityEngine.Transform transform = new UnityEngine.Transform(), physicsParent, overridePhysicsParent;
        public UnityEngine.Vector3 serverPlayerPosition, externalForces, externalForceAutoFade;
        public UnityEngine.Animator playerBodyAnimator = new UnityEngine.Animator();
        public UnityEngine.AudioSource slimeSlipAudio = new UnityEngine.AudioSource();
        public GrabbableObject currentlyHeldObjectServer;
        public int OriginalEventCalls, OriginalLateUpdateCalls;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LateUpdate() { OriginalLateUpdateCalls++; }
#if STAMINA_EVENTS_ALL || STAMINA_EVENTS_MIXED
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DamageOnOtherClients(int damageNumber, int newHealthAmount) { OriginalEventCalls++; }
#endif
#if STAMINA_EVENTS_ALL
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TeleportPlayer(UnityEngine.Vector3 position, bool withRotation = false, float rot = 0, bool allowInteractTrigger = false, bool enableController = true) { OriginalEventCalls++; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PlayerJumpedClientRpc() { OriginalEventCalls++; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnDestroy() { OriginalEventCalls++; }
#endif
    }
}
namespace AntiCheat
{
    public class ConfigValue<T> { public T Value; }
    public static class PluginConfig
    {
        public static ConfigValue<bool> StaminaObservation = new ConfigValue<bool> { Value = true };
    }
}
public static class AntiCheatPlugin
{
    public static int Notifications;
    public static void ShowStaminaSuspicion(GameNetcodeStuff.PlayerControllerB player) => Notifications++;
}
