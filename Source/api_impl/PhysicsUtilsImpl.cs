﻿// Kerbal Attachment System API
// Mod idea: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// API design and implemenation: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KASAPIv2;
using UnityEngine;

namespace KASImpl {

class PhysicsUtilsImpl : IPhysicsUtils {
  /// <inheritdoc/>
  public void ApplyGravity(Rigidbody rb, Vessel vessel, double rbAirDragMult = 1.0) {
    // Apply the gravity as it's done in FlightIntegrator for the physical object.
    var geeForce = FlightGlobals.getGeeForceAtPosition(vessel.CoMD, vessel.mainBody)
                   + FlightGlobals.getCoriolisAcc(vessel.velocityD, vessel.mainBody)
                   + FlightGlobals.getCentrifugalAcc(vessel.CoMD, vessel.mainBody);
    rb.AddForce(geeForce * PhysicsGlobals.GraviticForceMultiplier, ForceMode.Acceleration);
    // Apply the atmosphere drag force as it's done in FlightIntegrator for the physical object.
    if (PhysicsGlobals.ApplyDrag && vessel.atmDensity > 0) {
      var pseudoReDragMult = 1; //FIXME: find out what it is
      var d = 0.0005 * pseudoReDragMult * vessel.atmDensity * rbAirDragMult
          * (rb.velocity + Krakensbane.GetFrameVelocity()).sqrMagnitude
          * (double)PhysicsGlobals.DragMultiplier;
      if (!double.IsNaN(d) && !double.IsInfinity(d)) {
        var atmDragForce = -(rb.velocity + Krakensbane.GetFrameVelocity()).normalized * d;
        if (PhysicsGlobals.DragUsesAcceleration) {
          rb.AddForce(atmDragForce, ForceMode.Acceleration);
        } else {
          rb.AddForce(atmDragForce, ForceMode.Force);
        }
      }
    }
  }
}

}  // namespace
