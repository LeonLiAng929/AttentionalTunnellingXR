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
* **`DimensionVisualiser` & `StudyManager`**: Utilizes the Meta XR SDK to load user-placed physical spatial anchors, defining a safe, boundaryless walkable arena. The system procedurally generates and highlights navigation routes between these physical anchors to guide the participant through the physical space.
* **`SpatialAligner`**: A mathematical utility that translates raw headset and controller tracking coordinates into a "Standardized Space." By establishing the first two spatial anchors as a strict origin and Z-forward axis (ignoring height variations), it ensures that all collected telemetry data remains perfectly consistent across different participants, regardless of how the physical anchors were mapped in the real world.

### 2. The Primary Task Visual
* **`CanvasAnchorBehaviour`**: The UI for the visual line charts shifts between 4 specific spatial paradigms (Conditions) to manipulate where the user must focus:
  * **Peripersonal**: Attached directly to the user's hand/controller.
  * https://github.com/user-attachments/assets/f65d5d57-1a09-40d2-8f94-ae21dd62ea50
  * **Focal**: Locked to the user's direct gaze at a set depth.
  * https://github.com/user-attachments/assets/d2da2528-2870-4f90-a171-187f3c03a67a
  * **Action**: Placed at a fixed distance, mapped onto the ground and following the user's body rotation (headset yaw).
  * https://github.com/user-attachments/assets/d0087dcc-e9a4-4c98-b8bd-c72111c61e11
  * **Ambient**: World-locked along the route midpoints in the physical room.
  * https://github.com/user-attachments/assets/d4e4edd8-e9e2-46e1-a2f3-51f7b1700fb3
  * *(A fifth **Trial Mode** enables all paradigms simultaneously for onboarding).*
  * The line chart updates every 4 seconds (configurable) regardless of the selection.
### 3. Hazard Simulation 
The hazard system is strictly controlled to ensure consistent testing parameters:
* **Spawning Logic**: 
  * **Interval**: A new hazard attempts to spawn at a randomized interval between **4.0 and 10.0 seconds**.
  * **Location**: Hazards spawn exactly **10 meters** away from the user, set precisely at the user's calibrated eye level.
  * **Field of View**: They spawn within a **110-degree arc** (-55 to +55 degrees) relative to the forward vector of the route the user is currently walking.
  * **Boundaries**: Before spawning, the system verifies that the calculated spawn point falls within the physical arena bounds.
* **Movement & Interception**: 
  When a hazard spawns, it takes on one of three quota-based speed profiles and uses quadratic equations to perfectly calculate an interception trajectory with the walking user.
  * **Static**: 0 km/h (0 m/s). Representing static tripping hazards.
  * **Slow**: 4.5 km/h (1.25 m/s). Representing pedastrains
  * **Fast**: 15.0 km/h (~4.17 m/s). Rrepresenting faster road users like cars.
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
  * **Standardized Tracking**: The user's Aligned X/Y/Z Position, Aligned Quaternion Rotation, and Aligned Gaze Forward Vector, normalized against the physical room's anchor geometry to ensure identical coordinate spaces across all study participants.

* Finally, the user's progression history is appended back to `UserInfo.csv` to seamlessly track completion and resume the study later.

## Revised Study Design Alternatives (Discussion Draft)

### Research framing

The study investigates dual-task performance while participants are running. Participants perform the MR line-chart task while detecting and responding to abstract hazards. 

The four MR content-placement strategies are inspired by Previc's model of three-dimensional interaction spaces:

* **Peripersonal:** hand-attached content in near-body visuomotor space.
* **Focal:** gaze-centred content supporting visual search and object recognition.
* **Action:** ground-referenced content ahead of the participant in locomotor/action space.
* **Ambient:** world-locked content in earth-fixed environmental space.


### Research questions

1. **Placement and hazard distance:** How do visualisation positioning strategy affect people's ability to detect and respond to hazard at different distances (depths) while running?
2. **Hazard movement speed:** How does hazard movement speed affect detection and response performance while the user is also moving?

### Study controls

* **Participant pace:** Running speed is self-selected, recorded, and not experimentally manipulated. It should be analysed continuously, separating each participant's typical speed from event-level deviations, rather than dividing participants into arbitrary high- and low-speed groups.
* **Spatial balance:** The five repetitions of each hazard configuration are balanced across far left, near left, centre, near right, and far right path-relative positions. The angular range should be within the FOV of the headset.
* **Saliency:** Should we equalise the angular size or physical size of the hazards?

### Alternative A: Two-stage design

#### Stage 1 — Static hazards

* Four visualisation positionings × three static onset distances, e.g., 3, 6, and 9 m.
* Estimate the effects of visualisation, distance, and their interaction.
* Select the vis positioning with the strongest hazard detection while maintaining non-inferior chart accuracy.
* 5 repetitions of each configuration, balanced across path-relative positions.
#### Stage 2 — Moving hazards

* Compare static, low-speed, and high-speed hazards at a common 9 m onset distance, which move towards the participant's current position.
* 5 repetitions of each configuration, balanced across path-relative positions.
### Alternative B: Combined design

The combined within-participant study uses five hazard configurations:

1. Static at 3 m.
2. Static at 6 m.
3. Static at 9 m.
4. Low-speed moving at 9 m.
5. High-speed moving at 9 m.

Each participant encounters five instances of every configuration under each visualisation:

`5 configurations × 5 repetitions × 4 visualisations = 100 hazard events per participant`

Two overlapping analyses are preregistered:

* **Static-distance analysis:** Visualisation × Static Distance (3, 6, and 9 m).
* **Hazard-speed analysis:** Visualisation × Hazard Speed (static, low, and high at 9 m).

### Decisions to be made

1. Select the two-stage or combined alternative.
2. Define the five spatial angle ranges (or maybe fixed angles like -45, -20, 0, 20, 45?).
4. Choose equal angular size or physical size.

### Supporting literature

* Previc, F. H. (1998). [The neuropsychology of 3-D space](https://pubmed.ncbi.nlm.nih.gov/9747184/). *Psychological Bulletin, 124*(2), 123–164.
* Patla, A. E., & Vickers, J. N. (2003). [How far ahead do we look when required to step on specific locations in the travel path during locomotion?](https://pubmed.ncbi.nlm.nih.gov/12478404/) *Experimental Brain Research, 148*, 133–138.
* Cullen, M. M., et al. (2020). [Gaze-behaviors of runners in a natural, urban running environment](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0233158). *PLOS ONE, 15*(5), e0233158.
* Syiem, B. V., et al. (2021). [Impact of Task on Attentional Tunneling in Handheld Augmented Reality](https://doi.org/10.1145/3411764.3445580). *CHI '21*.
* Song, J., et al. (2023). [Peripheral target detection can be modulated by target distance but not attended distance in 3D space simulated by monocular depth cues](https://doi.org/10.1016/j.visres.2022.108160). *Vision Research, 204*, 108160.
