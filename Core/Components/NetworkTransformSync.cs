using System;
using UnityEngine;
using Omni.Inspector;
using Omni.Shared;

namespace Omni.Core.Components
{
    enum TransformMovementState
    {
        Idle,
        StartedMoving,
        Moving
    }

    public enum AuthorityMode
    {
        Owner, Server
    }

    public enum UpdateMode
    {
        Update,
        FixedUpdate
    }

    public class NetworkTransformState
    {
        private const byte k_PositionMask = 1;
        private const byte k_RotationMask = 2;
        private const byte k_ScaleMask = 4;
        private const byte k_VelocityMask = 8;
        private const byte k_AngularVelocityMask = 16;
        private const byte k_AtPositionalRestMask = 64;
        private const byte k_AtRotationalRestMask = 128;

        internal NetworkTransformSync networkTransform;

        public Vector3 m_Position;
        public Quaternion m_Rotation;
        public Vector3 m_Scale;
        public Vector3 m_Velocity, m_AngularVelocity;
        public Vector3 m_lastRotationVector;

        public float m_OwnerTimestamp;
        public float m_ReceivedTimestamp;

        public bool isTeleport;
        public bool atPositionalRest, atRotationalRest;

        public bool _IsRelayPosition, _IsRelayRotation;
        public bool _IsRelayVelocity, _IsRelayAngularVelocity;
        public bool _IsRelayScale;

        public NetworkTransformState() { }
        public NetworkTransformState CopyFrom(NetworkTransformState state)
        {
            m_OwnerTimestamp = state.m_OwnerTimestamp;
            m_Position = state.m_Position;
            m_Rotation = state.m_Rotation;
            m_Scale = state.m_Scale;
            m_Velocity = state.m_Velocity;
            m_AngularVelocity = state.m_AngularVelocity;
            m_ReceivedTimestamp = state.m_ReceivedTimestamp;
            return this;
        }

        public static NetworkTransformState Lerp(NetworkTransformState target, NetworkTransformState start, NetworkTransformState end, float t)
        {
            target.m_Position = Vector3.Lerp(start.m_Position, end.m_Position, t);
            target.m_Rotation = Quaternion.Lerp(start.m_Rotation, end.m_Rotation, t);
            target.m_Scale = Vector3.Lerp(start.m_Scale, end.m_Scale, t);
            target.m_Velocity = Vector3.Lerp(start.m_Velocity, end.m_Velocity, t);
            target.m_AngularVelocity = Vector3.Lerp(start.m_AngularVelocity, end.m_AngularVelocity, t);
            target.m_OwnerTimestamp = Mathf.Lerp(start.m_OwnerTimestamp, end.m_OwnerTimestamp, t);
            return target;
        }

        public void Reset()
        {
            m_OwnerTimestamp = 0;
            m_Position = Vector3.zero;
            m_Rotation = Quaternion.identity;
            m_Scale = Vector3.zero;
            m_Velocity = Vector3.zero;
            m_AngularVelocity = Vector3.zero;
            atPositionalRest = false;
            atRotationalRest = false;
            isTeleport = false;
            m_ReceivedTimestamp = 0;
        }

        public void CaptureFromSync(NetworkTransformSync controller)
        {
            networkTransform = controller;
            m_OwnerTimestamp = controller.LocalTime;
            m_Position = controller.GetPosition();
            m_Rotation = controller.GetRotation();
            m_Scale = controller.GetScale();

            if (controller.hasRigidbody)
            {
#if UNITY_6000_0_OR_NEWER
                m_Velocity = controller.rb.linearVelocity;
#else
                m_Velocity = controller.rb.velocity;
#endif
                m_AngularVelocity = controller.rb.angularVelocity * Mathf.Rad2Deg;
            }
            else if (controller.hasRigidbody2D)
            {
#if UNITY_6000_0_OR_NEWER
                m_Velocity = controller.rb2D.linearVelocity;
#else
                m_Velocity = controller.rb2D.velocity;
#endif
                m_AngularVelocity.x = 0;
                m_AngularVelocity.y = 0;
                m_AngularVelocity.z = controller.rb2D.angularVelocity;
            }
            else
            {
                m_Velocity = Vector3.zero;
                m_AngularVelocity = Vector3.zero;
            }
        }

        internal void Serialize(DataBuffer writer)
        {
            bool isSendPosition, isSendRotation, isSendScale, isSendVelocity;
            bool isSendAngularVelocity, isSendAtPositionalRestTag, isSendAtRotationalRestTag;

            if (networkTransform.IsServer && !networkTransform.HasControl)
            {
                isSendPosition = _IsRelayPosition;
                isSendRotation = _IsRelayRotation;
                isSendScale = _IsRelayScale;
                isSendVelocity = _IsRelayVelocity;
                isSendAngularVelocity = _IsRelayAngularVelocity;
                isSendAtPositionalRestTag = atPositionalRest;
                isSendAtRotationalRestTag = atRotationalRest;
            }
            else
            {
                isSendPosition = networkTransform.sendPosition;
                isSendRotation = networkTransform.sendRotation;
                isSendScale = networkTransform.sendScale;
                isSendVelocity = networkTransform.sendVelocity;
                isSendAngularVelocity = networkTransform.sendAngularVelocity;
                isSendAtPositionalRestTag = networkTransform.sendAtPositionalRestMessage;
                isSendAtRotationalRestTag = networkTransform.sendAtRotationalRestMessage;
            }

            if (!networkTransform.IsServer)
            {
                if (isSendPosition) networkTransform.lastPositionWhenStateWasSent = m_Position;
                if (isSendRotation) networkTransform.lastRotationWhenStateWasSent = m_Rotation;
                if (isSendScale) networkTransform.lastScaleWhenStateWasSent = m_Scale;
                if (isSendVelocity) networkTransform.lastVelocityWhenStateWasSent = m_Velocity;
                if (isSendAngularVelocity) networkTransform.lastAngularVelocityWhenStateWasSent = m_AngularVelocity;
            }

            byte mask = GetMask(isSendPosition, isSendRotation, isSendScale, isSendVelocity, isSendAngularVelocity, isSendAtPositionalRestTag, isSendAtRotationalRestTag);
            writer.Write(mask);
            writer.Write(m_OwnerTimestamp);

            if (isSendPosition)
            {
                if (networkTransform.compressPosition)
                {
                    if (networkTransform.IsSyncingXPosition)
                        writer.Write(HalfHelper.Compress(m_Position.x));
                    if (networkTransform.IsSyncingYPosition)
                        writer.Write(HalfHelper.Compress(m_Position.y));
                    if (networkTransform.IsSyncingZPosition)
                        writer.Write(HalfHelper.Compress(m_Position.z));
                }
                else
                {
                    if (networkTransform.IsSyncingXPosition)
                        writer.Write(m_Position.x);
                    if (networkTransform.IsSyncingYPosition)
                        writer.Write(m_Position.y);
                    if (networkTransform.IsSyncingZPosition)
                        writer.Write(m_Position.z);
                }
            }

            if (isSendRotation)
            {
                Vector3 rot = m_Rotation.eulerAngles;
                if (networkTransform.compressRotation)
                {
                    if (networkTransform.IsSyncingXRotation)
                        writer.Write(HalfHelper.Compress(rot.x * Mathf.Deg2Rad));
                    if (networkTransform.IsSyncingYRotation)
                        writer.Write(HalfHelper.Compress(rot.y * Mathf.Deg2Rad));
                    if (networkTransform.IsSyncingZRotation)
                        writer.Write(HalfHelper.Compress(rot.z * Mathf.Deg2Rad));
                }
                else
                {
                    if (networkTransform.IsSyncingXRotation)
                        writer.Write(rot.x);
                    if (networkTransform.IsSyncingYRotation)
                        writer.Write(rot.y);
                    if (networkTransform.IsSyncingZRotation)
                        writer.Write(rot.z);
                }
            }

            if (isSendScale)
            {
                if (networkTransform.compressScale)
                {
                    if (networkTransform.IsSyncingXScale)
                        writer.Write(HalfHelper.Compress(m_Scale.x));

                    if (networkTransform.IsSyncingYScale)
                        writer.Write(HalfHelper.Compress(m_Scale.y));

                    if (networkTransform.IsSyncingZScale)
                        writer.Write(HalfHelper.Compress(m_Scale.z));
                }
                else
                {
                    if (networkTransform.IsSyncingXScale)
                        writer.Write(m_Scale.x);

                    if (networkTransform.IsSyncingYScale)
                        writer.Write(m_Scale.y);

                    if (networkTransform.IsSyncingZScale)
                        writer.Write(m_Scale.z);
                }
            }

            if (isSendVelocity)
            {
                if (networkTransform.compressVelocity)
                {
                    if (networkTransform.IsSyncingXVelocity)
                        writer.Write(HalfHelper.Compress(m_Velocity.x));

                    if (networkTransform.IsSyncingYVelocity)
                        writer.Write(HalfHelper.Compress(m_Velocity.y));

                    if (networkTransform.IsSyncingZVelocity)
                        writer.Write(HalfHelper.Compress(m_Velocity.z));
                }
                else
                {
                    if (networkTransform.IsSyncingXVelocity)
                        writer.Write(m_Velocity.x);

                    if (networkTransform.IsSyncingYVelocity)
                        writer.Write(m_Velocity.y);

                    if (networkTransform.IsSyncingZVelocity)
                        writer.Write(m_Velocity.z);
                }
            }

            if (isSendAngularVelocity)
            {
                if (networkTransform.compressAngularVelocity)
                {
                    if (networkTransform.IsSyncingXAngularVelocity)
                        writer.Write(HalfHelper.Compress(m_AngularVelocity.x * Mathf.Deg2Rad));

                    if (networkTransform.IsSyncingYAngularVelocity)
                        writer.Write(HalfHelper.Compress(m_AngularVelocity.y * Mathf.Deg2Rad));

                    if (networkTransform.IsSyncingZAngularVelocity)
                        writer.Write(HalfHelper.Compress(m_AngularVelocity.z * Mathf.Deg2Rad));
                }
                else
                {
                    if (networkTransform.IsSyncingXAngularVelocity)
                        writer.Write(m_AngularVelocity.x);

                    if (networkTransform.IsSyncingYAngularVelocity)
                        writer.Write(m_AngularVelocity.y);

                    if (networkTransform.IsSyncingZAngularVelocity)
                        writer.Write(m_AngularVelocity.z);
                }
            }
        }

