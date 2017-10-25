﻿// Kerbal Attachment System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KASAPIv1;
using KSPDev.GUIUtils;
using KSPDev.KSPInterfaces;
using KSPDev.PartUtils;
using System;
using UnityEngine;

namespace KAS {

/// <summary>
/// Module that ties two parts with a cable. It allows any movement that doesn't try to increase the
/// maximum cable length.
/// </summary>
/// <remarks>
/// It can link either parts of the same vessel or parts of two different vessels.
/// </remarks>
public sealed class KASModuleCableJoint : KASModuleJointBase,
    // KAS interfaces.
    IKasJointEventsListener, IHasContextMenu,
    // KSPDev sugar interfaces.
    IsPhysicalObject {

  #region Localizable strings
  /// <include file="SpecialDocTags.xml" path="Tags/Message0/*"/>
  static readonly Message NotStretchedMsg = new Message(
      "#kasLOC_06000",
      defaultTemplate: "Cable is not stretched",
      description: "Message to show when cable stretch is checked, and it's close to zero.");

  /// <include file="SpecialDocTags.xml" path="Tags/Message1/*"/>
  /// <include file="KSPDevUtilsAPI_HelpIndex.xml" path="//item[@name='T:KSPDev.GUIUtils.PercentType']/*"/>
  static readonly Message<PercentType> StretchRatioMsg = new Message<PercentType>(
      "#kasLOC_06001",
      defaultTemplate: "Cable stretch: <<1>>",
      description: "Message to report the cable stretch ratio when it's not zero."
      + "\nArgument <<1>> is a ratio between the joint limit and the actual length.",
      example: "Cable stretch: 1.25%");
  #endregion

  #region Part's config fields
  /// <summary>
  /// Force per one meter of the stretched cable to apply to keep the object close to each other.
  /// </summary>
  /// <remarks>A too high value may result in the joint destruction.</remarks>
  /// <include file="SpecialDocTags.xml" path="Tags/ConfigSetting/*"/>
  [KSPField]
  public float cableStrength = 1000f;

  /// <summary>Force to apply to damper oscillations.</summary>
  /// <include file="SpecialDocTags.xml" path="Tags/ConfigSetting/*"/>
  [KSPField]
  public float cableSpringDamper = 1f;
  #endregion

  /// <summary>Threshold for determining if there is no cable stretch.</summary>
  const float MinViableStretch = 0.0001f;

  /// <summary>Intermediate game object used to keep the joints.</summary>
  GameObject jointObj;

  /// <summary>Actual joint object.</summary>
  ConfigurableJoint springJoint;

  /// <summary>Renderer for the link. Can be <c>null</c>.</summary>
  ILinkRenderer renderer;

  /// <summary>Maximum allowed distance between the linked objects.</summary>
  float maxJointDistance {
    get { return springJoint.linearLimit.limit; }
  }

  /// <summary>Gets current distance between the joint ends.</summary>
  float currentJointDistance {
    get {
      return Vector3.Distance(
          linkTarget.part.rb.transform.TransformPoint(springJoint.anchor),
          springJoint.connectedBody.transform.TransformPoint(springJoint.connectedAnchor));
    }
  }

  #region IHasContextMenu implementation
  /// <inheritdoc/>
  public void UpdateContextMenu() {
    PartModuleUtils.SetupEvent(
        this, CheckCableStretchContextMenuAction, e => e.active = isLinked);
  }
  #endregion

  #region IsPhysicalObject implementation
  /// <inheritdoc/>
  public void FixedUpdate() {
    if (renderer != null) {
      // Adjust texture on the cable to simulate stretching.
      var jointLength = maxJointDistance;
      var length = currentJointDistance;
      renderer.stretchRatio = length > jointLength ? length / jointLength : 1.0f;
    }
  }
  #endregion

  #region PartModule overrides 
  /// <inheritdoc/>
  public override void OnStart(PartModule.StartState state) {
    base.OnStart(state);
    UpdateContextMenu();
  }
  #endregion

  #region KASModuleJointBase overrides
  /// <inheritdoc/>
  public override bool CreateJoint(ILinkSource source, ILinkTarget target) {
    var res = base.CreateJoint(source, target);
    if (res) {
      renderer = part.FindModuleImplementing<ILinkRenderer>();
      CreateDistanceJoint(source, target);
      UpdateContextMenu();
    }
    return res;
  }

  /// <inheritdoc/>
  public override void DropJoint() {
    base.DropJoint();
    Destroy(springJoint);
    springJoint = null;
    Destroy(jointObj);
    jointObj = null;
    renderer = null;
    UpdateContextMenu();
  }

  /// <inheritdoc/>
  public override void AdjustJoint(bool isUnbreakable = false) {
    springJoint.breakForce =
        isUnbreakable ? Mathf.Infinity : GetClampedBreakingForce(linkBreakTorque);
    springJoint.breakTorque =
        isUnbreakable ? Mathf.Infinity : GetClampedBreakingTorque(linkBreakForce);
  }
  #endregion

  #region IKasJointEventsListener implementation
  /// <inheritdoc/>
  public void OnKASJointBreak(GameObject hostObj, float breakForce) {
    linkSource.BreakCurrentLink(LinkActorType.Physics);
  }
  #endregion

  #region GUI action handlers
  /// <summary>
  /// Context menu action that triggers current stretch ration check. Result is reported to UI.
  /// </summary>
  [KSPEvent(guiName = "Check cable stretch", guiActive = true, guiActiveUnfocused = true)]
  public void CheckCableStretchContextMenuAction() {
    var stretchRatio = GetCableStretch();
    if (stretchRatio <= MinViableStretch) {
      ScreenMessages.PostScreenMessage(
          NotStretchedMsg, ScreenMessaging.DefaultMessageTimeout, ScreenMessageStyle.UPPER_LEFT);
    } else {
      ScreenMessages.PostScreenMessage(
          StretchRatioMsg.Format(stretchRatio * 100),
          ScreenMessaging.DefaultMessageTimeout, ScreenMessageStyle.UPPER_LEFT);
    }
  }
  #endregion

  /// <summary>Returns ratio of the current cable stretch.</summary>
  /// <returns>
  /// <c>0</c> if cable is not stretched. Percentile of the stretching otherwsie. I.e. if cable's
  /// original length was <c>100</c> and the current length is <c>110</c> then stretch ratio is
  /// <c>0.1</c> (10%).
  /// </returns>
  public float GetCableStretch() {
    var stretch = currentJointDistance - maxJointDistance;
    if (stretch < MinViableStretch) {
      return 0f;
    }
    return stretch / maxJointDistance;
  }

  #region Local utility methods
  /// <summary>Creates a distance joint between the source and the target.</summary>
  void CreateDistanceJoint(ILinkSource source, ILinkTarget target) {
    jointObj = new GameObject("RopeConnectorHead");
    jointObj.AddComponent<BrokenJointListener>().hostPart = part;
    // Joints behave crazy when the connected rigidbody masses differ to much. So use the average.
    var rb = jointObj.AddComponent<Rigidbody>();
    rb.mass = (source.part.mass + target.part.mass) / 2;
    rb.useGravity = false;

    // Temporarily align to the source to have the spring joint remembered zero length.
    jointObj.transform.parent = source.physicalAnchorTransform;
    jointObj.transform.localPosition = Vector3.zero;

    springJoint = jointObj.AddComponent<ConfigurableJoint>();
    springJoint.enableCollision = true;
    springJoint.enablePreprocessing = false;
    KASAPI.JointUtils.ResetJoint(springJoint);
    KASAPI.JointUtils.SetupDistanceJoint(
        springJoint,
        springForce: cableStrength,
        springDamper: cableSpringDamper,
        maxDistance: originalLength);
    springJoint.breakTorque = GetClampedBreakingTorque(linkBreakForce);
    springJoint.breakForce = GetClampedBreakingForce(linkBreakTorque);
    springJoint.autoConfigureConnectedAnchor = false;
    springJoint.anchor = Vector3.zero;
    springJoint.connectedBody = source.part.Rigidbody;
    springJoint.connectedAnchor = source.part.Rigidbody.transform.InverseTransformPoint(
        source.physicalAnchorTransform.position);
    
    // Move plug head to the target and adhere it there at the attach node transform.
    jointObj.transform.parent = target.physicalAnchorTransform;
    jointObj.transform.localPosition = Vector3.zero;
    var fixedJoint = jointObj.AddComponent<ConfigurableJoint>();
    springJoint.enableCollision = true;
    springJoint.enablePreprocessing = true;
    KASAPI.JointUtils.ResetJoint(fixedJoint);
    KASAPI.JointUtils.SetupFixedJoint(fixedJoint);
    fixedJoint.autoConfigureConnectedAnchor = false;
    fixedJoint.anchor = Vector3.zero;
    fixedJoint.connectedBody = target.part.Rigidbody;
    fixedJoint.connectedAnchor = target.part.Rigidbody.transform.InverseTransformPoint(
        target.physicalAnchorTransform.position);
    fixedJoint.breakForce = Mathf.Infinity;
    fixedJoint.breakTorque = Mathf.Infinity;
    jointObj.transform.parent = jointObj.transform;
  }
  #endregion
}

}  // namespace
