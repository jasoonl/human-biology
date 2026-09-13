# The Absolute Architecture & Code Injection Framework: Interactive 3D Medical Explorer (Unity Pro)

## 1. Executive Vision, Pedagogical Modalities, & Product Strategy

**Strategic Overview**
This document outlines the end-to-end development, architecture, and deployment strategy for a 1:1 scale, medically accurate 3D anatomical simulation built in Unity 2023 LTS+. The application operates on a "Write Once, Scale Everywhere" philosophy, dynamically adjusting rendering fidelity and memory footprints across High-End PC (HDRP), standalone VR (URP on Meta Quest/Vision Pro), and browser-based portals (WebGL/WebGPU).

**Advanced Pedagogical & Cognitive Modalities**
*   **Dual-Coding & Multimedia Learning Theory:** By presenting spatial 3D models alongside synchronized auditory explanations (TTS) and written text, the software targets multiple cognitive channels simultaneously, minimizing cognitive overload and maximizing retention.
*   **Kinesthetic Spatial Assembly (Constructivism):** Users build spatial memory by physically dragging, dropping, and rotating bones and organs into place using 6DOF VR controllers or multi-touch inputs, rather than passively observing.
*   **Algorithmic Spaced Repetition (SRS):** Integrated SQLite databases track user failure rates on specific anatomical nodes. The system calculates an optimized Ebbinghaus decay curve to re-test the user exactly when they are statistically likely to forget the information.
*   **Gamified Clinical Scenarios:** Upper-level modules place students in timed, high-pressure triage scenarios (e.g., "Identify the source of the hemorrhage in the descending aorta") to bridge the gap between gross anatomy memorization and clinical application.

**Strict Regulatory, Security, & Compliance Framework**
*   **Data Sandboxing (HIPAA/FERPA/GDPR/PIPEDA):** The application is strictly air-gapped regarding patient data. When importing patient DICOM/MRI files, all volumetric rendering is processed locally on the GPU. No telemetry contains identifiable student or patient data.
*   **AES-256 Encryption at Rest:** All local analytics, save states, and session data are encrypted utilizing machine-specific hardware keys before being written to persistent storage.
*   **FDA SaMD Compliance & Liability:** The UI permanently renders a non-dismissible overlay during clinical modes stating: "Educational Reference Only - Not for Diagnostic or Surgical Execution," legally exempting the software from FDA 510(k) medical device clearance.
*   **SOC 2 Network Architecture:** For multiplayer "Classroom Modes," the architecture utilizes ephemeral, end-to-end encrypted relay servers. No chat logs, voice data, or spatial coordinates are logged on external servers.

---

## 2. Exhaustive Medical Ontology & Database Architecture

The software utilizes a dual-database approach: a read-only JSON schema containing the SNOMED-CT and FMA mapped anatomical dictionary, and a local SQLite database for mutable user data.

### 2.1 The Expanded Anatomical Dictionary (11 Systems & Micro-Dives)
1.  **Integumentary System:** Epidermis, Dermis, Subcutaneous layers down to the Stratum Basale. Eccrine/apocrine glands, pacinian corpuscles. *Pathologies:* Melanoma progression, Psoriasis, 3rd-Degree Burns.
2.  **Skeletal System & Articulations:** 206 bones, trabeculae, marrow, Haversian canals. *Pathologies:* Osteoarthritis, Osteoporosis, Compound Fractures, Osteosarcoma.
3.  **Muscular System:** 600+ muscles, epimysium/perimysium fascial layers. *Micro-Dive:* Sarcomere sliding filament theory (ATP/Calcium binding). *Pathologies:* Muscular Dystrophy, Tendonitis.
4.  **Nervous System (CNS/PNS):** Brain (Brodmann areas), spinal cord, sympathetic chain. *Micro-Dive:* Synaptic clefts (neurotransmitter exocytosis). *Pathologies:* Alzheimer's (Tau tangles), MS (demyelination), Parkinson's.
5.  **Cardiovascular System:** SA/AV node electrical pathways, 4 chambers, valves. *Micro-Dive:* Micro-bloodstream (RBCs, WBCs, platelets). *Pathologies:* Atherosclerosis, Thrombosis, Myocardial Infarction.
6.  **Respiratory System:** Tracheobronchial tree. *Micro-Dive:* Alveolar gas exchange gradients (O2/CO2 partial pressure). *Pathologies:* Emphysema, Asthma, COVID-19 ARDS.
7.  **Digestive System:** Esophagus to rectum, peristalsis mapping, liver lobules. *Pathologies:* Cirrhosis, Peptic Ulcers, Crohn's Disease, Appendicitis.
8.  **Renal & Urinary System:** Kidney cortex/medulla, ureters, bladder. *Micro-Dive:* Nephron glomerular filtration and Loop of Henle. *Pathologies:* Nephrolithiasis (Kidney Stones).
9.  **Lymphatic & Immune System:** Spleen, thymus, lymph nodes. *Micro-Dive:* Macrophage phagocytosis, T-Cell/B-Cell antibody production. *Pathologies:* Lymphoma, Autoimmune responses.
10. **Endocrine System:** Pituitary, thyroid, adrenal, Islets of Langerhans. *Micro-Dive:* Hormone receptor binding. *Pathologies:* Type 1 & 2 Diabetes, Hyperthyroidism.
11. **Reproductive System:** Male and female macro/micro anatomy, gametogenesis, embryonic development stages.