        public static NetworkTransformState Deserialize(DataBuffer reader, NetworkTransformSync identity)
        {
            var nState = new NetworkTransformState();

            byte mask = reader.Read<byte>();
            bool syncPosition = (mask & k_PositionMask) == k_PositionMask;
            bool syncRotation = (mask & k_RotationMask) == k_RotationMask;
            bool syncScale = (mask & k_ScaleMask) == k_ScaleMask;
            bool syncVelocity = (mask & k_VelocityMask) == k_VelocityMask;
            bool syncAngularVelocity = (mask & k_AngularVelocityMask) == k_AngularVelocityMask;

            nState.atPositionalRest = (mask & k_AtPositionalRestMask) == k_AtPositionalRestMask;
            nState.atRotationalRest = (mask & k_AtRotationalRestMask) == k_AtRotationalRestMask;

            nState.m_OwnerTimestamp = reader.Read<float>();
            nState.networkTransform = identity;

            var transform = nState.networkTransform;
            nState.m_ReceivedTimestamp = transform.LocalTime;

            if (transform.IsServer && !transform.HasControl)
            {
                nState._IsRelayPosition = syncPosition;
                nState._IsRelayRotation = syncRotation;
                nState._IsRelayScale = syncScale;
                nState._IsRelayVelocity = syncVelocity;
                nState._IsRelayAngularVelocity = syncAngularVelocity;
            }

            if (transform.recStatesCounter < transform.m_SendRate)
                transform.recStatesCounter++;

            if (syncPosition)
            {
                if (transform.compressPosition)
                {
                    if (transform.IsSyncingXPosition)
                        nState.m_Position.x = HalfHelper.Decompress(reader.Read<ushort>());

                    if (transform.IsSyncingYPosition)
                        nState.m_Position.y = HalfHelper.Decompress(reader.Read<ushort>());

                    if (transform.IsSyncingZPosition)
                        nState.m_Position.z = HalfHelper.Decompress(reader.Read<ushort>());
                }
                else
                {
                    if (transform.IsSyncingXPosition)
                        nState.m_Position.x = reader.Read<float>();

                    if (transform.IsSyncingYPosition)
                        nState.m_Position.y = reader.Read<float>();

                    if (transform.IsSyncingZPosition)
                        nState.m_Position.z = reader.Read<float>();
                }
            }
            else
            {
                if (transform.stateCount > 0) nState.m_Position = transform._buffer[0].m_Position;
                else nState.m_Position = transform.GetPosition();
            }

            if (syncRotation)
            {
                nState.m_lastRotationVector = Vector3.zero;
                if (transform.compressRotation)
                {
                    if (transform.IsSyncingXRotation)
                    {
                        nState.m_lastRotationVector.x = HalfHelper.Decompress(reader.Read<ushort>());
                        nState.m_lastRotationVector.x *= Mathf.Rad2Deg;
                    }

                    if (transform.IsSyncingYRotation)
                    {
                        nState.m_lastRotationVector.y = HalfHelper.Decompress(reader.Read<ushort>());
                        nState.m_lastRotationVector.y *= Mathf.Rad2Deg;
                    }

                    if (transform.IsSyncingZRotation)
                    {
                        nState.m_lastRotationVector.z = HalfHelper.Decompress(reader.Read<ushort>());
                        nState.m_lastRotationVector.z *= Mathf.Rad2Deg;
                    }

                    nState.m_Rotation = Quaternion.Euler(nState.m_lastRotationVector);
                }
                else
                {
                    if (transform.IsSyncingXRotation)
                        nState.m_lastRotationVector.x = reader.Read<float>();

                    if (transform.IsSyncingYRotation)
                        nState.m_lastRotationVector.y = reader.Read<float>();

                    if (transform.IsSyncingZRotation)
                        nState.m_lastRotationVector.z = reader.Read<float>();

                    nState.m_Rotation = Quaternion.Euler(nState.m_lastRotationVector);
                }
            }
            else
            {
                if (transform.stateCount > 0) nState.m_Rotation = transform._buffer[0].m_Rotation;
                else nState.m_Rotation = transform.GetRotation();
            }

            if (syncScale)
            {
                if (transform.compressScale)
                {
                    if (transform.IsSyncingXScale)
                        nState.m_Scale.x = HalfHelper.Decompress(reader.Read<ushort>());

                    if (transform.IsSyncingYScale)
                        nState.m_Scale.y = HalfHelper.Decompress(reader.Read<ushort>());

                    if (transform.IsSyncingZScale)
                        nState.m_Scale.z = HalfHelper.Decompress(reader.Read<ushort>());
                }
                else
                {
                    if (transform.IsSyncingXScale)
                        nState.m_Scale.x = reader.Read<float>();

                    if (transform.IsSyncingYScale)
                        nState.m_Scale.y = reader.Read<float>();

                    if (transform.IsSyncingZScale)
                        nState.m_Scale.z = reader.Read<float>();
                }
            }
            else
            {
                if (transform.stateCount > 0) nState.m_Scale = transform._buffer[0].m_Scale;
                else nState.m_Scale = transform.GetScale();
            }

            if (syncVelocity)
            {
                if (transform.compressVelocity)
                {
                    if (transform.IsSyncingXVelocity)
                        nState.m_Velocity.x = HalfHelper.Decompress(reader.Read<ushort>());

                    if (transform.IsSyncingYVelocity)
                        nState.m_Velocity.y = HalfHelper.Decompress(reader.Read<ushort>());

                    if (transform.IsSyncingZVelocity)
                        nState.m_Velocity.z = HalfHelper.Decompress(reader.Read<ushort>());
                }
                else
                {
                    if (transform.IsSyncingXVelocity)
                        nState.m_Velocity.x = reader.Read<float>();

                    if (transform.IsSyncingYVelocity)
                        nState.m_Velocity.y = reader.Read<float>();

                    if (transform.IsSyncingZVelocity)
                        nState.m_Velocity.z = reader.Read<float>();
                }

                transform.latestReceivedVelocity = nState.m_Velocity;
            }
            else nState.m_Velocity = transform.latestReceivedVelocity;

            if (syncAngularVelocity)
            {
                if (transform.compressAngularVelocity)
                {
                    nState.m_lastRotationVector = Vector3.zero;
                    if (transform.IsSyncingXAngularVelocity)
                    {
                        nState.m_lastRotationVector.x = HalfHelper.Decompress(reader.Read<ushort>());
                        nState.m_lastRotationVector.x *= Mathf.Rad2Deg;
                    }

                    if (transform.IsSyncingYAngularVelocity)
                    {
                        nState.m_lastRotationVector.y = HalfHelper.Decompress(reader.Read<ushort>());
                        nState.m_lastRotationVector.y *= Mathf.Rad2Deg;
                    }

                    if (transform.IsSyncingZAngularVelocity)
                    {
                        nState.m_lastRotationVector.z = HalfHelper.Decompress(reader.Read<ushort>());
                        nState.m_lastRotationVector.z *= Mathf.Rad2Deg;
                    }

                    nState.m_AngularVelocity = nState.m_lastRotationVector;
                }
                else
                {
                    if (transform.IsSyncingXAngularVelocity)
                        nState.m_AngularVelocity.x = reader.Read<float>();

                    if (transform.IsSyncingYAngularVelocity)
                        nState.m_AngularVelocity.y = reader.Read<float>();

                    if (transform.IsSyncingZAngularVelocity)
                        nState.m_AngularVelocity.z = reader.Read<float>();
                }

                transform.latestReceivedAngularVelocity = nState.m_AngularVelocity;
            }
            else nState.m_AngularVelocity = transform.latestReceivedAngularVelocity;

            return nState;
        }

        private static byte GetMask(bool isSendPosition, bool isSendRotation, bool isSendScale, bool isSendVelocity, bool isSendAngularVelocity, bool isAtPositionalRest, bool isAtRotationalRest)
        {
            byte mask = 0;
            if (isSendPosition) mask = (byte)(mask | k_PositionMask);
            if (isSendRotation) mask = (byte)(mask | k_RotationMask);
            if (isSendScale) mask = (byte)(mask | k_ScaleMask);
            if (isSendVelocity) mask = (byte)(mask | k_VelocityMask);
            if (isSendAngularVelocity) mask = (byte)(mask | k_AngularVelocityMask);
            if (isAtPositionalRest) mask = (byte)(mask | k_AtPositionalRestMask);
            if (isAtRotationalRest) mask = (byte)(mask | k_AtRotationalRestMask);
            return mask;
        }
    }

    /// <summary>
    /// Specifies the extrapolation behavior for network transform synchronization.
    /// </summary>
    /// <remarks>
    /// Extrapolation is used to predict object movement when new network updates have not yet arrived,
    /// helping to smooth out motion during latency spikes. Extrapolation requires velocity synchronization.
    /// </remarks>
    public enum ExtrapolationMode
    {
        /// <summary>
        /// Disables extrapolation. Objects will only use interpolation based on received data.
        /// </summary>
        None,
        /// <summary>
        /// Enables extrapolation within user-defined time or distance limits.
        /// </summary>
        Limited,
        /// <summary>
        /// Enables unlimited extrapolation with no restrictions.
        /// </summary>
        Unlimited
    }

    public enum TransformSyncAxis
    {
        AllAxes,
        XAndY,
        XAndZ,
        YAndZ,
        XOnly,
        YOnly,
        ZOnly,
        Disabled
    }

    /// <summary>
    /// NetworkTransformSync synchronizes the Transform and Rigidbody state of a GameObject over the network.
    /// Supports smooth interpolation, extrapolation, threshold-based updates, and authority transfer.
    /// </summary>
    /// <remarks>
    /// - Suitable for high-speed action games and precise multiplayer movement.
    /// - Owned objects broadcast state at a fixed send rate or when thresholds are exceeded.
    /// - Remote objects interpolate or extrapolate for smoothness and lag compensation.
    /// - Supports teleportation, authority handover, and optional data compression.
    /// </remarks>
    [DeclareFoldoutGroup("Interpolation & Extrapolation", Expanded = true)]
    [DeclareFoldoutGroup("Lerp Speeds", Expanded = true)]
    [DeclareFoldoutGroup("Others")]
    [DeclareTabGroup("Sync & Compression")]
    [DeclareTabGroup("Thresholds")]
    public partial class NetworkTransformSync : NetworkBehaviour
    {
        private const byte k_StateSyncRpcId = 8;
        private const byte k_StateSyncToOthersRpcId = 9;
        private const int k_PowMaxTime = 12;

        private readonly float minTimePrecision = Mathf.Pow(2, k_PowMaxTime - 24);
        internal float LocalTime { get; private set; }

        /// <summary>
        /// Time (in seconds) the remote object is displayed behind the latest received state, to ensure smooth interpolation.
        /// </summary>
        /// <remarks>
        /// A higher value increases interpolation stability and visual smoothness, at the cost of increased visual latency.
        /// A lower value reduces input-to-visual lag but increases the risk of jitter if network packets arrive late.
        /// Recommended: set to at least the average packet interval (e.g., 1 / NetworkSendRate).
        /// </remarks>
        [Group("Interpolation & Extrapolation")]
        [Range(0.005f, 1f)]
        [LabelWidth(140)]
        [Tooltip("Interpolation Delay (in seconds):\n" +
        "How far behind in time (buffer) remote clients interpolate the object's state. " +
        "A higher value (e.g., 0.1) means more latency tolerance and fewer visual jumps during network hiccups, " +
        "but introduces extra delay. Lower values (e.g., 0.03) make movement more responsive but can lead to visible stuttering " +
        "if network quality drops. Recommended: 1 / SendRate + average network latency. Example: For SendRate=30, latency=0.03s, use 0.06.")]
        public float interpolationDelay = 0.05f;

