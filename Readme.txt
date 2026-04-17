First-Person Rig Setup Guide
============================

This guide explains how to set up the first-person arm rig and punch system used by
`FirstPersonPunchRig` in this project.

Prerequisites
-------------
1. Unity project opened successfully.
2. Package Manager includes:
   - Animation Rigging
3. A first-person arm model in your scene (for example: `Assets/fpArms/fpArms.fbx`).

Scene Setup
-----------
1. Create or use your Player root GameObject.
2. Add these components to the player:
   - `CharacterController`
   - `PlayerController`
3. Add a child Camera and attach:
   - `CameraLook`
4. Place the arm mesh as a child of the camera so it follows view movement.

Rig Hierarchy Setup (Animation Rigging)
---------------------------------------
1. On the arm root object, add:
   - `RigBuilder`
2. Create a child object named `ArmRig` and add:
   - `Rig` component
3. Under `ArmRig`, create IK targets and hints:
   - `LeftHandTarget`
   - `RightHandTarget`
   - `LeftElbowHint`
   - `RightElbowHint`
4. On `ArmRig`, create Two Bone IK constraints for each arm:
   - `LeftArmIK` (Root/Mid/Tip = left shoulder/elbow/hand bones)
   - `RightArmIK` (Root/Mid/Tip = right shoulder/elbow/hand bones)
5. Assign each constraint's:
   - Target (LeftHandTarget / RightHandTarget)
   - Hint (LeftElbowHint / RightElbowHint)
6. Add `ArmRig` to `RigBuilder` layers.

Attach FirstPersonPunchRig
--------------------------
1. Add `FirstPersonPunchRig` to the first-person arm rig root.
2. In the inspector, assign:
   - Rig
     - `Arm Rig` -> the `Rig` on `ArmRig`
     - `Spine` -> the bone that should rotate slightly during punches
     - `Left Arm IK` -> left Two Bone IK constraint
     - `Right Arm IK` -> right Two Bone IK constraint
   - IK Targets
     - `Left Hand Target`
     - `Right Hand Target`
3. In the component context menu, click **Cache Rest Pose** once targets are in the desired idle position.

Punch Tuning
------------
Use these fields in `FirstPersonPunchRig` to shape the motion:
- Timing
  - `Wind Up Duration`
  - `Strike Duration`
  - `Recover Duration`
  - `Punch Cooldown`
- Shape
  - `Forward Distance`
  - `Inward Distance`
  - `Upward Distance`
  - `Wind Up Back Distance`
- Spine
  - `Spine Pitch`
  - `Spine Yaw`
- Curves
  - `Wind Up Curve`
  - `Strike Curve`
  - `Recover Curve`

Runtime Controls
----------------
- Left mouse button (`Mouse0`) triggers punch input.
- Punches alternate right and left hands automatically.
- Cooldown prevents immediate retriggering.

Quick Verification Checklist
----------------------------
1. Enter Play mode.
2. Move camera to confirm arms follow first-person view.
3. Click left mouse button:
   - Right and left punches alternate.
   - Hand returns to rest pose after punch.
   - Spine applies a subtle punch rotation.
4. If hand snaps to wrong position, stop play mode and run **Cache Rest Pose** again.

Troubleshooting
---------------
- Arms do not move:
  - Confirm Animation Rigging package is installed.
  - Confirm `RigBuilder` exists and contains the `ArmRig` layer.
  - Confirm IK constraint weights are > 0.
- Punch throws null reference:
  - Verify all references in `FirstPersonPunchRig` are assigned.
- Punch motion looks mirrored/wrong side:
  - Recheck left/right hand target assignments.
- Body does not rotate with mouse:
  - Ensure `CameraLook.playerBody` is assigned to player root.

Notes
-----
- `FirstPersonPunchRig` restores hand/spine rest values in `OnDisable`, which helps prevent pose drift when disabling the component.
- Keep punch distances small for first-person readability and to reduce clipping with the camera.