### 2.2 Advanced JSON Schema Architecture
```json
{
  "entityID": "SYS_CV_HEART_LV",
  "snomedCTCode": "27814002",
  "icd10PathologyCodes": ["I51.7", "I21.9"],
  "latinName": "Ventriculus sinister cordis",
  "commonName": "Left Ventricle",
  "systemCategory": ["Cardiovascular", "Muscular"],
  "descriptionProfessional": "The left lower chamber of the heart that receives oxygenated blood from the left atrium and pumps it out under high pressure through the aorta.",
  "descriptionPatient": "The main pumping chamber of your heart. It pushes oxygen-rich blood out to the rest of your body.",
  "boundsCenterOffset": {"x": 0.0, "y": -0.05, "z": 0.02},
  "idealCameraDistance": 0.15,
  "addressableMeshKey": "Mesh_Heart_LeftVentricle_LOD0",
  "audioTTSKey": "Audio_LeftVentricle_Desc",
  "connectedNodes": ["SYS_CV_HEART_LA", "SYS_CV_VALVE_AORTIC", "SYS_CV_VALVE_MITRAL"],
  "hasMicroDive": true,
  "microDiveKey": "Scene_Micro_Myocardium",
  "pharmacologyTargets": ["Lisinopril", "Metoprolol"]
}
```

---

## 3. Technical Budgets & Memory Pipelines

**Hardware-Targeted Performance Tiers**
1.  **Tier 1 (High-End PC/Hospital Workstations - HDRP):**
    *   *Poly Budget:* 25,000,000+ triangles.
    *   *Textures:* 4K/8K uncompressed atlases.
    *   *Shaders:* Volumetric Subsurface Scattering, Raytraced Ambient Occlusion, Screen Space Reflections.
2.  **Tier 2 (Standalone VR Quest 3/Vision Pro - URP):**
    *   *Poly Budget:* < 1,500,000 triangles (requires aggressive Addressable streaming).
    *   *Textures:* 2K ASTC compressed atlases.
    *   *Shaders:* Baked lighting, Mobile SSS approximation, Fixed Foveated Rendering.
3.  **Tier 3 (Browser WebGL/Chromebooks - URP/WebGPU):**
    *   *Poly Budget:* < 500,000 triangles.
    *   *Textures:* 1K DXT/Crunch compressed.
    *   *Shaders:* Unlit or simple Blinn-Phong, aggressive Quadric Error Metric (QEM) decimation.

**Unity Addressables Asset Streaming**
To prevent Out of Memory (OOM) crashes on Tier 2/3 devices, the massive 50GB project is chunked into 5MB-10MB Addressable groups. When a user requests the "Cardiovascular System," the engine dynamically downloads the chunk from an AWS S3 bucket (if not locally cached), instantiates it, and forces `Resources.UnloadUnusedAssets()` when the system is hidden.

---

## 4. The 100-Phase AI Code Injection Master-Grid

To build this massive architecture, feed these highly explicit prompts sequentially to your AI coding assistant (Claude, GPT-4, or Gemini). Verify compilation in Unity after each module.

### Module I: Bootstrapping, Data & Dependency Injection (1-10)
> **Phase 1: Subsystem Bootstrapping**
> "Write a C# `GameBootstrapper` using `UnityEngine.Subsystems`. Implement a Singleton `GameManager` that initializes `DataController`, `ViewController`, and `InputController`. Use `Task.WhenAll` to load these asynchronously so the main thread remains unblocked. Include `public enum GameState { Initializing, FreeRoam, MicroDive, QuizMode, AssemblyMode }`."

> **Phase 2: Custom Dependency Injection (DI)**
> "Write a strict DI framework for Unity. Create `[Inject] public class InjectAttribute : Attribute`. Write `DiContainer` that registers interfaces (e.g., `Bind<IDataController>().To<DataController>()`) and resolves them in the `Awake` phase of registered MonoBehaviours using `System.Reflection` to enforce SOLID principles."

> **Phase 3: JSON Deserialization Engine**
> "Write `AnatomyDataParser` using `Newtonsoft.Json`. Define an `AnatomyNode` class matching my schema (EntityID, LatinName, SnomedCode, BoundsOffset). Write `public async Task<Dictionary<string, AnatomyNode>> LoadDataAsync(string path)` that caches data into an O(1) lookup dictionary."

> **Phase 4: SQLite Database Initialization**
> "Write `DatabaseManager` using `Mono.Data.Sqlite`. On boot, create a local file `userdata.db`. Execute a SQL command to create a table `UserProgress` (NodeID VARCHAR PRIMARY KEY, CorrectStrikes INT, LastReviewed DATETIME, ReviewInterval FLOAT, EaseFactor FLOAT)."

> **Phase 5: Spaced Repetition Math (SM-2)**
> "Write `StudyTracker`. Expose `LogQuizResult(string nodeID, int qualityScore0to5)`. Implement the SuperMemo-2 algorithm: `newEase = oldEase + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02))`. Update the database `ReviewInterval` based on the ease factor and commit via SQL `UPDATE`."

> **Phase 6: Addressables Memory Manager**
> "Write `AssetStreamingManager` using `UnityEngine.AddressableAssets`. Create `public async Task<GameObject> LoadAnatomyGroup(string key)`. Cache the `AsyncOperationHandle` in a Dictionary. Write `UnloadAnatomyGroup` which calls `Addressables.Release(handle)` and triggers `Resources.UnloadUnusedAssets()`."

> **Phase 7: Cloud Asset Chunking (WebGL)**
> "Write `CloudStreamingManager`. For WebGL, Addressables load from a remote catalog. Write a Coroutine using `UnityWebRequest` that pings the remote AWS S3 bucket to check the manifest hash. If a new version exists, prompt the UI to download the updated asset bundles."