        /// <summary>
        /// Extrapolation mode used when no recent network updates are available.
        /// </summary>
        /// <remarks>
        /// Extrapolation predicts the object's next state based on the latest known movement, filling in missing data during network lag or packet loss.
        /// - None: Disable extrapolation. The object will freeze if updates are delayed.
        /// - Limited: Extrapolate up to the defined time/distance limits.
        /// - Unlimited: Extrapolate indefinitely while no new data is received.
        /// </remarks>
        [Group("Interpolation & Extrapolation")]
        [LabelWidth(140)]
        [Tooltip("Extrapolation Mode:\n" +
        "Determines how missing network updates are predicted. " +
        "'None' disables extrapolation (remote objects will freeze if packets are lost). " +
        "'Limited' allows extrapolation up to a set time/distance. " +
        "'Unlimited' extrapolates forever (risk of erratic movement; not recommended for action games). " +
        "Generally, 'Limited' gives best balance for most games.")]
        public ExtrapolationMode extrapolationMode = ExtrapolationMode.Limited;

        /// <summary>
        /// Enables limiting extrapolation by a maximum time duration.
        /// </summary>
        /// <remarks>
        /// When enabled, extrapolation will not exceed the maximum time specified in <see cref="extrapolationLimitTime"/>.
        /// This can prevent remote objects from drifting too far into predicted states during network interruptions.
        /// Requires velocity synchronization to function correctly.
        /// If disabled, extrapolation by time will not be enforced.
        /// </remarks>
        [Group("Interpolation & Extrapolation")]
        [HideIf(nameof(extrapolationMode), ExtrapolationMode.None)]
        [DisableIf(nameof(extrapolationMode), ExtrapolationMode.Unlimited)]
        [LabelWidth(170)]
        [Tooltip("Enable to limit extrapolation based on time (seconds). Requires velocity sync.")]
        public bool useExtrapolationLimitTime = true;

        /// <summary>
        /// Maximum time (in seconds) the object is allowed to extrapolate into the future during network delays.
        /// </summary>
        /// <remarks>
        /// Limits how long remote objects will predict their movement when network updates are missing. Higher values can reduce visual stutter during lag spikes but may cause unrealistic movement if too large.
        /// Requires velocity synchronization to function.
        /// </remarks>
        [Group("Interpolation & Extrapolation")]
        [ShowIf(nameof(useExtrapolationLimitTime)), HideIf(nameof(extrapolationMode), ExtrapolationMode.None)]
        [DisableIf(nameof(extrapolationMode), ExtrapolationMode.Unlimited)]
        [LabelWidth(170), Min(0)]
        [Tooltip("Extrapolation Time Limit (seconds):\n" +
        "How many seconds into the future a non-owned object is allowed to be predicted if no new data arrives. " +
        "Shorter limits (e.g., 0.25) prevent the object from drifting too far during connection loss. " +
        "Longer limits (e.g., 1.0) are only recommended for tolerant, slow-moving objects. " +
        "Requires velocity sync. Default: 0.25.")]
        public float extrapolationLimitTime = 0.3f;

        /// <summary>
        /// Enable to limit extrapolation by maximum distance.
        /// </summary>
        /// <remarks>
        /// If enabled, extrapolation will stop when the predicted object position exceeds the defined distance limit from the last known state.
        /// Use together with or instead of the time limit. Requires velocity synchronization.
        /// </remarks>
        [Group("Interpolation & Extrapolation")]
        [HideIf(nameof(extrapolationMode), ExtrapolationMode.None)]
        [DisableIf(nameof(extrapolationMode), ExtrapolationMode.Unlimited)]
        [LabelWidth(170)]
        [Tooltip("Enable to limit extrapolation based on distance (Unity units) instead of just time.")]
        public bool useExtrapolationDistanceLimit = true;

        /// <summary>
        /// Maximum distance the object is allowed to extrapolate from the last known state.
        /// </summary>
        /// <remarks>
        /// Extrapolation will be stopped if the predicted position exceeds this distance, helping prevent erratic or unrealistic movement during network delays.
        /// Requires velocity synchronization to function.
        /// Measured in world units (e.g., meters).
        /// </remarks>
        [Group("Interpolation & Extrapolation")]
        [ShowIf(nameof(useExtrapolationDistanceLimit)), HideIf(nameof(extrapolationMode), ExtrapolationMode.None)]
        [DisableIf(nameof(extrapolationMode), ExtrapolationMode.Unlimited)]
        [LabelWidth(170), Min(0)]
        [Tooltip("Extrapolation Distance Limit (units):\n" +
        "Maximum distance a non-owned object can be predicted into the future during packet loss. " +
        "Keeps fast objects from appearing in wrong locations after lag. " +
        "Lower values (e.g., 2.0) for precise games, higher (10+) for large worlds or slow objects.")]
        public float extrapolationDistanceLimit = 2;

        /// <summary>
        /// Interpolation factor for smoothly updating position towards the target state (0 = no movement, 1 = instant).
        /// </summary>
        /// <remarks>
        /// Lower values result in smoother but slower movement; higher values make objects respond more quickly but can introduce jitter.
        /// Recommended range: 0.8 to 1.0 for most action games.
        /// </remarks>
        [Range(0, 1)]
        [Group("Lerp Speeds")]
        [Tooltip("How quickly to move position towards the target. 0 = never, 1 = instant. Example: 0.85")]
        public float positionLerpFactor = .85f;

        /// <summary>
        /// Interpolation factor for smoothly updating rotation towards the target state (0 = no rotation, 1 = instant).
        /// </summary>
        /// <remarks>
        /// Lower values produce smoother but slower rotation updates; higher values result in faster response but can cause jitter.
        /// Typical range: 0.8 to 1.0 for fast-moving objects.
        /// </remarks>
        [Range(0, 1)]
        [Group("Lerp Speeds")]
        [Tooltip("How quickly to move rotation towards the target. 0 = never, 1 = instant. Example: 0.85")]
        public float rotationLerpFactor = .85f;

        /// <summary>
        /// Interpolation factor for smoothly updating scale towards the target state (0 = no scaling, 1 = instant).
        /// </summary>
        /// <remarks>
        /// Lower values produce smoother but slower scale changes; higher values make scaling more responsive but can cause visible snapping.
        /// Typical range: 0.8 to 1.0 for most use cases.
        /// </remarks>
        [Range(0, 1)]
        [Group("Lerp Speeds")]
        [Tooltip("How quickly to move scale towards the target. 0 = never, 1 = instant. Example: 0.85")]
        public float scaleLerpFactor = .85f;

        /// <summary>
        /// Rate at which estimated remote time is adjusted to match the owner's timeline (0 = never, 5 = instant).
        /// </summary>
        /// <remarks>
        /// Lower values provide smoother, gradual corrections; higher values respond more quickly to sudden latency changes but may cause abrupt timeline jumps.
        /// Recommended: keep below 0.5 unless severe latency spikes are expected.
        /// </remarks>
        [Range(0, 5)]
        [Group("Lerp Speeds")]
        [Tooltip("Time Correction Rate:\n" +
        "How quickly the local client's internal clock aligns with the remote authoritative clock for smooth playback. " +
        "Low values = gradual correction (smoother, but lag remains longer after spikes). " +
        "High values = fast correction (possible visible snapping after spikes). Typical: 0.1 (keep below 0.5 unless necessary).")]
        public float timeCorrectionRate = 0.01f;

        /// <summary>
        /// Axes to synchronize for position updates.
        /// </summary>
        /// <remarks>
        /// Select which position axes (X, Y, Z) to sync over the network.  
        /// For stationary objects, choose None to save bandwidth.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Syncs")]
        [LabelWidth(150)]
        public TransformSyncAxis syncPosition = TransformSyncAxis.AllAxes;

        /// <summary>
        /// Axes to synchronize for rotation updates.
        /// </summary>
        /// <remarks>
        /// Select which rotation axes (X, Y, Z) to sync over the network.  
        /// For objects that do not rotate, choose None.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Syncs")]
        [LabelWidth(150)]
        public TransformSyncAxis syncRotation = TransformSyncAxis.AllAxes;

        /// <summary>
        /// Axes to synchronize for scale updates.
        /// </summary>
        /// <remarks>
        /// Select which scale axes (X, Y, Z) to sync over the network.  
        /// Choose None for objects with static scale.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Syncs")]
        [LabelWidth(150)]
        public TransformSyncAxis syncScale = TransformSyncAxis.Disabled;

        /// <summary>
        /// Axes to synchronize for velocity updates.
        /// </summary>
        /// <remarks>
        /// Select which velocity axes (X, Y, Z) to sync for rigidbody objects.  
        /// Choose None for objects that never move.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Syncs")]
        [LabelWidth(150)]
        public TransformSyncAxis syncVelocity = TransformSyncAxis.AllAxes;

        /// <summary>
        /// Axes to synchronize for angular velocity updates.
        /// </summary>
        /// <remarks>
        /// Select which angular velocity axes (X, Y, Z) to sync for rigidbody objects.  
        /// Choose None for objects that do not rotate.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Syncs")]
        [LabelWidth(150)]
        public TransformSyncAxis syncAngularVelocity = TransformSyncAxis.AllAxes;

        /// <summary>
        /// Enable compression for position values to reduce bandwidth usage (uses half-precision floats).
        /// </summary>
        /// <remarks>
        /// When enabled, position values are sent as 16-bit floats (Half) instead of 32-bit, saving bandwidth but slightly reducing accuracy (noticeable error above ~600 units).
        /// </remarks>
        [Group("Sync & Compression"), Tab("Compression")]
        [LabelWidth(160)]
        public bool compressPosition = false;

        /// <summary>
        /// Enable compression for rotation values to reduce bandwidth usage (uses half-precision floats).
        /// </summary>
        /// <remarks>
        /// When enabled, rotation values are sent as 16-bit floats (Half) instead of 32-bit. May introduce minor precision loss.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Compression")]
        [LabelWidth(160)]
        public bool compressRotation = false;

        /// <summary>
        /// Enable compression for scale values to reduce bandwidth usage (uses half-precision floats).
        /// </summary>
        /// <remarks>
        /// When enabled, scale values are sent as 16-bit floats (Half) instead of 32-bit. Precision loss is usually negligible for most objects.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Compression")]
        [LabelWidth(160)]
        public bool compressScale = false;

        /// <summary>
        /// Enable compression for velocity values to reduce bandwidth usage (uses half-precision floats).
        /// </summary>
        /// <remarks>
        /// When enabled, velocity values are sent as 16-bit floats (Half) instead of 32-bit. May affect accuracy for extremely high speeds.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Compression")]
        [LabelWidth(160)]
        public bool compressVelocity = false;

