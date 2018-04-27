﻿// Kerbal Attachment System
// Mod idea: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using UnityEngine;

namespace KASAPIv1 {

/// <summary>Base interface for an end of the link.</summary>
/// <remarks>
/// This interface represents the complete definition of the link's state. However, it explicitly
/// ignores the <i>logic</i> of making a link. Such a logic must be implemented in the specialized
/// interfaces.
/// </remarks>
/// <seealso cref="ILinkSource"/>
/// <seealso cref="ILinkTarget"/>
public interface ILinkPeer {
  /// <summary>Part that owns the source.</summary>
  /// <value>Instance of the part.</value>
  Part part { get; }

  /// <summary>Source link type identifier.</summary>
  /// <value>Arbitrary string. Can be empty.</value>
  /// <remarks>
  /// This value is used to find the compatible peers. The peers of the different types will not
  /// be able to connect with each other.
  /// </remarks>
  string cfgLinkType { get; }

  /// <summary>Name of the attach node on the part.</summary>
  /// <value>Arbitrary string. It cannot be empty.</value>
  /// <remarks>
  /// Every link peer is bound to an attach node thru which the link is made (including the coupling
  /// operations). Several peer modules can share the same node, but in case of any of the modules
  /// has made a link, the other dependents will go into the <see cref="LinkState.Locked"/> state.
  /// I.e. only one link can be made thru one attach node.
  /// </remarks>
  string cfgAttachNodeName { get; }

  /// <summary>Current state of the peer.</summary>
  /// <value>The current state.</value>
  /// <seealso cref="isLinked"/>
  /// <seealso cref="isLocked"/>
  LinkState linkState { get; }

  /// <summary>Other end of the link.</summary>
  /// <value>The other end of the link or <c>null</c> if no link established.</value>
  ILinkPeer otherPeer { get; }

  /// <summary>The persisted ID of the linked part of the other peer.</summary>
  /// <value>The flight ID of the part.</value>
  /// <remarks>This value must be available during the vessel loading.</remarks>
  uint linkPartId { get; }

  /// <summary>The persisted index of the module on the part of the other peer.</summary>
  /// <value>The module index or <c>-1</c>.</value>
  /// <remarks>This value must be available during the vessel loading.</remarks>
  int linkModuleIndex { get; }

  /// <summary>
  /// Transform that defines the position and orientation of the base node to which all the
  /// renderers and physical anchors are aligned.
  /// </summary>
  /// <value>Game object transformation. It's never <c>null</c>.</value>
  Transform nodeTransform { get; }

  /// <summary>
  /// Attach node to use when the peers need to couple into a single parts hierarchy.
  /// </summary>
  /// <remarks>
  /// The node is not required to be in the list of the attach nodes of the part. The caller must
  /// ensure it before doing the actual coupling.
  /// </remarks>
  /// <value>The attach node or <c>null</c> if the peer doesn't support coupling.</value>
  /// <seealso cref="ILinkJoint.SetCoupleOnLinkMode"/>
  /// <seealso cref="IAttachNodesUtils.AddNode"/>
  /// <seealso cref="ILinkUtils.CoupleParts"/>
  AttachNode coupleNode { get; }

  /// <summary>Parsed attach node definition of the peer.</summary>
  /// <remarks>
  /// This node is required to be fully setup on the part creation. The node must belong to the
  /// part, but it's not required to be listed in the part's attach nodes list. 
  /// </remarks>
  /// <value>The attach node. It's never <c>null</c>.</value>
  AttachNode attachNode { get; }

  /// <summary>Tells if this peer is currectly linked to another peer.</summary>
  /// <value>The current state of the link.</value>
  /// <seealso cref="linkState"/>
  bool isLinked { get; }

  /// <summary>Tells if the peer's link ability is disabled.</summary>
  /// <value>The locked state.</value>
  /// <seealso cref="linkState"/>
  /// <seealso cref="isNodeBlocked"/>
  bool isLocked { get; }

  /// <summary>Tells if the peer's attach node is occupied by an incpompatible part.</summary>
  /// <value>
  /// <c>true</c> if the state is <see cref="LinkState.NodeIsBlocked"/>, or <c>false</c>, otherwise.
  /// </value>
  /// <seealso cref="linkState"/>
  /// <seealso cref="isLocked"/>
  bool isNodeBlocked { get; }
}

}  // namespace