> **Phase 8: Automated Data Unit Tests (Moq)**
> "Write `DataControllerTests` using `NUnit.Framework` and `Moq`. Write a `[UnityTest]` that yields a Coroutine to load a dummy JSON file, asserting that the resulting dictionary is not null and `Count > 0`. Test that requesting an invalid ID throws a custom `AnatomyNotFoundException`."

> **Phase 9: Secure PlayerPrefs Manager**
> "Write `SecurePrefs`. Unity's PlayerPrefs are easily hacked. Write a wrapper class that encrypts string and float values using `System.Security.Cryptography.Aes` before writing to `PlayerPrefs.SetString`, ensuring local settings and progress cannot be externally modified."

> **Phase 10: Master State Machine**
> "Write `AppStateMachine`. Implement a state pattern with `IState` interfaces for `MainMenuState`, `MacroExplorationState`, `MicroSimulationState`, and `ClinicalQuizState`. Ensure robust transition logic that calls `Enter()`, `Execute()`, and `Exit()` cleanly."

### Module II: Advanced Input, Camera & Raycasting (11-22)
> **Phase 11: Input Action Maps (New Input System)**
> "Write `InputManager` referencing `UnityEngine.InputSystem`. Programmatically generate an Action Map with bindings for Mouse (Delta, Scroll, Left/Right Click), Touch (Primary Position, Secondary Position), and XR (Pinch, Grip, Trigger). Expose C# events for `OnPrimaryInteract`, `OnZoom`, and `OnOrbit`."

> **Phase 12: Smooth Orbital Camera Core**
> "Write `AdvancedOrbitalCamera`. Inputs: `Vector2 orbitDelta`, `float zoomDelta`. Use `Mathf.SmoothDamp` for position/rotation. Prevent Gimbal Lock: clamp Y-axis orbit between -85f and 85f. Add a spherecast collision check: if the camera hits 'Anatomy_Macro' layer, override zoom distance to the hit point minus 0.1f."

> **Phase 13: Multi-Touch Gesture Interpreter**
> "Write `TouchGestureInterpreter`. Hook into `InputManager`. If two fingers are detected, calculate the distance delta between frames. Map this delta to the camera's `zoomDelta`. Calculate the average midpoint movement of the two fingers and map it to the camera's `panDelta`."

> **Phase 14: Mesh Bounding-Box Auto-Focus**
> "Write `CameraFocusTargeter`. Given a `GameObject`, iterate `GetComponentsInChildren<Renderer>()` to calculate a composite `Bounds`. Calculate required distance: `distance = bounds.extents.magnitude / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)`. Lerp the `AdvancedOrbitalCamera.zoomDistance` to this exact value over 1.5 seconds (Ease-In-Out)."

> **Phase 15: Non-Allocating Physics Raycaster**
> "Write `AnatomyRaycaster`. On `OnPrimaryInteract`, execute `int hits = Physics.RaycastNonAlloc(ray, resultsArray, maxDistance, layerMask)`. Sort hits by distance. Ignore any hit where `MeshRenderer.material.GetFloat("_Alpha") < 0.2f`. Invoke `public static event Action<string> OnNodeSelected;` passing the ID."

> **Phase 16: Dynamic Bounding Volume Hierarchies (BVH)**
> "Write `BVHManager`. With 50M polygons, standard physics raycasts are too slow. Write a custom script that constructs a lightweight BVH of all active anatomy parts based on their AABB (Axis-Aligned Bounding Box). Intersect the ray with the BVH first before passing the subset to Unity's native `Physics.Raycast`."

> **Phase 17: Exploded View Matrix Math**
> "Write `ExplodedViewController`. On `Awake()`, for a parent object, iterate children. Calculate normalized outward vector: `(child.position - parent.position).normalized`. Cache `originalLocalPos` and `outwardDirection`. Write `UpdateExplosion(float slider0to1)` setting `child.localPosition = Vector3.Lerp(originalPos, originalPos + (dir * maxDist), slider0to1)`."

> **Phase 18: Cross-Section Slicing Tool Controller**
> "Write `RuntimeSlicer`. Create a draggable UI plane with a BoxCollider. In `Update()`, extract `plane.transform.position` and `plane.transform.up`. Push these to the GPU using `Shader.SetGlobalVector("_SlicePlanePosition", pos)` and `Shader.SetGlobalVector("_SlicePlaneNormal", norm)`."

> **Phase 19: Object Isolation & Ghosting**
> "Write `FocusModeController`. When an organ is double-clicked, find all other renderers in the scene. Use a `MaterialPropertyBlock` to lerp their Albedo alpha to 0.15f and their emission to black, 'ghosting' the rest of the body while keeping the selected organ at 1.0f opacity."

> **Phase 20: Spatial 3D Minimap Radar**
> "Write `MinimapController`. Create a secondary Orthographic Camera rendering a 'Minimap' layer (wireframe proxy). Render to a `RenderTexture` on a UI RawImage. Calculate relative position of Main Camera to body bounds, update a UI cone icon on the minimap to indicate viewing angle/zoom."

> **Phase 21: Camera Path Flythrough (Cinematics)**
> "Write `CinematicTourManager`. Utilize Unity Splines. Define a `SplineContainer` passing through the digestive tract. Write a Coroutine that mounts the Main Camera to the spline, updating position and `Transform.LookAt` along the spline's tangent vectors, yielding based on a serialized speed float."

> **Phase 22: Cutaway Masking Spheres**
> "Write `VolumetricCutawayController`. Instead of a flat slicing plane, allow the user to drag a sphere through the body. Pass the sphere's `transform.position` and `radius` to a global shader vector. Provide the HLSL logic to discard any pixels that fall within the radius of this sphere in world-space."