        /// <summary>
        /// Enable compression for angular velocity values to reduce bandwidth usage (uses half-precision floats).
        /// </summary>
        /// <remarks>
        /// When enabled, angular velocity values are sent as 16-bit floats (Half) instead of 32-bit. Minor loss of accuracy may occur at very high rotations.
        /// </remarks>
        [Group("Sync & Compression"), Tab("Compression")]
        [LabelWidth(160)]
        public bool compressAngularVelocity = false;

        /// <summary>
        /// Minimum position change required before a new state is sent.
        /// </summary>
        /// <remarks>
        /// Set to 0 to send updated positions whenever they change.  
        /// Set above 0 to only send when the position has changed more than this value (in world units), reducing bandwidth but possibly lowering accuracy.
        /// </remarks>
        [Group("Thresholds"), Tab("Thresholds")]
        [LabelWidth(150), Min(0)]
        [Tooltip("Position Send Threshold:\n" +
        "Minimum position change (in units) required before sending a sync update. Set to 0 to always send changes. " +
        "Higher values = less bandwidth, but less accurate. 0.01 recommended for most games.")]
        public float positionSendThreshold = 0.01f;

        /// <summary>
        /// Minimum rotation change (in degrees) required before a new state is sent.
        /// </summary>
        /// <remarks>
        /// Set to 0 to send updated rotations whenever they change.  
        /// Set above 0 to only send when the rotation has changed more than this value, reducing bandwidth but possibly lowering accuracy.
        /// </remarks>
        [Group("Thresholds"), Tab("Thresholds")]
        [LabelWidth(150), Min(0)]
        [Tooltip("Rotation Send Threshold:\n" +
        "Minimum rotation change (in degrees) required before sending a sync update. Set to 0 to always send. Typical: 0.2.")]
        public float rotationSendThreshold = 0.1f;

        /// <summary>
        /// Minimum scale change required before a new state is sent.
        /// </summary>
        /// <remarks>
        /// Set to 0 to send updated scale whenever it changes.  
        /// Set above 0 to only send when the scale has changed more than this value (in world units), reducing bandwidth but possibly lowering accuracy.
        /// </remarks>
        [Group("Thresholds"), Tab("Thresholds")]
        [LabelWidth(150), Min(0)]
        [Tooltip("Scale Send Threshold:\n" +
        "Minimum scale change required before sending update. 0 for maximum accuracy, or higher to reduce updates.")]
        public float scaleSendThreshold = 0f;

        /// <summary>
        /// Minimum velocity change required before a new state is sent.
        /// </summary>
        /// <remarks>
        /// Set to 0 to send updated velocity whenever it changes.  
        /// Set above 0 to only send when the velocity has changed more than this value, reducing bandwidth but possibly lowering accuracy.  
        /// Measured in velocity units (e.g., meters/second).
        /// </remarks>
        [Group("Thresholds"), Tab("Thresholds")]
        [LabelWidth(150), Min(0)]
        [Tooltip("Velocity Send Threshold:\n" +
        "Minimum velocity change (in units/sec) before sending update. 0.01 is a good default.")]
        public float velocitySendThreshold = 0.01f;

        /// <summary>
        /// Minimum angular velocity change (in degrees/second) required before a new state is sent.
        /// </summary>
        /// <remarks>
        /// Set to 0 to send updated angular velocity whenever it changes.  
        /// Set above 0 to only send when the angular velocity has changed more than this value, reducing bandwidth but possibly lowering accuracy.
        /// </remarks>
        [Group("Thresholds"), Tab("Thresholds")]
        [LabelWidth(150), Min(0)]
        [Tooltip("Angular Velocity Send Threshold:\n" +
        "Minimum angular velocity change (degrees/sec) before sending update. 0.1 is a good default.")]
        public float angularVelocitySendThreshold = 0.1f;

        /// <summary>
        /// Minimum position change required to update remote objects on non-owners.
        /// </summary>
        /// <remarks>
        /// Set to 0 to update remote positions every frame.  
        /// Set above 0 to only update when the position is different from the target by more than this value, reducing unnecessary updates but possibly lowering smoothness.
        /// </remarks>
        [Group("Thresholds"), Tab("Thresholds")]
        [LabelWidth(150), Min(0)]
        [Tooltip("Position Update Threshold:\n" +
        "Minimum position change required to update remote objects on non-owners. Set to 0 to update every frame. " +
        "Higher values = less updates, but less smoothness.")]
        public float positionUpdateThreshold = 0.001f;

        /// <summary>
        /// Minimum rotation change (in degrees) required to update remote objects on non-owners.
        /// </summary>
        /// <remarks>
        /// Set to 0 to update remote rotation every frame.  
        /// Set above 0 to only update when the rotation is different from the target by more than this value, reducing unnecessary updates but possibly lowering smoothness.
        /// </remarks>
        [Group("Thresholds"), Tab("Thresholds")]
        [LabelWidth(150), Min(0)]
        [Tooltip("Rotation Update Threshold:\n" +
        "Minimum angular distance before remote client applies update. Used to prevent micro-jitter. 0.05 recommended.")]
        public float rotationUpdateThreshold = 0.05f;

        /// <summary>
        /// Distance threshold for immediate position snapping instead of interpolation.
        /// </summary>
        /// <remarks>
        /// If the difference between the current and target position exceeds this value (in world units), the object will instantly snap to the target position instead of smoothly interpolating.  
        /// Set to 0 to disable snapping.
        /// </remarks>
        [Group("Thresholds"), Tab("Snaps")]
        [LabelWidth(150)]
        [Range(0, 1000)]
        [Tooltip("Position Snap Threshold:\n" +
        "If the distance to the target exceeds this (in units), the object will instantly snap instead of smoothly interpolating. Prevents visible lag after spikes or teleportation. Typical: 2.0")]
        public float positionSnapThreshold = 0f;

        /// <summary>
        /// Angle threshold (in degrees) for immediate rotation snapping instead of interpolation.
        /// </summary>
        /// <remarks>
        /// If the difference between the current and target rotation exceeds this value, the object will instantly snap to the target rotation instead of smoothly interpolating.  
        /// Set to 0 to disable snapping.
        /// </remarks>
        [Group("Thresholds"), Tab("Snaps")]
        [LabelWidth(150)]
        [Range(0, 1000)]
        [Tooltip("Rotation Snap Threshold:\n" +
        "If the angular distance to the target exceeds this (in degrees), the object will instantly snap to the new rotation. Prevents visible lag. Typical: 30.")]
        public float rotationSnapThreshold = 0f;

        /// <summary>
        /// Scale difference threshold for immediate snapping instead of interpolation.
        /// </summary>
        /// <remarks>
        /// If the difference between the current and target scale exceeds this value (in world units), the object will instantly snap to the target scale instead of smoothly interpolating.  
        /// Set to 0 to disable snapping.
        /// </remarks>
        [Group("Thresholds"), Tab("Snaps")]
        [LabelWidth(150)]
        [Range(0, 1000)]
        [Tooltip("Scale Snap Threshold:\n" +
        "If the difference in scale exceeds this, the object will snap to the new scale. Used to prevent laggy scaling.")]
        public float scaleSnapThreshold = 0f;

        /// <summary>
        /// Time difference threshold for instantly correcting estimated owner time.
        /// </summary>
        /// <remarks>
        /// If the estimated owner time differs from the received value by more than this amount (in seconds), it is corrected immediately instead of gradually.  
        /// Increase this only if using very low send rates or expecting large network latency spikes.
        /// </remarks>
        [Group("Thresholds"), Tab("Snaps")]
        [LabelWidth(150), Min(0)]
        [Tooltip("Time Snap Threshold:\n" +
        "If the difference between the estimated and authoritative owner time exceeds this (in seconds), the client will immediately correct to the server's time, skipping interpolation. " +
        "This prevents long-term drift or visible lag after network spikes. Typical: 1.0")]
        public float timeSnapThreshold = 2.0f;

        /// <summary>
        /// Number of network updates sent per second.
        /// </summary>
        /// <remarks>
        /// Higher values increase update frequency and responsiveness but use more bandwidth.  
        /// For low send rates, decrease interpolation/lerp factors if movement appears jittery.  
        /// Recommended: keep interpolationDelay > (1 / sendRate) for smoothest results.
        /// </remarks>
        [Group("Others")]
        [Range(1f, 124f)]
        [LabelWidth(130)]
        [Tooltip("Send Rate:\n" +
        "Number of times per second the owner sends sync updates. Higher values = smoother, but more bandwidth. 30 is a common value for most games.")]
        public float m_SendRate = 30;

        [Group("Others")]
        [LabelWidth(130)]
        [Tooltip("Authority Mode:\n" +
        "Determines who is authoritative for the object's movement and state: Owner (the controlling client) or Server. Use Server for anti-cheat or when commands come from server logic.")]
        public AuthorityMode authorityMode;

        /// <summary>
        /// Update cycle used for applying synchronization to non-owners.
        /// </summary>
        /// <remarks>
        /// 'Update' offers smoother visual results; 'FixedUpdate' is recommended for physics-driven objects.
        /// </remarks>
        [Group("Others")]
        [LabelWidth(130)]
        [Tooltip("Update Mode:\n" +
        "Should the sync update logic run in Update (per frame, smoother visuals) or FixedUpdate (synchronized with physics, best for rigidbodies and cinemachine)? " +
        "Use FixedUpdate for all physics-driven movement.")]
        public UpdateMode UpdateMode = UpdateMode.Update;

        /// <summary>
        /// Use velocity-based synchronization on non-owners instead of direct position updates.
        /// </summary>
        /// <remarks>
        /// When enabled, remote objects use synchronized velocity to update their position for smoother results at high speeds.  
        /// Requires Rigidbody. Can be less accurate if objects are blocked or colliding.  
        /// Recommended to combine with positionSnapThreshold.
        /// </remarks>
        [Group("Others")]
        [LabelWidth(130)]
        [Tooltip(
            "Use Velocity Sync On Remotes:\n" +
            "If enabled, non-owned (remote) objects will update their position based on synchronized velocity data " +
            "instead of receiving direct position updates each tick. This approach often provides smoother motion at high speeds " +
            "and can help reduce visible stutter or jitter on fast-moving objects, especially in racing, flying, or physics-heavy games.\n\n" +
            "Requires the object to have a Rigidbody component. " +
            "Keep in mind: This method is less accurate if the object is blocked, collides, or experiences sudden stops—" +
            "for example, if it hits a wall, there may be a visible mismatch until the next network correction. " +
            "For best results, combine with a suitable Position Snap Threshold to instantly correct major errors after collisions."
        )]
        public bool useVelocitySyncOnRemotes = false;

