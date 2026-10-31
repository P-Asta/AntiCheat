// Execute the production adapter/tracker with game boundary doubles. No RPC or kick API exists here.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type type, string method) { }
    }
    public sealed class HarmonyPostfix : Attribute { }
    public sealed class HarmonyWrapSafe : Attribute { }
    public sealed class HarmonyTargetMethods : Attribute { }
    public sealed class HarmonyPrepare : Attribute { }
    public static class AccessTools
    {
        public static MethodInfo DeclaredMethod(Type type, string name) => type.GetMethod(name,
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
    }
}
namespace UnityEngine
{
    public class Object { }
    public class Transform : Object { public Transform parent; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude => x * x + y * y + z * z;
    }
    public static class Time
    {
        public static float time, deltaTime = .25f, timeScale = 1f, realtimeSinceStartup;
    }
    public class Animator
    {
        public bool Sprinting, Transition, isActiveAndEnabled = true;
        public bool IsInTransition(int layer) => Transition;
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
    private void EndOfGame() { }
    private void OnDestroy() { }
    private void OnPlayerDC(int playerObjectNumber, ulong clientId) { }
}
namespace GameNetcodeStuff
{
    public class PlayerControllerB
    {
        public bool IsSpawned = true, IsOwner, isHostPlayerObject, isPlayerControlled = true, isPlayerDead;
        public ulong actualClientId = 7, OwnerClientId = 7, playerClientId;
        public int health = 100;
        public bool criticallyInjured, isExhausted;
        public float sprintTime = 5, movementSpeed = 1, sprintMeter = .75f, carryWeight = 1;
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
        private void LateUpdate() { }
        private void DamageOnOtherClients(int damageNumber, int newHealthAmount) { }
        private void TeleportPlayer(UnityEngine.Vector3 position, bool withRotation = false, float rot = 0, bool allowInteractTrigger = false, bool enableController = true) { }
        private void PlayerJumpedClientRpc() { }
        private void OnDestroy() { }
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
    public static readonly List<GameNetcodeStuff.PlayerControllerB> Notifications = new List<GameNetcodeStuff.PlayerControllerB>();
    public static TestLogger Log = new TestLogger();
    public static void ShowStaminaSuspicion(GameNetcodeStuff.PlayerControllerB player) => Notifications.Add(player);
}
public class TestLogger { public void LogWarning(object message) { } }