### Module III: Rendering, Shaders & Materials (23-38)
> **Phase 23: Subsurface Scattering (SSS) Controller**
> "Write `TissueMaterialController`. Cache a `MaterialPropertyBlock`. In `Update()`, set it via `renderer.SetPropertyBlock()`. Control the HDRP SSS profile by altering `_ThicknessMultiplier` and `_TransmissionIntensity` based on a UI slider, allowing real-time transition from opaque bone to fleshy tissue."

> **Phase 24: Boolean Clipping HLSL Node**
> "Provide the exact HLSL custom function node code required for the Shader Graph to clip fragments based on the global vectors set in Phase 18. The logic must calculate the dot product of the fragment's world position against the plane normal, calling `clip()` to discard pixels on the negative side."

> **Phase 25: Backface Cap Rendering Pass**
> "Write the Shader Graph setup for sliced organ caps. Create a second material (same mesh), Render Face = 'Front'. In fragment shader, ignore lighting, output solid color `#4A0000`. Apply the `_SlicePlane` clipping logic, but invert the normal, creating the illusion of a solid interior mass."

> **Phase 26: Dynamic Depth Peeling (Z-Sorting)**
> "Write `LayeredTransparencyController`. Standard transparency Z-fights. Store arrays of `Renderer`s categorized by system. Expose `SetSystemAlpha()`. When alpha < 1.0, swap Render Queue property: Skeleton to 3000, Muscles to 3010, Skin to 3020, guaranteeing inner structures render before outer transparent ones."

> **Phase 27: Muscle Flexion Vertex Shader (Soft Body Fake)**
> "Write `MuscleBulgeController` and Shader Graph logic. Monitor joint hinge angle via `Vector3.Angle(upperArm, forearm)`. Normalize to 0-1 float. Pass via `_BulgeAmount` to material. In Vertex Shader, multiply `_BulgeAmount` by vertex normal and a painted Vertex Color mask (center of bicep expands)."

> **Phase 28: ML-Agents FEM Soft Body (Advanced)**
> "Write `SoftBodyDeformer`. Rigid vertex shading isn't enough for surgical simulation. Integrate Unity ML-Agents to infer tissue tearing. Write a script that reads contact forces from a virtual scalpel and updates a localized spring-mass mesh deformation grid based on a pre-trained ONNX neural network model."

> **Phase 29: Real-time Decal Projection (Surgical Cuts)**
> "Write `DecalProjectorManager`. When simulating a procedure, instantiate a URP/HDRP Decal Projector at the exact raycast hit point of the virtual scalpel. Map an 'incision' texture. Dynamically align projector's forward vector to the `RaycastHit.normal` so it wraps over curved organic surfaces."

> **Phase 30: Volumetric Raymarching (DICOM/MRI)**
> "Write an HLSL Raymarching shader to visualize a `Texture3D`. Cast a ray from the camera through a cube bounds. Sample 3D texture at discrete intervals. Use a 1D ramp texture to map sampled Hounsfield Unit floats (e.g., 0.8 for bone) to specific RGBA colors, accumulating until alpha hits 1.0."

> **Phase 31: X-Ray Fresnel Effect Shader**
> "Write an 'X-Ray' Shader Graph. Calculate the dot product between the View Direction and the Surface Normal. Run through a Power node to isolate the rim. Add a procedural scrolling noise texture to the Emission channel to simulate radiological interference. Set Surface Type to Transparent, Blend Mode to Additive."

> **Phase 32: Cinematic Post-Processing Link**
> "Write `CinematicPostProcessManager`. Retrieve `Volume` component. Extract `Bloom` and `DepthOfField`. In `Update()`, map the `AdvancedOrbitalCamera.zoomDistance` to Bloom intensity using an inverse lerp. As camera gets extremely close (macro-photography), increase bloom/blur for an organic, wet presentation."

> **Phase 33: Colorblind Assist Filter**
> "Write `ColorblindFilterFeature` inheriting from `ScriptableRendererFeature`. In `Execute`, Blit camera color target to temp texture using custom material. Shader must implement Daltonization matrix math: shift red/green confusion lines of Protanopia into blue/yellow spectrum, increasing contrast between arteries and veins."

> **Phase 34: Compute Shader Fluid Dynamics (Bladder/Stomach)**
> "Write `FluidSimController`. Create `ComputeShader` defining a 2D Eulerian grid of velocity/density values. Dispatch with thread groups `(texWidth/8, texHeight/8, 1)`. Implement advection and projection steps for incompressible fluid. Write output to `RenderTexture` mapped to Albedo of a plane."

> **Phase 35: Procedural Capillary Generation**
> "Write `CapillaryGenerator`. Modeling microscopic vessels is impossible. Write a compute shader that utilizes a 3D Lindenmayer System (L-System) to procedurally generate branching capillary tubes radiating outward from the termination point of any major artery at runtime."

> **Phase 36: Blood Flow UV Scrolling**
> "Write a Shader Graph for veins and arteries. Multiply a scrolling noise texture along the V-axis of the UV coordinates. Expose a `_FlowSpeed` and `_PulseIntensity` parameter. Tie these parameters to the `CardioAnimationController` BPM to visually simulate blood rushing beneath the vessel walls."

> **Phase 37: GPU Instanced Point Clouds**
> "Write `PointCloudRenderer`. For raw MRI data points, meshes are too heavy. Read a CSV of Vector3 coordinates. Push these to a `ComputeBuffer`. Use `Graphics.DrawProceduralNow` inside `OnRenderObject` with a custom geometry shader that expands each point into a billboarded quad facing the camera."

> **Phase 38: Texture Channel Packing & Unpacking**
> "Write an Editor Script `TexturePacker`. Take a Metallic map, Ambient Occlusion map, Detail Mask, and Smoothness map. Combine them into the R, G, B, and A channels of a single `Texture2D` respectively. Save the asset to disk. This reduces texture memory overhead by 75% for mobile targets."