        /// <summary>
        /// Maximum distance allowed for velocity-based correction before snapping to the target position.
        /// </summary>
        /// <remarks>
        /// If the predicted position differs from the target by more than this value (in world units), the object will snap to the correct location.  
        /// Helps to prevent visible drifting with high-speed movement or sudden corrections.
        /// </remarks>
        [Group("Others")]
        [LabelWidth(130)]
        [Range(0, 1000)]
        [ShowIf(nameof(useVelocitySyncOnRemotes))]
        [Tooltip(
            "Velocity Correction Threshold:\n" +
            "Maximum distance (in Unity world units) that a remote object's predicted position (using velocity sync) " +
            "can differ from the authoritative position before snapping instantly to the correct location.\n\n" +
            "A higher value allows more prediction error but can reduce visual popping from frequent snapping—" +
            "recommended for large, open, or fast-paced games. A lower value (e.g., 1-3 units) makes corrections snappier and keeps " +
            "objects tightly in sync, but may increase visible position jumps, especially if lag spikes or collisions occur. " +
            "Tweak for your gameplay: 10 is a safe default for most cases, adjust as needed for your game's scale and speed."
        )]
        public float velocityCorrectionThreshold = 10;

        /// <summary>
        /// Use local space for all synchronization operations.
        /// </summary>
        /// <remarks>
        /// Useful for VR applications or objects always operating in local space (e.g., hand controllers).  
        /// When enabled, all sync operations use localPosition/localRotation/localScale.
        /// </remarks>
        [Group("Others")]
        [LabelWidth(130)]
        [Tooltip("Use Local Transform:\n" +
        "If enabled, position/rotation/scale are synced as local to the parent object (good for VR hands, children, etc). If disabled, world transform is used.")]
        public bool useLocalTransform = false;

        private float _ownerTime;
        private float lastTimeOwnerTimeWasSet;

        public float ApproximateNetworkTimeOnOwner
        {
            get
            {
                return _ownerTime + (LocalTime - lastTimeOwnerTimeWasSet);
            }
            set
            {
                _ownerTime = value;
                lastTimeOwnerTimeWasSet = LocalTime;
            }
        }

        private NetworkTransformState latestValidatedState;

        [NonSerialized]
        public NetworkTransformState[] _buffer;

        [NonSerialized]
        public int stateCount;

        [NonSerialized]
        internal Transform transformToSync;

        [NonSerialized]
        public Rigidbody rb;
        [NonSerialized]
        public bool hasRigidbody = false;
        [NonSerialized]
        public Rigidbody2D rb2D;

        [NonSerialized]
        public bool hasRigidbody2D = false;


        bool dontEasePosition = false;

        bool dontEaseRotation = false;

        bool dontEaseScale = false;


        float firstReceivedMessageZeroTime;


        [NonSerialized]
        public float lastTimeStateWasSent;

        [NonSerialized]
        public Vector3 lastPositionWhenStateWasSent;

        [NonSerialized]
        public Quaternion lastRotationWhenStateWasSent = Quaternion.identity;

        [NonSerialized]
        public Vector3 lastScaleWhenStateWasSent;

        [NonSerialized]
        public Vector3 lastVelocityWhenStateWasSent;

        [NonSerialized]
        public Vector3 lastAngularVelocityWhenStateWasSent;

        [NonSerialized]
        public bool forceStateSend = false;
        [NonSerialized]
        public bool sendAtPositionalRestMessage = false;
        [NonSerialized]
        public bool sendAtRotationalRestMessage = false;

        [NonSerialized]
        public bool sendPosition;
        [NonSerialized]
        public bool sendRotation;
        [NonSerialized]
        public bool sendScale;
        [NonSerialized]
        public bool sendVelocity;
        [NonSerialized]
        public bool sendAngularVelocity;
        NetworkTransformState targetTempState;
        NetworkTransformState sendingTempState;
        [NonSerialized]
        public Vector3 latestReceivedVelocity;
        [NonSerialized]
        public Vector3 latestReceivedAngularVelocity;
        float timeSpentExtrapolating = 0;
        bool extrapolatedLastFrame = false;
        Vector3 positionLastFrame;
        Quaternion rotationLastFrame;
        int atRestThresholdCount = 3;
        int samePositionCount;
        int sameRotationCount;
        TransformMovementState restStatePosition = TransformMovementState.Moving;
        TransformMovementState restStateRotation = TransformMovementState.Moving;
        NetworkTransformState latestEndStateUsed;
        Vector3 latestTeleportedFromPosition;
        Quaternion latestTeleportedFromRotation;

        public bool IsSyncingXPosition => syncPosition == TransformSyncAxis.AllAxes ||
                     syncPosition == TransformSyncAxis.XAndY ||
                     syncPosition == TransformSyncAxis.XAndZ ||
                     syncPosition == TransformSyncAxis.XOnly;

        public bool IsSyncingYPosition => syncPosition == TransformSyncAxis.AllAxes ||
                     syncPosition == TransformSyncAxis.XAndY ||
                     syncPosition == TransformSyncAxis.YAndZ ||
                     syncPosition == TransformSyncAxis.YOnly;

        public bool IsSyncingZPosition => syncPosition == TransformSyncAxis.AllAxes ||
                     syncPosition == TransformSyncAxis.XAndZ ||
                     syncPosition == TransformSyncAxis.YAndZ ||
                     syncPosition == TransformSyncAxis.ZOnly;

        public bool IsSyncingXRotation => syncRotation == TransformSyncAxis.AllAxes ||
                     syncRotation == TransformSyncAxis.XAndY ||
                     syncRotation == TransformSyncAxis.XAndZ ||
                     syncRotation == TransformSyncAxis.XOnly;

        public bool IsSyncingYRotation => syncRotation == TransformSyncAxis.AllAxes ||
                     syncRotation == TransformSyncAxis.XAndY ||
                     syncRotation == TransformSyncAxis.YAndZ ||
                     syncRotation == TransformSyncAxis.YOnly;

        public bool IsSyncingZRotation => syncRotation == TransformSyncAxis.AllAxes ||
                     syncRotation == TransformSyncAxis.XAndZ ||
                     syncRotation == TransformSyncAxis.YAndZ ||
                     syncRotation == TransformSyncAxis.ZOnly;

        public bool IsSyncingXScale => syncScale == TransformSyncAxis.AllAxes ||
                     syncScale == TransformSyncAxis.XAndY ||
                     syncScale == TransformSyncAxis.XAndZ ||
                     syncScale == TransformSyncAxis.XOnly;

        public bool IsSyncingYScale => syncScale == TransformSyncAxis.AllAxes ||
                     syncScale == TransformSyncAxis.XAndY ||
                     syncScale == TransformSyncAxis.YAndZ ||
                     syncScale == TransformSyncAxis.YOnly;

        public bool IsSyncingZScale => syncScale == TransformSyncAxis.AllAxes ||
                     syncScale == TransformSyncAxis.XAndZ ||
                     syncScale == TransformSyncAxis.YAndZ ||
                     syncScale == TransformSyncAxis.ZOnly;

        public bool IsSyncingXVelocity => syncVelocity == TransformSyncAxis.AllAxes ||
                     syncVelocity == TransformSyncAxis.XAndY ||
                     syncVelocity == TransformSyncAxis.XAndZ ||
                     syncVelocity == TransformSyncAxis.XOnly;

        public bool IsSyncingYVelocity => syncVelocity == TransformSyncAxis.AllAxes ||
                     syncVelocity == TransformSyncAxis.XAndY ||
                     syncVelocity == TransformSyncAxis.YAndZ ||
                     syncVelocity == TransformSyncAxis.YOnly;

        public bool IsSyncingZVelocity => syncVelocity == TransformSyncAxis.AllAxes ||
                     syncVelocity == TransformSyncAxis.XAndZ ||
                     syncVelocity == TransformSyncAxis.YAndZ ||
                     syncVelocity == TransformSyncAxis.ZOnly;

        public bool IsSyncingXAngularVelocity => syncAngularVelocity == TransformSyncAxis.AllAxes ||
                     syncAngularVelocity == TransformSyncAxis.XAndY ||
                     syncAngularVelocity == TransformSyncAxis.XAndZ ||
                     syncAngularVelocity == TransformSyncAxis.XOnly;

        public bool IsSyncingYAngularVelocity => syncAngularVelocity == TransformSyncAxis.AllAxes ||
                     syncAngularVelocity == TransformSyncAxis.XAndY ||
                     syncAngularVelocity == TransformSyncAxis.YAndZ ||
                     syncAngularVelocity == TransformSyncAxis.YOnly;

        public bool IsSyncingZAngularVelocity => syncAngularVelocity == TransformSyncAxis.AllAxes ||
                     syncAngularVelocity == TransformSyncAxis.XAndZ ||
                     syncAngularVelocity == TransformSyncAxis.YAndZ ||
                     syncAngularVelocity == TransformSyncAxis.ZOnly;

        public bool HasAuthorityOrOwnedByServer => IsMine || (IsServer && IsOwnedByServer);
        public bool HasControl => (authorityMode == AuthorityMode.Owner && HasAuthorityOrOwnedByServer) || (authorityMode == AuthorityMode.Server && IsServer);

        /// <summary>
        /// Validates an incoming network transform state before it is applied to the local object.
        /// </summary>
        /// <param name="latestReceivedState">The most recently received state from the network.</param>
        /// <param name="latestValidatedState">The previously validated state, if any.</param>
        /// <returns>
        /// True if the <paramref name="latestReceivedState"/> should be accepted and applied; false to reject the update.
        /// </returns>
        /// <remarks>
        /// Override this method to add custom validation logic (e.g., anti-cheat, out-of-bounds detection, or filtering for invalid data)
        /// before accepting a received state. By default, all received states are accepted.
        /// </remarks>
        protected virtual bool OnValidateState(NetworkTransformState latestReceivedState, NetworkTransformState latestValidatedState) => true;

        public void Awake()
        {
            int calculatedStateBufferSize = ((int)(m_SendRate * interpolationDelay) + 1) * 2;
            _buffer = new NetworkTransformState[Mathf.Max(calculatedStateBufferSize, 30)];

            transformToSync = transform;
            rb = GetComponent<Rigidbody>();
            rb2D = GetComponent<Rigidbody2D>();

            if (rb)
            {
                hasRigidbody = true;
            }
            else if (rb2D)
            {
                hasRigidbody2D = true;
                if (syncVelocity != TransformSyncAxis.Disabled) syncVelocity = TransformSyncAxis.XAndY;
                if (syncAngularVelocity != TransformSyncAxis.Disabled) syncAngularVelocity = TransformSyncAxis.ZOnly;
            }

            if (!rb && !rb2D)
            {
                syncVelocity = TransformSyncAxis.Disabled;
                syncAngularVelocity = TransformSyncAxis.Disabled;
            }

            if (extrapolationMode == ExtrapolationMode.Unlimited)
            {
                useExtrapolationDistanceLimit = false;
                useExtrapolationLimitTime = false;
            }

            targetTempState = new NetworkTransformState();
            sendingTempState = new NetworkTransformState();
        }

        void Update()
        {
            if (UpdateMode == UpdateMode.Update)
                Process();
        }

