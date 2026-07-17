
https://github.com/user-attachments/assets/14c7ba56-85a1-44fe-8115-23210f913ec2

https://github.com/user-attachments/assets/065534a0-570d-4669-aeb3-a4728e850753

https://github.com/user-attachments/assets/b4424630-caf4-4394-9755-0ab6e2b09969
# Attentional Tunnelling in Mixed Reality (XR)

## Purpose of the Research
Attentional Tunnelling (also known as cognitive tunnelling or inattentional blindness) is a psychological phenomenon where an individual becomes hyper-focused on a primary task or central focal area, causing them to completely fail to perceive unexpected but highly salient peripheral events or hazards.

This project is a Unity-based Mixed Reality (MR) research application designed to measure and analyze attentional tunnelling in a gym-scale environment. By manipulating the spatial placement and depth of a primary visual task, the study evaluates how different Augmented Reality (AR) UI paradigms affect a user's situational awareness, spatial navigation, and reaction times to physical hazards. 

## The Study Task
Participants in this study are required to balance two competing tasks while physically walking through a room:

1. **The Primary Cognitive Task (Visual Task)**: 
   As the user walks, they are presented with dynamic AR line charts. The line charts refresh at a fixed **4.0-second interval**, generating a new set of data points across two separate lines. Exactly one data point will be the absolute maximum across both lines. The user's primary task is to rapidly scan the data, identify which line contains the maximum value, and record their guess by pressing the corresponding face button on their VR controller (X or A). 
   * **Making a Selection**: When the user presses a button, their guess accuracy and reaction time are recorded, and the selected line is visually highlighted. The chart refreshes after the 4.0-second timer expires.
   * **Timeout (No Selection)**: If the user fails to make a selection before the 4.0-second interval expires, a "Missed" guess is logged. The chart refreshes after the 4.0-second timer expires.

2. **The Awareness Task (Hazard Detection)**: 
   While the user is fixated on the line charts, simulated physical hazards will occasionally spawn in their vision. The user must maintain enough situational awareness to notice these hazards. When a hazard is spotted, the user must immediately pull their controller's index trigger to "react" and destroy it. 

## Project Implementation & Architecture

### 1. Spatial Anchoring & Routing
* **`DimensionVisualiser` & `StudyManager`**: Utilises the Meta XR SDK to load user-placed physical spatial anchors, defining a safe, boundaryless walkable arena. The system procedurally generates and highlights navigation routes between these physical anchors to guide the participant through the physical space.
* **`SpatialAligner`**: A mathematical utility that translates raw headset and controller tracking coordinates into a "Standardised Space." By establishing the first two spatial anchors as a strict origin and Z-forward axis (ignoring height variations), it ensures that all collected telemetry data remains perfectly consistent across different participants, regardless of how the physical anchors were mapped in the real world.

### 2. The Primary Task Visual
* **`CanvasAnchorBehaviour`**: The UI for the visual line charts shifts between 4 specific spatial paradigms (Conditions) to manipulate where the user must focus:
  * **Peripersonal**: Attached directly to the user's hand/controller.
 

https://github.com/user-attachments/assets/f65d5d57-1a09-40d2-8f94-ae21dd62ea50

    
  * **Focal**: Locked to the user's direct gaze at a set depth.
    


https://github.com/user-attachments/assets/d2da2528-2870-4f90-a171-187f3c03a67a



  * **Action**: Placed at a fixed distance, mapped onto the ground and following the user's body rotation (headset yaw).



https://github.com/user-attachments/assets/d0087dcc-e9a4-4c98-b8bd-c72111c61e11


    
  * **Ambient**: World-locked along the route midpoints in the physical room.





https://github.com/user-attachments/assets/d4e4edd8-e9e2-46e1-a2f3-51f7b1700fb3


  * *(A fifth **Trial Mode** enables all paradigms simultaneously for onboarding).*
  * The line chart updates every 4 seconds (configurable) regardless of the selection.
### 3. Hazard Simulation 
The hazard system is strictly controlled to ensure consistent testing parameters:
* **Spawning Logic**: 
  * **Interval**: A new hazard attempts to spawn at a randomised interval between **4.0 and 10.0 seconds**.
  * **Location**: Hazards spawn exactly **10 meters** away from the user, set precisely at the user's calibrated eye level.
  * **Field of View**: They spawn within a **110-degree arc** (-55 to +55 degrees) relative to the forward vector of the route the user is currently walking.
  * **Boundaries**: Before spawning, the system verifies that the calculated spawn point falls within the physical arena bounds.
* **Movement & Interception**: 
  When a hazard spawns, it takes on one of three quota-based speed profiles and uses quadratic equations to perfectly calculate an interception trajectory with the walking user.
  * **Static**: 0 km/h (0 m/s). Representing static tripping hazards.
  * **Slow**: 4.5 km/h (1.25 m/s). Representing pedastrains
  * **Fast**: 15.0 km/h (~4.17 m/s). Representing faster road users like cars.
* **Visuals & Size**: The hazard is represented by a red, 1m x 1m x 1m sphere.

### 4. Telemetry & Data Logging
* **`StudyFlowManager`**: A robust State Machine (`Idle`, `Prepared`, `Starting`, `Running`, `Completed`) that manages the transitions of the study, driven by researcher inputs.
* **`CentralDataLogger`**: A configuration-driven backend. Upon launch, it reads from `UserInfo.csv` and `StudySchedule.csv` to determine the user's specific condition sequence. When a condition concludes, data is flushed to three separate `{UID}_{Condition}_[Type].csv` files:

  **A. Hazard Logs (`_HazardLog.csv`)**
  Records every discrete hazard event (Reaction, Miss, or False Positive).
  * **Event Data**: Timestamp (elapsed condition time), Event Type, Time to React (seconds), and Hazard Speed Profile.
  * **Spatial Data**: The user's Raw and Aligned Headset Positions/Rotations at the exact moment of the event, alongside the hazard's Raw and Aligned Positions.
  * **Field of View**: The precise Horizontal and Vertical FOV angles of the hazard relative to the user's direct gaze when the event occurred.

  **B. Visual Logs (`_VisualLog.csv`)**
  Records every guess interaction made during the primary cognitive task.
  * **Task Data**: Timestamp, the exact array of generated values for Line 1 and Line 2, the Truth Line (correct answer), and the User's Guess (1, 2, or -1 for timeout).
  * **Performance**: The exact Time Taken (in seconds) to submit the guess.

  **C. Telemetry Logs (`_TelemetryLog.csv`)**
  Records continuous, high-frequency spatial tracking data every physics frame (`FixedUpdate`).
  * **Absolute Tracking**: The user's Raw X/Y/Z Position, Raw Quaternion Rotation, and Raw Gaze Forward Vector in absolute world space.
  * **Standardised Tracking**: The user's Aligned X/Y/Z Position, Aligned Quaternion Rotation, and Aligned Gaze Forward Vector, normalised against the physical room's anchor geometry to ensure identical coordinate spaces across all study participants.

### 5. Subjective Data

* NASA-TLX and Borg Scale, plus any additional comments in a post-hoc questionaire.