### Module IV: DOTS/ECS Microscopic Simulation (39-48)
> **Phase 39: Macro-to-Micro Scale Easing**
> "Write `ScaleDiveController`. We cannot shrink camera to 0.0001 scale. Write a Coroutine over 3s: 1) Fade screen to white via HDRP Volume. 2) Move camera to hidden 'Micro Scene' at (0,0,0) containing upscaled micro-models. 3) Change `nearClipPlane` to 0.01. 4) Unload Macro Addressables."

> **Phase 40: ECS Blood Flow Boilerplate**
> "Write ECS initialization for blood simulation. Define `public struct BloodCell : IComponentData { public float Speed; public float Progress; }`. Write an `IJobEntity` Burst-compiled job that updates `LocalTransform.Position` by calculating position along a Bezier curve based on `Progress`, looping back to 0."

> **Phase 41: ECS Compute Buffer Instancing**
> "Write `InstancedBloodRenderer`. Extract `LocalToWorld` matrices from ECS entity query into `NativeArray<Matrix4x4>`. Allocate `ComputeBuffer`. Use `Graphics.DrawMeshInstancedIndirect` passing mesh, material, and `argsBuffer` to render 100,000 red blood cells in a single GPU draw call."

> **Phase 42: ECS Collision & Phagocytosis (Immunology)**
> "Write an ECS System `ImmuneResponseSystem`. Introduce `WhiteBloodCell` and `Pathogen` entities. Use Unity Physics (DOTS) to detect proximity. If distance < threshold, lerp Pathogen position to WBC position, scale Pathogen to 0, and trigger a particle burst to simulate macrophage phagocytosis."

> **Phase 43: ECS Flocking Algorithm (WBC Search)**
> "Write a Boids/Flocking system in ECS. White blood cells must swarm toward a detected pathogen. Implement Cohesion, Alignment, and Separation jobs using the Burst compiler. Factor in a `PathogenAttraction` vector that pulls the swarm toward the infection site."

> **Phase 44: Receptor Binding Pharmacology Visualizer**
> "Write `PharmacologyVisualizer`. Instantiate 3D cell receptor and drug molecule. Write Coroutine that lerps molecule toward receptor (Ease-In). Once distance < 0.05f, trigger particle burst, change emission color of receptor to green to signify mechanism of action."

> **Phase 45: Alveolar Gas Exchange Particles**
> "Write `GasExchangeSystem` using Unity VFX Graph. Emit blue particles (CO2) from the blood vessel mesh toward the alveoli cavity. Emit red particles (O2) from the cavity into the vessel. Use Vector Fields to simulate the diffusion gradient based on partial pressure variables."

> **Phase 46: ATP Cross-Bridge Cycling Visualizer**
> "Write `SarcomereAnimator`. Create a highly detailed animation script utilizing `Mathf.Sin` loops. Visually demonstrate a Calcium ion binding to Troponin, shifting Tropomyosin. Move the Myosin head to attach to the Actin filament, and visually spawn an ATP molecule that triggers the 'power stroke'."

> **Phase 47: Neural Action Potential Particles**
> "Write `ActionPotentialVisualizer`. When simulating a nerve, instantiate a burst of fast-moving glowing particles along the outer bounds of the nerve mesh. Synchronize the particle speed to a `ConductionVelocity` variable to demonstrate myelinated vs. unmyelinated nerve signal propagation."

> **Phase 48: Neurotransmitter Exocytosis**
> "Write `SynapticCleftManager`. When the action potential reaches the axon terminal, trigger an animation of synaptic vesicles fusing with the presynaptic membrane. Emit specific colored particles (e.g., green for Dopamine, blue for Serotonin) that drift across the cleft and 'snap' to postsynaptic receptors."

### Module V: Gamification, UI & Spaced Repetition (49-62)
> **Phase 49: Dynamic 3D Billboarding & Repulsion**
> "Write `LabelManager`. Maintain `List<RectTransform>` of active UI labels pinned to 3D world coordinates. In `LateUpdate`, run nested loop comparing `RectTransformUtility.RectangleContainsScreenPoint`. If rects overlap, calculate vector between centers, add `repulsionForce * deltaTime` to screen-space positions."

> **Phase 50: Procedural Heartbeat Math (Audio/Visual Sync)**
> "Write `CardioAnimationController`. Expose `float targetBPM`. Calculate `phase = (Time.time * targetBPM / 60f) * Mathf.PI * 2f`. Use sine waves for Lub-Dub motion: `scale = Mathf.Max(0, Mathf.Sin(phase)*0.1f + Mathf.Sin(phase*2f)*0.05f)`. Link an `AudioSource` that pitch-shifts a `.wav` file based on BPM."

> **Phase 51: Pathology Progression Sliders**
> "Write `DiseaseProgressionManager`. Expose `SetPathologySeverity(string nodeID, float severity0to100)`. Use `skinnedMesh.SetBlendShapeWeight(index, severity)` to deform geometry (liver swelling). Use `MaterialPropertyBlock` to lerp `_BaseColor` from healthy red to diseased yellow-brown based on severity."

> **Phase 52: Interactive Gamified Quizzing**
> "Write `QuizController`. Call `StudyTracker.GetDueNodes()`. Display 'Locate [Anatomy]'. Start Coroutine timer (15s). Subscribe to `AnatomyRaycaster.OnNodeSelected`. If ID matches, instantiate UI checkmark, add points (`score = timeLeft * 10`), trigger next question. If wrong, deduct points, flash correct organ."