        void FixedUpdate()
        {
            if (UpdateMode == UpdateMode.FixedUpdate)
                Process();

            SendState();
            positionLastFrame = GetPosition();
            rotationLastFrame = GetRotation();
            ResetFlags();
        }

        void Process()
        {
            LocalTime += Time.deltaTime;
            if (!HasControl)
            {
                FixTime();
                ApplyInterpolationOrExtrapolation();
            }
        }

        protected override void OnOwnershipGained()
        {
            TeleportOwnedObjectFromOwner();
        }

        private void SendState()
        {
            if (!IsRegistered) return;
            if (!HasControl || m_SendRate == 0) return;

            if (syncPosition != TransformSyncAxis.Disabled)
            {
                if (positionLastFrame == GetPosition())
                {
                    if (restStatePosition != TransformMovementState.Idle)
                    {
                        samePositionCount++;
                    }
                    if (samePositionCount == atRestThresholdCount)
                    {
                        samePositionCount = 0;
                        restStatePosition = TransformMovementState.Idle;
                        ForceStateSendNextFixedUpdate();
                    }
                }
                else
                {
                    if (restStatePosition == TransformMovementState.Idle && GetPosition() != latestTeleportedFromPosition)
                    {
                        restStatePosition = TransformMovementState.StartedMoving;
                        ForceStateSendNextFixedUpdate();
                    }
                    else if (restStatePosition == TransformMovementState.StartedMoving)
                    {
                        restStatePosition = TransformMovementState.Moving;
                    }
                    else
                    {
                        samePositionCount = 0;
                    }
                }
            }
            else
            {
                restStatePosition = TransformMovementState.Idle;
            }

            if (syncRotation != TransformSyncAxis.Disabled)
            {
                if (rotationLastFrame == GetRotation())
                {
                    if (restStateRotation != TransformMovementState.Idle)
                    {
                        sameRotationCount++;
                    }

                    if (sameRotationCount == atRestThresholdCount)
                    {
                        sameRotationCount = 0;
                        restStateRotation = TransformMovementState.Idle;
                        ForceStateSendNextFixedUpdate();
                    }
                }
                else
                {
                    if (restStateRotation == TransformMovementState.Idle && GetRotation() != latestTeleportedFromRotation)
                    {
                        restStateRotation = TransformMovementState.StartedMoving;
                        ForceStateSendNextFixedUpdate();
                    }
                    else if (restStateRotation == TransformMovementState.StartedMoving)
                    {
                        restStateRotation = TransformMovementState.Moving;
                    }
                    else
                    {
                        sameRotationCount = 0;
                    }
                }
            }
            else
            {
                restStateRotation = TransformMovementState.Idle;
            }

            if (LocalTime - lastTimeStateWasSent <= (1f / m_SendRate) && !forceStateSend) return;

            sendPosition = ShouldSendPosition();
            sendRotation = ShouldSendRotation();
            sendScale = ShouldSendScale();
            sendVelocity = ShouldSendVelocity();
            sendAngularVelocity = ShouldSendAngularVelocity();

            if (!sendPosition && !sendRotation && !sendScale && !sendVelocity && !sendAngularVelocity) return;

            sendingTempState.CaptureFromSync(this);

            if (restStatePosition == TransformMovementState.Idle) sendAtPositionalRestMessage = true;
            if (restStateRotation == TransformMovementState.Idle) sendAtRotationalRestMessage = true;

            if (restStatePosition == TransformMovementState.StartedMoving)
            {
                sendingTempState.m_Position = lastPositionWhenStateWasSent;
            }
            if (restStateRotation == TransformMovementState.StartedMoving)
            {
                sendingTempState.m_Rotation = lastRotationWhenStateWasSent;
            }
            if (restStatePosition == TransformMovementState.StartedMoving ||
                restStateRotation == TransformMovementState.StartedMoving)
            {
                sendingTempState.m_OwnerTimestamp = LocalTime - Time.deltaTime;
                if (restStatePosition != TransformMovementState.StartedMoving)
                {
                    sendingTempState.m_Position = positionLastFrame;
                }
                if (restStateRotation != TransformMovementState.StartedMoving)
                {
                    sendingTempState.m_Rotation = rotationLastFrame;
                }
            }

            lastTimeStateWasSent = LocalTime;

            if (IsServer)
            {
                Send(sendingTempState);

                if (sendPosition) lastPositionWhenStateWasSent = sendingTempState.m_Position;
                if (sendRotation) lastRotationWhenStateWasSent = sendingTempState.m_Rotation;
                if (sendScale) lastScaleWhenStateWasSent = sendingTempState.m_Scale;
                if (sendVelocity) lastVelocityWhenStateWasSent = sendingTempState.m_Velocity;
                if (sendAngularVelocity) lastAngularVelocityWhenStateWasSent = sendingTempState.m_AngularVelocity;
            }
            else if (IsMine)
            {
                using var writer = NetworkManager.Pool.Rent();
                sendingTempState.Serialize(writer);
                Client.Rpc(authorityMode == AuthorityMode.Server ? k_StateSyncRpcId : k_StateSyncToOthersRpcId, writer);
            }
        }

        bool triedToExtrapolateTooFar = false;
        void ApplyInterpolationOrExtrapolation()
        {
            if (stateCount == 0) return;

            if (!extrapolatedLastFrame)
            {
                targetTempState.Reset();
            }

            triedToExtrapolateTooFar = false;

            float interpolationTime = ApproximateNetworkTimeOnOwner - interpolationDelay;

            if (stateCount > 1 && _buffer[0].m_OwnerTimestamp > interpolationTime)
            {
                Interpolate(interpolationTime);
                extrapolatedLastFrame = false;
            }
            else if (_buffer[0].atPositionalRest && _buffer[0].atRotationalRest)
            {
                targetTempState.CopyFrom(_buffer[0]);
                extrapolatedLastFrame = false;
                if (useVelocitySyncOnRemotes) triedToExtrapolateTooFar = true;
            }
            else
            {
                bool success = Extrapolate(interpolationTime);
                extrapolatedLastFrame = true;
                triedToExtrapolateTooFar = !success;

                if (useVelocitySyncOnRemotes)
                {
                    float timeSinceLatestReceive = interpolationTime - _buffer[0].m_OwnerTimestamp;
                    targetTempState.m_Velocity = _buffer[0].m_Velocity;
                    targetTempState.m_Position = _buffer[0].m_Position + targetTempState.m_Velocity * timeSinceLatestReceive;
                    Vector3 predictedPos = transform.position + targetTempState.m_Velocity * Time.deltaTime;
                    float percent = (targetTempState.m_Position - predictedPos).sqrMagnitude / (velocityCorrectionThreshold * velocityCorrectionThreshold);
                    targetTempState.m_Velocity = Vector3.Lerp(targetTempState.m_Velocity, (targetTempState.m_Position - transform.position) / Time.deltaTime, percent);
                }
            }

            float actualPositionLerpSpeed = positionLerpFactor;
            float actualRotationLerpSpeed = rotationLerpFactor;
            float actualScaleLerpSpeed = scaleLerpFactor;

            bool teleportPosition = false;
            bool teleportRotation = false;

            if (dontEasePosition)
            {
                actualPositionLerpSpeed = 1;
                teleportPosition = true;
                dontEasePosition = false;
            }
            if (dontEaseRotation)
            {
                actualRotationLerpSpeed = 1;
                teleportRotation = true;
                dontEaseRotation = false;
            }
            if (dontEaseScale)
            {
                actualScaleLerpSpeed = 1;
                dontEaseScale = false;
            }

            if (!triedToExtrapolateTooFar)
            {
                bool changedPositionEnough = false;
                float distance = 0;
                if (GetPosition() != targetTempState.m_Position)
                {
                    if (positionUpdateThreshold != 0)
                    {
                        distance = Vector3.Distance(GetPosition(), targetTempState.m_Position);
                    }
                }
                if (positionUpdateThreshold != 0)
                {
                    if (distance > positionUpdateThreshold)
                    {
                        changedPositionEnough = true;
                    }
                }
                else
                {
                    changedPositionEnough = true;
                }

                bool changedRotationEnough = false;
                float angleDifference = 0;
                if (GetRotation() != targetTempState.m_Rotation)
                {
                    if (rotationUpdateThreshold != 0)
                    {
                        angleDifference = Quaternion.Angle(GetRotation(), targetTempState.m_Rotation);
                    }
                }
                if (rotationUpdateThreshold != 0)
                {
                    if (angleDifference > rotationUpdateThreshold)
                    {
                        changedRotationEnough = true;
                    }
                }
                else
                {
                    changedRotationEnough = true;
                }

                bool changedScaleEnough = false;
                if (GetScale() != targetTempState.m_Scale)
                {
                    changedScaleEnough = true;
                }

                if (syncPosition != TransformSyncAxis.Disabled)
                {
                    if (changedPositionEnough)
                    {
                        Vector3 newPosition = GetPosition();
                        if (IsSyncingXPosition)
                        {
                            newPosition.x = targetTempState.m_Position.x;
                        }
                        if (IsSyncingYPosition)
                        {
                            newPosition.y = targetTempState.m_Position.y;
                        }
                        if (IsSyncingZPosition)
                        {
                            newPosition.z = targetTempState.m_Position.z;
                        }

                        if (useVelocitySyncOnRemotes && !teleportPosition)
                        {
#if UNITY_6000_0_OR_NEWER
                            if (hasRigidbody) rb.linearVelocity = targetTempState.m_Velocity;
                            if (hasRigidbody2D) rb2D.linearVelocity = targetTempState.m_Velocity;
#else
                            if (hasRigidbody) rb.velocity = targetTempState.m_Velocity;
                            if (hasRigidbody2D) rb2D.velocity = targetTempState.m_Velocity;
#endif
                        }
                        else
                        {
                            SetPosition(Vector3.Lerp(GetPosition(), newPosition, actualPositionLerpSpeed), teleportPosition);
                        }
                    }
                }
                if (syncRotation != TransformSyncAxis.Disabled)
                {
                    if (changedRotationEnough)
                    {
                        Vector3 newRotation = GetRotation().eulerAngles;
                        if (IsSyncingXRotation)
                        {
                            newRotation.x = targetTempState.m_Rotation.eulerAngles.x;
                        }
                        if (IsSyncingYRotation)
                        {
                            newRotation.y = targetTempState.m_Rotation.eulerAngles.y;
                        }
                        if (IsSyncingZRotation)
                        {
                            newRotation.z = targetTempState.m_Rotation.eulerAngles.z;
                        }

                        Quaternion newQuaternion = Quaternion.Euler(newRotation);
                        SetRotation(Quaternion.Lerp(GetRotation(), newQuaternion, actualRotationLerpSpeed), teleportRotation);
                    }
                }
                if (syncScale != TransformSyncAxis.Disabled)
                {
                    if (changedScaleEnough)
                    {
                        Vector3 newScale = GetScale();
                        if (IsSyncingXScale)
                        {
                            newScale.x = targetTempState.m_Scale.x;
                        }
                        if (IsSyncingYScale)
                        {
                            newScale.y = targetTempState.m_Scale.y;
                        }
                        if (IsSyncingZScale)
                        {
                            newScale.z = targetTempState.m_Scale.z;
                        }

                        SetScale(Vector3.Lerp(GetScale(), newScale, actualScaleLerpSpeed));
                    }
                }
            }
            else if (triedToExtrapolateTooFar)
            {
                if (hasRigidbody)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector3.zero;
#else
                    rb.velocity = Vector3.zero;
#endif
                    rb.angularVelocity = Vector3.zero;
                }
                if (hasRigidbody2D)
                {
#if UNITY_6000_0_OR_NEWER
                    rb2D.linearVelocity = Vector2.zero;
#else
                    rb2D.velocity = Vector2.zero;
#endif
                    rb2D.angularVelocity = 0;
                }
            }
        }