> **Phase 53: Streak Multipliers & Gamification**
> "Write `ScoreManager`. Extend the QuizController. Implement a streak multiplier (e.g., 3 correct in a row = 1.5x points). Hook this into a UI particle system that fires confetti when the multiplier increases, triggering dopamine loops for K-12 students."

> **Phase 54: Surgical Assembly Kinesthetics**
> "Write `AssemblyModeController`. Cache exact local positions of bone `GameObjects`. On start, randomize positions within `Random.insideUnitSphere * 2f`. Implement `IDragHandler`. If dragging and `Vector3.Distance(drag, correct) < 0.1f` AND `Quaternion.Angle < 15f`, snap object to correct transform and lock."

> **Phase 55: Voice Recognition Navigation (NLP)**
> "Write `VoiceCommandManager`. Utilize `UnityEngine.Windows.Speech.KeywordRecognizer`. Populate `string[]` with keys from JSON dictionary. In `OnPhraseRecognized` callback, take `args.text`, look up `AnatomyNode` ID, invoke `CameraFocusTargeter.FocusOn(nodeID)` for hands-free navigation."

> **Phase 56: PDF Fact Sheet Generation**
> "Write `InfoExporter`. When user clicks 'Export Info', compile `DescriptionPatient`, `Pathologies`, and current camera's `RenderTexture` view. Serialize data and utilize `iTextSharp` library to generate a formatted PDF fact sheet directly into the local Documents folder."

> **Phase 57: Text-To-Speech (TTS) Integration**
> "Write `AccessibilityManager`. Implement `ITextToSpeechProvider` using native OS speech APIs (e.g., `System.Speech.Synthesis`). Expose `Speak(string text)`. When user clicks anatomy node, pass `node.DescriptionPatient` to this method. Include `StopSpeaking()` interrupt for rapid clicking."

> **Phase 58: Audio Occlusion & Low-Pass Filtering**
> "Write `AudioEnvironmentController`. Place `AudioSource` on heart. In `Update()`, execute `Physics.Linecast` from Camera to AudioSource. Count hits on 'Anatomy_Macro' layer. For every layer of tissue, increase `cutoffFrequency` of an `AudioLowPassFilter` component to muffle sound."

> **Phase 59: Object Pooling (Zero Garbage Collection)**
> "Write `ObjectPooler`. Create `Dictionary<string, Queue<GameObject>>`. Write `InitializePool(prefab, count)` which instantiates items and calls `SetActive(false)`. Write `SpawnFromPool()` and `ReturnToPool()`. Refactor LabelManager to use this pool to maintain zero GC allocations."

> **Phase 60: UI Localization (I18n)**
> "Write `LocalizationManager` using `UnityEngine.Localization`. Create String Tables for English, Spanish, Mandarin. Expose `ChangeLanguage(localeCode)`. Write async method yielding `LocalizationSettings.InitializationOperation`, dynamically updating all `TextMeshProUGUI` components tied to `LocalizedString` listeners."

> **Phase 61: UI Scale Mode & Accessibility**
> "Write an Editor script that validates every `Canvas` in the project. Ensure the `CanvasScaler` component is set to 'Scale With Screen Size'. Verify that no UI text element uses a font size smaller than 14pt to ensure WCAG AA compliance across desktop and tablet formats."

> **Phase 62: Memory & FPS Profiling GUI**
> "Write `PerformanceMonitor`. Use `Profiler.GetTotalAllocatedMemoryLong()`. Calculate rolling average of `1.0f / Time.unscaledDeltaTime` (FPS). Output to UI Canvas. If memory > 2GB or frame times > 11ms, flash text red for developer auditing."

### Module VI: Multiplayer NGO & XR Interaction (63-75)
> **Phase 63: NGO Multi-User Initialization**
> "Write `ClassroomNetworkManager` inheriting `NetworkBehaviour` (Unity NGO). Create `NetworkVariable<FixedString32Bytes> FocusedAnatomyID`. Write `[ServerRpc(RequireOwnership = true)] void SetFocusServerRpc(string id)`. Attach `OnValueChanged` event so when server updates, Client cameras lerp to focus."

> **Phase 64: Client Latency Interpolation**
> "Write `NetworkTransformInterpolator`. Network ticks arrive at 30Hz, VR rendering at 120Hz. Maintain ring buffer of last 4 received `Vector3` network positions (teacher's laser pointer). Use Catmull-Rom spline equation in `Update()` to calculate smoothed position, eliminating network jitter."

> **Phase 65: Networked 3D Annotation (Batching)**
> "Write `NetworkedAnnotationTool`. Host draws lines on meshes. Collect points into `Vector3[]` (max size 20). Call `[ServerRpc] void DrawLineSegmentServerRpc(Vector3[] points)` to broadcast batch to clients, who append points to their local `LineRenderer` components."

> **Phase 66: Distributed Quiz Synchronization**
> "Write `DistributedQuizManager`. Host clicks 'Launch Quiz'. Call `[ClientRpc]` instantiating quiz UI on students. Students submit via `[ServerRpc(RequireOwnership = false)] void SubmitAnswerServerRpc(ulong clientID, bool isCorrect)`. Server tallies responses, pushes updated bar chart graphic back to Host."

> **Phase 67: Voice-over-IP (VoIP) Integration**
> "Write `VoiceChatManager` integrating with Photon Voice or Vivox. Create spatialized 3D audio sources mapped to the networked Player Prefabs, allowing the teacher's voice to emanate from their avatar's position in the virtual classroom environment."

> **Phase 68: Role-Based Access Control (RBAC)**
> "Write `NetworkAuthorityManager`. Restrict interactions. Only `NetworkManager.IsServer` (Teacher) can trigger `RuntimeSlicer` or `DiseaseProgressionManager`. Clients are restricted to local orbit and hover-highlighting unless explicitly granted permission via `[ServerRpc] RequestControl()`."