        void Interpolate(float interpolationTime)
        {
            int stateIndex = 0;
            for (; stateIndex < stateCount; stateIndex++)
            {
                if (_buffer[stateIndex].m_OwnerTimestamp <= interpolationTime) break;
            }

            if (stateIndex == stateCount)
                stateIndex--;

            NetworkTransformState end = _buffer[Mathf.Max(stateIndex - 1, 0)];
            NetworkTransformState start = _buffer[stateIndex];

            float t = (interpolationTime - start.m_OwnerTimestamp) / (end.m_OwnerTimestamp - start.m_OwnerTimestamp);
            ShouldTeleport(start, ref end, interpolationTime, ref t);

            targetTempState = NetworkTransformState.Lerp(targetTempState, start, end, t);
            if (positionSnapThreshold != 0)
            {
                float positionDifference = (end.m_Position - start.m_Position).magnitude;
                if (positionDifference > positionSnapThreshold)
                {
                    targetTempState.m_Position = end.m_Position;
                }

                dontEasePosition = true;
            }

            if (scaleSnapThreshold != 0)
            {
                float scaleDifference = (end.m_Scale - start.m_Scale).magnitude;
                if (scaleDifference > scaleSnapThreshold)
                {
                    targetTempState.m_Scale = end.m_Scale;
                }

                dontEaseScale = true;
            }

            if (rotationSnapThreshold != 0)
            {
                float rotationDifference = Quaternion.Angle(end.m_Rotation, start.m_Rotation);
                if (rotationDifference > rotationSnapThreshold)
                {
                    targetTempState.m_Rotation = end.m_Rotation;
                }

                dontEaseRotation = true;
            }

            if (useVelocitySyncOnRemotes)
            {
                Vector3 predictedPos = transform.position + targetTempState.m_Velocity * Time.deltaTime;
                float percent = (targetTempState.m_Position - predictedPos).sqrMagnitude / (velocityCorrectionThreshold * velocityCorrectionThreshold);
                targetTempState.m_Velocity = Vector3.Lerp(targetTempState.m_Velocity, (targetTempState.m_Position - transform.position) / Time.deltaTime, percent);
            }
        }

        private bool Extrapolate(float interpolationTime)
        {
            if (!extrapolatedLastFrame || targetTempState.m_OwnerTimestamp < _buffer[0].m_OwnerTimestamp)
            {
                targetTempState.CopyFrom(_buffer[0]);
                timeSpentExtrapolating = 0;
            }

            if (extrapolationMode != ExtrapolationMode.None && stateCount >= 2)
            {
                if (syncVelocity == TransformSyncAxis.Disabled && !_buffer[0].atPositionalRest)
                {
                    targetTempState.m_Velocity = (_buffer[0].m_Position - _buffer[1].m_Position) / (_buffer[0].m_OwnerTimestamp - _buffer[1].m_OwnerTimestamp);
                }
                if (syncAngularVelocity == TransformSyncAxis.Disabled && !_buffer[0].atRotationalRest)
                {
                    Quaternion deltaRot = _buffer[0].m_Rotation * Quaternion.Inverse(_buffer[1].m_Rotation);
                    Vector3 eulerRot = new(Mathf.DeltaAngle(0, deltaRot.eulerAngles.x), Mathf.DeltaAngle(0, deltaRot.eulerAngles.y), Mathf.DeltaAngle(0, deltaRot.eulerAngles.z));
                    Vector3 angularVelocity = eulerRot / (_buffer[0].m_OwnerTimestamp - _buffer[1].m_OwnerTimestamp);
                    targetTempState.m_AngularVelocity = angularVelocity;
                }
            }

            if (extrapolationMode == ExtrapolationMode.None) return false;

            if (useExtrapolationLimitTime &&
                timeSpentExtrapolating > extrapolationLimitTime)
            {
                return false;
            }

            bool hasVelocity = Mathf.Abs(targetTempState.m_Velocity.x) >= .01f || Mathf.Abs(targetTempState.m_Velocity.y) >= .01f ||
                Mathf.Abs(targetTempState.m_Velocity.z) >= .01f;
            bool hasAngularVelocity = Mathf.Abs(targetTempState.m_AngularVelocity.x) >= .01f || Mathf.Abs(targetTempState.m_AngularVelocity.y) >= .01f ||
                Mathf.Abs(targetTempState.m_AngularVelocity.z) >= .01f;

            if (!hasVelocity && !hasAngularVelocity)
                return false;

            float timeDif = timeSpentExtrapolating == 0 ? interpolationTime - targetTempState.m_OwnerTimestamp : Time.deltaTime;
            timeSpentExtrapolating += timeDif;
            if (hasVelocity)
            {
                targetTempState.m_Position += targetTempState.m_Velocity * timeDif;
                if (Mathf.Abs(targetTempState.m_Velocity.y) >= .01f)
                {
                    if (hasRigidbody && rb.useGravity)
                    {
                        targetTempState.m_Velocity += Physics.gravity * timeDif;
                    }
                    else if (hasRigidbody2D)
                    {
                        targetTempState.m_Velocity += rb2D.gravityScale * timeDif * Physics.gravity;
                    }
                }

                if (hasRigidbody)
                {
#if UNITY_6000_0_OR_NEWER
                    targetTempState.m_Velocity -= targetTempState.m_Velocity * timeDif * rb.linearDamping;
#else
                    targetTempState.m_Velocity -= rb.drag * timeDif * targetTempState.m_Velocity;
#endif
                }
                else if (hasRigidbody2D)
                {
#if UNITY_6000_0_OR_NEWER
                    targetTempState.m_Velocity -= targetTempState.m_Velocity * timeDif * rb2D.linearDamping;
#else
                    targetTempState.m_Velocity -= rb2D.drag * timeDif * targetTempState.m_Velocity;
#endif
                }
            }

            if (hasAngularVelocity)
            {
                float axisLength = timeDif * targetTempState.m_AngularVelocity.magnitude;
                Quaternion angularRotation = Quaternion.AngleAxis(axisLength, targetTempState.m_AngularVelocity);
                targetTempState.m_Rotation = angularRotation * targetTempState.m_Rotation;

                float angularDrag = 0;
#if UNITY_6000_0_OR_NEWER
                if (hasRigidbody) angularDrag = rb.angularDamping;
                if (hasRigidbody2D) angularDrag = rb2D.angularDamping;
#else
                if (hasRigidbody) angularDrag = rb.angularDrag;
                if (hasRigidbody2D) angularDrag = rb2D.angularDrag;
#endif
                if ((hasRigidbody || hasRigidbody2D) && angularDrag > 0)
                {
                    targetTempState.m_AngularVelocity -= angularDrag * timeDif * targetTempState.m_AngularVelocity;
                }
            }

            return !useExtrapolationDistanceLimit ||
                Vector3.Distance(_buffer[0].m_Position, targetTempState.m_Position) < extrapolationDistanceLimit;
        }