> **Phase 69: Networked State Snapshot Sync**
> "Write `GameStateSynchronizer`. When a late-joining student connects, the Server must package the current `SlicePlanePosition`, `DiseaseSeverity`, and `ExplodedSliderValue` into a `byte[]` payload and transmit it via a reliable sequenced channel to instantly synchronize the late client's visual state."

> **Phase 70: Ephemeral Relay Servers (Security)**
> "Configure Unity Relay Services. Write connection logic that generates a 6-digit alphanumeric join code. Ensure connections utilize DTLS encryption. Log connection states and handle network timeouts gracefully with a UI popup prompting the student to reconnect."

> **Phase 71: XR Hand Tracking Interaction**
> "Write `HandTrackingGrabInteractable` (XR Interaction Toolkit). Require Rigidbody on target. On `selectEntered` (Pinch), configure `FixedJoint` attaching object to interactor. On `selectExited` (Release), capture interactor's velocity/angular velocity, applying to Rigidbody for throwing physics."

> **Phase 72: Spatial Anchors for AR Passthrough**
> "Write `SpatialAnchorManager` (OpenXR). Expose method to instantiate model. On placement, call API to create persistent spatial anchor at real-world `Pose`. Save UUID string to `PlayerPrefs`. On boot, query XR environment for UUID and auto-align 3D scene to physical desk."

> **Phase 73: XR Haptic Feedback Mapping**
> "Write `HapticFeedbackManager`. Cache `XRBaseController`. Write `PlayHeartbeatHaptic(amplitude, duration)` synchronized with sine wave peak from Phase 50. Write `PlayScalpelHaptic()` triggering high-frequency impulse when `RuntimeSlicer` plane intersects mesh collider."

> **Phase 74: Fixed Foveated Rendering Optimization**
> "Write `EyeTrackingOptimizer`. Enable Fixed Foveated Rendering (FFR) via XR APIs. Set FFR level to `HighTop`. This drops pixel density in peripheral vision, freeing up GPU bandwidth to maintain strict 120Hz refresh rate (preventing simulator sickness)."

> **Phase 75: Mixed Reality Occlusion Mesh**
> "Write `AROcclusionManager`. Using the headset's depth API, generate a runtime occlusion mesh of the user's physical desk. Assign a depth-only material to this mesh. This allows virtual organs placed on the desk to properly disappear if they roll behind a physical monitor or coffee cup."

### Module VII: Medical Scans (DICOM) & AI Segmentation (76-85)
> **Phase 76: DICOM File Parser (Native C++ Plugin)**
> "C# is too slow for massive MRI datasets. Write a Native C++ `.dll` plugin `FastDicomParser`. Export a method that takes a directory path, parses the 16-bit `.dcm` binaries natively, and returns a pointer to a flattened 1D array of Hounsfield Units directly to Unity using `[DllImport]`."

> **Phase 77: Texture3D VRAM Allocation**
> "Write `DicomVolumeLoader` in C#. Receive the array pointer from Phase 76. Construct a `float[,,] volumeData`. Map this array into a Unity `Texture3D` format (RHalf), applying `Texture.Apply()` so the 3D texture resides in VRAM for the raymarching shader."

> **Phase 78: Python TCP DICOM Segmentation**
> "Write `AISegmentationClient`. Open `TcpClient` to local Python backend running 3D U-Net. Serialize DICOM `Texture3D` byte array, send over socket. Read returned binary stream containing byte mask (0 tissue, 1 bone), apply mask to secondary `Texture3D` for colored rendering."

> **Phase 79: Marching Cubes Isosurface Extraction**
> "Volumetric raymarching doesn't allow physical interaction. Write a Compute Shader implementing the Marching Cubes algorithm. Feed it the `Texture3D` density field and an `IsoValue` threshold. Generate a true polygonal mesh `Mesh` object at runtime representing the bone structure from the MRI."

> **Phase 80: Spline-Based Surgical Pre-Planning**
> "Write `SurgicalTrajectoryPlanner`. Allow surgeons to click points on the generated Marching Cubes mesh to define a drilling trajectory for pedicle screws. Generate a Unity Spline between these points. Calculate the distance and angle relative to the world-axis and display them on a UI panel."

> **Phase 81: DICOM Patient Anonymization**
> "Write `DicomAnonymizer`. Before loading a `.dcm` file, parse the header tags. Find tags `(0010,0010)` Patient Name, `(0010,0020)` Patient ID, and `(0010,0030)` DOB. Scrub these values and overwrite them with empty strings to guarantee HIPAA compliance if the file is ever cached."

> **Phase 82: 4D Ultrasound (Time-Series DICOM)**
> "Write `4DVolumeController`. Handle time-series echocardiogram data. Load an array of `Texture3D` objects. Write an `Update()` loop that swaps the active `Texture3D` being fed to the Raymarching shader based on `Time.time`, creating a beating, volumetric representation of the patient's heart."

> **Phase 83: WebRTC Browser Streaming (Heavy Rendering)**
> "Write `WebRTCStreamingManager`. Mobile browsers cannot render Raymarched DICOMs. Deploy the Unity app on a cloud GPU server (AWS EC2). Use the Unity WebRTC package to stream the rendered video frames to a lightweight HTML5 client, transmitting mouse/touch input back to the server."

> **Phase 84: Quadric Error Metrics Decimation**
> "Write `AutoLODManager` Editor Window. Hook into Unity Mesh API. Write QEM decimation algorithm. Contract shortest edges sequentially, evaluating error matrix to preserve silhouette/UV boundaries. Auto-generate LOD1 (50%) and LOD2 (10%) and append to `LODGroup`."

> **Phase 85: Procedural Normal Map Generation**
> "Write `NormalMapGenerator`. When aggressively decimating meshes, the geometry looks flat. Write a Compute Shader that bakes the high-poly vertex normals into a tangent-space Normal Map texture, applying it to the low-poly LOD3 mesh to fake high-density details."

### Module VIII: Optimization, Security, & CI/CD (86-100)
> **Phase 86: AES-256 Encryption for Telemetry**
> "Write `SecurityManager`. Initialize `Aes aes = Aes.Create()`. Generate static key from hardware identifier. Write `Encrypt(string plainText)` that generates new IV, performs crypto transform, returns concatenated `[IV] + [Ciphertext]` to prevent block analysis attacks."

> **Phase 87: Binary State Serialization (Save Files)**
> "Write `SessionSaveManager`. Create serializable struct `SessionData` (Camera Pos/Rot, Slice Depth, Hidden Systems). Use `BinaryFormatter` to serialize data into `MemoryStream`. Convert bytes to Base64 string, save to `Application.persistentDataPath + "/session.dat"`."

> **Phase 88: Addressable Leak Detector (Editor Tool)**
> "Write `AssetLeakDetector` (`#if UNITY_EDITOR`). Hook into `playModeStateChanged`. When navigating Macro to Micro, compare loaded asset counts via `Addressables.ResourceLocators`. If handle released but Profiler Mesh count doesn't drop, log `Debug.LogError` highlighting VRAM leak."

> **Phase 89: Automated Raycast Integration Tests**
> "Write `RaycastIntegrationTests`. Spawn primitive Cube, Layer = 8, dummy `AnatomyData` ID = 'TEST_01'. Move camera to `(0,0,-5)`. Programmatically call `AnatomyRaycaster.ExecuteClick()`. Assert resulting event string == 'TEST_01'."

> **Phase 90: Automated Serialization Tests**
> "Write `SerializationTests`. Instantiate `SessionData` struct with randomized mock data (e.g., `cameraPos = Vector3(12, 3, 5)`). Call `SessionSaveManager.Save()`. Alter struct to zero. Call `Load()`. `Assert.AreEqual` loaded `cameraPos` matches original, proving AES/binary formatting works."

> **Phase 91: Security Obfuscation Pre-Build Hook**
> "Write Editor script `SecurityObfuscator`. Integrate Dotfuscator CLI. Execute command-line argument renaming all public methods, classes, and namespaces in `Assembly-CSharp.dll` to unreadable hash strings before packaging, preventing reverse engineering."

> **Phase 92: WebGL Input Touch Overrides**
> "Write `TouchGestureManager` compiled strictly for `#if UNITY_WEBGL`. Map browser `TouchEvent` APIs to Unity's Event System to prevent scrolling the actual HTML webpage while panning the 3D model inside the Canvas wrapper."

> **Phase 93: Dynamic Resolution Scaling**
> "Write `DynamicResolutionManager`. Monitor average FPS over 60-frame window. If FPS < 45 on underpowered hardware, call `UniversalRenderPipeline.asset.renderScale = 0.7f` to drop internal resolution rendering, prioritizing framerate over pixel density."

> **Phase 94: Shader Variant Stripping**
> "Write `ShaderVariantStripper` Editor script. Hook `IPreprocessShaders`. Check keywords. Strip any shader variants compiling for Forward Add, Point Lights, or Spot Lights, as the app strictly utilizes single Directional lighting and Image Based Lighting (IBL), reducing build times by 80%."

> **Phase 95: Device Battery Drain Mitigation**
> "Write `BatteryOptimizer`. On iOS/Android/Quest, if screen is completely static for 5 seconds (no input), dynamically drop `Application.targetFrameRate` to 30. Immediately restore to 60/90/120 upon any touch or head movement to save battery life."

> **Phase 96: Screen Space Ambient Occlusion (SSAO) Tuning**
> "Write `SSAOController`. Tie to URP Renderer Feature. For anatomical crevices (like the sulci of the brain), map the `Radius` and `Intensity` of the SSAO pass to the camera's zoom distance. Make shadows softer up close, and sharper from far away."

> **Phase 97: Asynchronous Scene Loading**
> "Write `SceneTransitionManager`. Never use `SceneManager.LoadScene()`. Always use `LoadSceneAsync()`. Display a UI loading bar mapped to `AsyncOperation.progress`. Yield until progress reaches 0.9f, fade screen to black, and then set `allowSceneActivation = true`."

> **Phase 98: Memory Defragmentation Utility**
> "Write `MemoryDefragger`. In mobile/WebGL, memory fragmentation causes crashes even if total RAM is sufficient. Write a method called during major scene transitions that calls `System.GC.Collect()` followed by `Resources.UnloadUnusedAssets()` sequentially to force the OS to contiguous memory blocks."

> **Phase 99: Crash Analytics SDK Hook**
> "Write `CrashReporter`. Integrate Unity Crash and Exception Reporting or Sentry SDK. Hook into `Application.logMessageReceived`. If `type == LogType.Exception`, serialize the current `GameState`, camera coordinates, and loaded memory blocks, appending them as breadcrumbs to the crash payload."

> **Phase 100: Final Deployment CI/CD (GitHub Actions)**
> "Write a GitHub Actions `.yml` workflow. Trigger on push to `main`. Spin up Ubuntu runner. Use `game-ci/unity-builder` to build WebGL, Android APK, and Windows Standalone. Run all NUnit tests. On success, zip artifacts, sign binaries, and upload to GitHub Releases automatically."