        void ShouldTeleport(NetworkTransformState start, ref NetworkTransformState end, float interpolationTime, ref float t)
        {
            if (start.m_OwnerTimestamp > interpolationTime && start.isTeleport && stateCount == 2)
            {
                end = start;
                t = 1;
                StopEasing();
            }

            for (int i = 0; i < stateCount; i++)
            {
                if (_buffer[i] == latestEndStateUsed && latestEndStateUsed != end && latestEndStateUsed != start)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (_buffer[j].isTeleport == true)
                        {
                            t = 1;
                            StopEasing();
                        }
                        if (_buffer[j] == start) break;
                    }
                    break;
                }
            }
            latestEndStateUsed = end;

            if (end.isTeleport == true)
            {
                t = 1;
                StopEasing();
            }
        }

        public Vector3 GetPosition()
        {
            return useLocalTransform ? transformToSync.localPosition : transformToSync.position;
        }

        public Quaternion GetRotation()
        {
            return useLocalTransform ? transformToSync.localRotation : transformToSync.rotation;
        }

        public Vector3 GetScale()
        {
            return transformToSync.localScale;
        }

        public void SetPosition(Vector3 position, bool isTeleporting)
        {
            if (useLocalTransform)
            {
                transformToSync.localPosition = position;
            }
            else
            {
                if (hasRigidbody && !isTeleporting && UpdateMode == UpdateMode.FixedUpdate)
                {
                    rb.MovePosition(position);
                }
                else if (hasRigidbody2D && !isTeleporting && UpdateMode == UpdateMode.FixedUpdate)
                {
                    rb2D.MovePosition(position);
                }
                else
                {
                    transformToSync.position = position;
                }
            }
        }

        public void SetRotation(Quaternion rotation, bool isTeleporting)
        {
            if (useLocalTransform)
            {
                transformToSync.localRotation = rotation;
            }
            else
            {
                if (hasRigidbody && !isTeleporting && UpdateMode == UpdateMode.FixedUpdate)
                {
                    rb.MoveRotation(rotation);
                }
                else if (hasRigidbody2D && !isTeleporting && UpdateMode == UpdateMode.FixedUpdate)
                {
                    rb2D.MoveRotation(rotation.eulerAngles.z);
                }
                else
                {
                    transformToSync.rotation = rotation;
                }
            }
        }

        public void SetScale(Vector3 scale)
        {
            transformToSync.localScale = scale;
        }

        void ResetFlags()
        {
            forceStateSend = false;
            sendAtPositionalRestMessage = false;
            sendAtRotationalRestMessage = false;
        }

        public void AddState(NetworkTransformState state)
        {
            if (stateCount > 1)
            {
                float deltaTime = state.m_OwnerTimestamp - _buffer[0].m_OwnerTimestamp;
                bool isOutOfOrder = deltaTime <= 0;

                if (isOutOfOrder)
                    return;
            }

            for (int i = _buffer.Length - 1; i >= 1; i--)
                _buffer[i] = _buffer[i - 1];

            _buffer[0] = state;
            stateCount = Mathf.Min(stateCount + 1, _buffer.Length);
        }

        public void StopEasing()
        {
            dontEasePosition = true;
            dontEaseRotation = true;
            dontEaseScale = true;
        }

        public void ClearBuffer()
        {
            stateCount = 0;
            firstReceivedMessageZeroTime = 0;
            restStatePosition = TransformMovementState.Moving;
            restStateRotation = TransformMovementState.Moving;
        }

        public void TeleportOwnedObjectFromOwner()
        {
            if (!HasControl)
            {
                if (IsServer)
                {
                    NetworkLogger.Print(
                        "Teleport request denied. As the server, use `TeleportAnyObjectFromServer` to relocate non-owned objects with a specified transform.",
                        NetworkLogger.LogType.Warning
                    );
                }
                else
                {
                    NetworkLogger.Print(
                        "Teleport request rejected. Only the object owner or the server may initiate remote movement operations.",
                        NetworkLogger.LogType.Warning
                    );
                }

                return;
            }

            latestTeleportedFromPosition = GetPosition();
            latestTeleportedFromRotation = GetRotation();
            if (IsServer)
            {
                Server.Rpc(3, GetPosition(), GetRotation().eulerAngles, GetScale(), LocalTime);
            }
            else if (IsMine)
            {
                Client.Rpc(3, GetPosition(), GetRotation().eulerAngles, GetScale(), LocalTime);
            }
        }

        public void TeleportAnyObjectFromServer(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
        {
            if (HasControl)
            {
                SetPosition(newPosition, true);
                SetRotation(newRotation, true);
                SetScale(newScale);
                TeleportOwnedObjectFromOwner();
            }
            else if (IsServer)
            {
                Server.Rpc(2, newPosition, newRotation.eulerAngles, newScale);
            }
            else
            {
                NetworkLogger.Print("Only the server can call this method.", NetworkLogger.LogType.Warning);
            }
        }

        [Client(2, DeliveryMode = DeliveryMode.Unreliable)]
        private void NonServerOwnedTeleportFromServerClientRpc(Vector3 newPosition, Vector3 newRotation, Vector3 newScale)
        {
            if (HasAuthorityOrOwnedByServer)
            {
                SetPosition(newPosition, true);
                SetRotation(Quaternion.Euler(newRotation), true);
                SetScale(newScale);
                TeleportOwnedObjectFromOwner();
            }
        }

        [Server(3, DeliveryMode = DeliveryMode.Unreliable)]
        private void TeleportServerRpc(Vector3 position, Vector3 rotation, Vector3 scale, float tempOwnerTime)
        {
            TeleportClientRpc(position, rotation, scale, tempOwnerTime);

            NetworkTransformState teleportState = new();
            teleportState.CaptureFromSync(this);
            teleportState.m_Position = position;
            teleportState.m_Rotation = Quaternion.Euler(rotation);
            teleportState.m_OwnerTimestamp = tempOwnerTime;
            teleportState.m_ReceivedTimestamp = LocalTime;
            teleportState.isTeleport = true;

            AddTeleportState(teleportState);
        }

        [Client(3)]
        private void TeleportClientRpc(Vector3 position, Vector3 rotation, Vector3 scale, float tempOwnerTime)
        {
            if (HasAuthorityOrOwnedByServer || IsServer) return;

            NetworkTransformState teleportState = new();
            teleportState.CaptureFromSync(this);
            teleportState.m_Position = position;
            teleportState.m_Rotation = Quaternion.Euler(rotation);
            teleportState.m_OwnerTimestamp = tempOwnerTime;
            teleportState.m_ReceivedTimestamp = LocalTime;
            teleportState.isTeleport = true;

            AddTeleportState(teleportState);
        }

        void AddTeleportState(NetworkTransformState teleportState)
        {
            if (teleportState != null)
            {
                teleportState.atPositionalRest = true;
                teleportState.atRotationalRest = true;
            }

            if (stateCount == 0) ApproximateNetworkTimeOnOwner = teleportState.m_OwnerTimestamp;
            if (stateCount == 0 || teleportState.m_OwnerTimestamp >= _buffer[0].m_OwnerTimestamp)
            {
                for (int k = _buffer.Length - 1; k >= 1; k--)
                    _buffer[k] = _buffer[k - 1];

                _buffer[0] = teleportState;
            }
            else
            {
                if (stateCount == _buffer.Length && _buffer[stateCount - 1].m_OwnerTimestamp > teleportState.m_OwnerTimestamp)
                    return;

                for (int i = stateCount - 1; i >= 0; i--)
                {
                    if (_buffer[i].m_OwnerTimestamp > teleportState.m_OwnerTimestamp)
                    {
                        for (int j = _buffer.Length - 1; j > i + 1; j--)
                        {
                            _buffer[j] = _buffer[j - 1];
                        }

                        _buffer[i + 1] = teleportState;
                        break;
                    }
                }
            }

            stateCount = Mathf.Min(stateCount + 1, _buffer.Length);
        }

        public void ForceStateSendNextFixedUpdate()
        {
            forceStateSend = true;
        }

        public bool ShouldSendPosition()
        {
            return syncPosition != TransformSyncAxis.Disabled &&
                (forceStateSend ||
                (GetPosition() != lastPositionWhenStateWasSent &&
                (positionSendThreshold == 0 || Vector3.Distance(lastPositionWhenStateWasSent, GetPosition()) > positionSendThreshold)));
        }

        public bool ShouldSendRotation()
        {
            return syncRotation != TransformSyncAxis.Disabled &&
                (forceStateSend ||
                (GetRotation() != lastRotationWhenStateWasSent &&
                (rotationSendThreshold == 0 || Quaternion.Angle(lastRotationWhenStateWasSent, GetRotation()) > rotationSendThreshold)));
        }

        public bool ShouldSendScale()
        {
            return syncScale != TransformSyncAxis.Disabled &&
                (forceStateSend ||
                (GetScale() != lastScaleWhenStateWasSent &&
                (scaleSendThreshold == 0 || Vector3.Distance(lastScaleWhenStateWasSent, GetScale()) > scaleSendThreshold)));
        }

        public bool ShouldSendVelocity()
        {
            if (hasRigidbody)
            {
#if UNITY_6000_0_OR_NEWER
                Vector3 velocity = rb.linearVelocity;
#else
                Vector3 velocity = rb.velocity;
#endif
                return syncVelocity != TransformSyncAxis.Disabled &&
                    (forceStateSend ||
                    (velocity != lastVelocityWhenStateWasSent &&
                    (velocitySendThreshold == 0 || Vector3.Distance(lastVelocityWhenStateWasSent, velocity) > velocitySendThreshold)));
            }

            if (hasRigidbody2D)
            {
#if UNITY_6000_0_OR_NEWER
                Vector2 velocity = rb2D.linearVelocity;
#else
                Vector2 velocity = rb2D.velocity;
#endif
                return syncVelocity != TransformSyncAxis.Disabled &&
                    (forceStateSend ||
                    ((velocity.x != lastVelocityWhenStateWasSent.x || velocity.y != lastVelocityWhenStateWasSent.y) &&
                    (velocitySendThreshold == 0 || Vector2.Distance(lastVelocityWhenStateWasSent, velocity) > velocitySendThreshold)));
            }

            return false;
        }

        public bool ShouldSendAngularVelocity()
        {
            if (hasRigidbody)
            {
                return syncAngularVelocity != TransformSyncAxis.Disabled &&
                    (forceStateSend ||
                    (rb.angularVelocity != lastAngularVelocityWhenStateWasSent &&
                    (angularVelocitySendThreshold == 0 ||
                    Vector3.Distance(lastAngularVelocityWhenStateWasSent, rb.angularVelocity * Mathf.Rad2Deg) > angularVelocitySendThreshold)));
            }

            if (hasRigidbody2D)
            {
                return syncAngularVelocity != TransformSyncAxis.Disabled &&
                    (forceStateSend ||
                    (rb2D.angularVelocity != lastAngularVelocityWhenStateWasSent.z &&
                    (angularVelocitySendThreshold == 0 ||
                    Mathf.Abs(lastAngularVelocityWhenStateWasSent.z - rb2D.angularVelocity) > angularVelocitySendThreshold)));
            }

            return false;
        }

        void Send(NetworkTransformState state)
        {
            using var message = NetworkManager.Pool.Rent();
            state.Serialize(message);
            Server.Rpc(authorityMode == AuthorityMode.Server ? k_StateSyncRpcId : k_StateSyncToOthersRpcId, message);
        }

        [Client(k_StateSyncRpcId, DeliveryMode = DeliveryMode.Unreliable)]
        [Client(k_StateSyncToOthersRpcId, DeliveryMode = DeliveryMode.Unreliable)]
        void OnClientStateReceivedRpc(DataBuffer data) => OnServerStateReceivedRpc(data, Identity.Owner);

        [Server(k_StateSyncRpcId, DeliveryMode = DeliveryMode.Unreliable)]
        [Server(k_StateSyncToOthersRpcId, Target = Target.Others, DeliveryMode = DeliveryMode.Unreliable)]
        void OnServerStateReceivedRpc(DataBuffer data, NetworkPeer peer)
        {
            NetworkTransformState networkState = NetworkTransformState.Deserialize(data, this);
            if (IsServer)
            {
                if (networkState.networkTransform == null || networkState.networkTransform.Identity.Owner.Id != peer.Id) return;
                if (networkState.networkTransform.latestValidatedState == null || networkState.networkTransform.OnValidateState(networkState, networkState.networkTransform.latestValidatedState))
                {
                    networkState.networkTransform.latestValidatedState = networkState;
                    networkState.networkTransform.Send(networkState);
                    networkState.networkTransform.AddState(networkState);
                }
            }
            else
            {
                if (networkState != null && networkState.networkTransform != null && !networkState.networkTransform.HasControl)
                {
                    networkState.networkTransform.AddState(networkState);
                }
            }
        }

        protected override void Reset()
        {
            base.Reset();
            if (transform != transform.root)
            {
                useLocalTransform = true;
                NetworkHelper.EditorSaveObject(gameObject);
            }
        }

        [HideInInspector]
        public int recStatesCounter;
        private void FixTime()
        {
            if (_buffer[0] == null || (_buffer[0].atPositionalRest && _buffer[0].atRotationalRest))
                return;

            float timeCorrection = Mathf.Max(timeCorrectionRate * Time.deltaTime, minTimePrecision);
            if (firstReceivedMessageZeroTime == 0)
                firstReceivedMessageZeroTime = LocalTime;

            float newTime = _buffer[0].m_OwnerTimestamp + (LocalTime - _buffer[0].m_ReceivedTimestamp);
            float timeChangeMagnitude = Mathf.Abs(ApproximateNetworkTimeOnOwner - newTime);
            if (recStatesCounter < m_SendRate || timeChangeMagnitude < timeCorrection || timeChangeMagnitude > timeSnapThreshold)
            {
                ApproximateNetworkTimeOnOwner = newTime;
            }
            else
            {
                if (ApproximateNetworkTimeOnOwner < newTime) ApproximateNetworkTimeOnOwner += timeCorrection;
                else ApproximateNetworkTimeOnOwner -= timeCorrection;
            }
        }
    }
}