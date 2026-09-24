using System.Collections.Generic;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Makes the structure you are pointing at, and the one you have selected, unmistakable.
    ///
    /// Pointing at a part tints it and shows its name beside the cursor. Clicking selects it and
    /// isolates it: it glows in a pulsing accent colour while every other structure turns into a
    /// faint, see-through ghost, so a nerve buried under muscle and bone is visible and a single
    /// vessel can be followed through a dense mesh of them. Ghosts cannot be clicked - the click
    /// goes to the selected part - so clicking empty space (or pressing Escape) is how you let go.
    /// H switches the ghosting off, leaving only the glow.
    ///
    /// The tint is a per-material property block on <c>_BaseColor</c>, which every shader in the
    /// project shares, and ghosting swaps in the skin shell's translucent shader; nothing is ever
    /// written to a material asset, and clearing puts every renderer and collider back exactly as
    /// the layer toggles have them. Translucent parts - the skin and pericardium - are left alone.
    ///
    /// Hover and the name tooltip are switched off while a quiz question is open, since the tooltip
    /// would give the answer away.
    /// </summary>
    public class AnatomyHighlighter : MonoBehaviour
    {
        [SerializeField] private ExplorerUIController explorerUI;
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private AnatomyLayerVisibility layerVisibility;
        [SerializeField] private Color accentColor = new Color(0.15f, 0.85f, 1f);
        [SerializeField, Range(0f, 1f)] private float hoverTint = 0.38f;
        [SerializeField, Range(0f, 1f)] private float selectedTint = 0.85f;
        [SerializeField] private Color ghostColor = new Color(0.55f, 0.66f, 0.82f);
        [SerializeField, Range(0f, 1f)] private float ghostColorMix = 0.55f;
        [SerializeField] private bool isolate = true;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private enum Look { Normal, Hover, Selected, Ghost }

        private sealed class Part
        {
            public string Id;
            public Renderer Renderer;
            public Collider Collider;
            public Material[] Original;
            public bool Ghosted;
            public Color[] Base;        // per material; alpha < 0.9 marks a translucent (untouched) material
            public Look[] Applied;
        }

        private readonly List<Part> _parts = new List<Part>();
        private readonly Dictionary<string, List<Part>> _partsById = new Dictionary<string, List<Part>>();
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private MaterialPropertyBlock _block;
        private Material _ghost;
        private readonly Dictionary<int, Material[]> _ghostArrays = new Dictionary<int, Material[]>();

        private string _hoverId;
        private string _selectedId;
        private GUIStyle _nameStyle, _latinStyle;
        private Texture2D _panel;
        private string _hoverName, _hoverLatin;

        public string SelectedId => _selectedId;

        private void OnEnable()
        {
            AnatomyRaycaster.OnNodeSelected += Select;
            AnatomyRaycaster.OnNothingSelected += Clear;
            if (layerVisibility != null) layerVisibility.OnVisibilityChanged += Clear;
        }

        private void OnDisable()
        {
            AnatomyRaycaster.OnNodeSelected -= Select;
            AnatomyRaycaster.OnNothingSelected -= Clear;
            if (layerVisibility != null) layerVisibility.OnVisibilityChanged -= Clear;
            RestoreAll();
        }

        private System.Collections.IEnumerator Start()
        {
            // The figure is built at edit time, so it is all there after one frame.
            yield return null;
            if (sourceCamera == null) sourceCamera = Camera.main;
            CacheParts();
        }

        private void CacheParts()
        {
            _parts.Clear();
            _partsById.Clear();
            foreach (var node in FindObjectsByType<AnatomyNodeReference>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var renderer = node.GetComponent<Renderer>();
                if (renderer == null || string.IsNullOrEmpty(node.EntityId)) continue;

                var materials = renderer.sharedMaterials;
                var bases = new Color[materials.Length];
                bool anyOpaque = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    bases[i] = materials[i] != null && materials[i].HasProperty(BaseColorId) ? materials[i].GetColor(BaseColorId) : new Color(0, 0, 0, 0);
                    anyOpaque |= bases[i].a >= 0.9f;
                }
                if (!anyOpaque) continue;   // translucent throughout (skin, pericardium): leave it

                var part = new Part
                {
                    Id = node.EntityId, Renderer = renderer, Collider = node.GetComponent<Collider>(),
                    Original = materials, Base = bases, Applied = new Look[materials.Length],
                };
                _parts.Add(part);
                if (!_partsById.TryGetValue(node.EntityId, out var list)) _partsById[node.EntityId] = list = new List<Part>();
                list.Add(part);
            }
        }

        private void Select(string id)
        {
            if (!Active) return;
            // Selecting the skin (or anything else with nothing opaque to light up) would just ghost
            // the whole body for no reason.
            if (!_partsById.ContainsKey(id)) { Clear(); return; }
            _selectedId = id;
            Refresh();
        }

        private void Clear()
        {
            if (_selectedId == null) return;
            _selectedId = null;
            Refresh();
        }

        private bool Active => explorerUI == null || explorerUI.QuizController == null || !explorerUI.QuizController.IsQuestionActive;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) Clear();
                if (keyboard.hKey.wasPressedThisFrame) { isolate = !isolate; Refresh(); }
            }

            if (!Active)
            {
                if (_hoverId != null) { _hoverId = null; Refresh(); }
                if (_selectedId != null) Clear();
                return;
            }

            string hovered = FindHovered();
            if (hovered != _hoverId)
            {
                _hoverId = hovered;
                _hoverName = _hoverLatin = null;
                if (hovered != null && GameManager.Instance != null && GameManager.Instance.DataController != null)
                {
                    try
                    {
                        var node = GameManager.Instance.DataController.GetNode(hovered);
                        _hoverName = node.CommonName;
                        _hoverLatin = node.LatinName;
                    }
                    catch (System.Exception) { /* an id with no dictionary entry just gets no tooltip */ }
                }
                Refresh();
            }

            if (_selectedId != null) Pulse();
        }

        private string FindHovered()
        {
            if (sourceCamera == null || Mouse.current == null) return null;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;

            Ray ray = sourceCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            int count = Physics.RaycastNonAlloc(ray, _hits, 100f);
            string best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.distance >= bestDistance) continue;
                var node = hit.collider.GetComponentInParent<AnatomyNodeReference>();
                if (node == null || string.IsNullOrEmpty(node.EntityId)) continue;
                var renderer = hit.collider.GetComponentInParent<Renderer>();
                if (renderer == null || !renderer.enabled) continue;
                // Only opaque parts can be hovered - the translucent skin covers everything and
                // would otherwise be all you ever pointed at.
                if (!_partsById.ContainsKey(node.EntityId)) continue;

                best = node.EntityId;
                bestDistance = hit.distance;
            }
            return best;
        }

        // ------------------------------------------------------------------ applying the look

        private void Refresh()
        {
            foreach (var part in _parts)
            {
                Look wanted;
                if (_selectedId != null)
                    wanted = part.Id == _selectedId ? Look.Selected : part.Id == _hoverId ? Look.Hover : (isolate ? Look.Ghost : Look.Normal);
                else
                    wanted = part.Id == _hoverId ? Look.Hover : Look.Normal;

                SetGhosted(part, wanted == Look.Ghost);
                for (int m = 0; m < part.Applied.Length; m++)
                {
                    if (wanted == part.Applied[m] && wanted != Look.Selected) continue;
                    ApplyColor(part, m, wanted, 1f);
                    part.Applied[m] = wanted;
                }
            }
        }

        private void Pulse()
        {
            if (!_partsById.TryGetValue(_selectedId, out var parts)) return;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
            float strength = Mathf.Lerp(0.72f, 1f, wave);
            foreach (var part in parts)
                for (int m = 0; m < part.Base.Length; m++) ApplyColor(part, m, Look.Selected, strength);
        }

        private void ApplyColor(Part part, int material, Look look, float strength)
        {
            if (part.Renderer == null) return;
            Color baseColor = part.Base[material];
            if (baseColor.a < 0.9f) return;   // a translucent material on an otherwise opaque part

            if (look == Look.Normal)
            {
                part.Renderer.SetPropertyBlock(null, material);
                return;
            }

            Color color;
            switch (look)
            {
                case Look.Hover: color = Color.Lerp(baseColor, accentColor, hoverTint); break;
                case Look.Selected: color = Color.Lerp(baseColor, accentColor * 1.25f, selectedTint * strength); break;   // a little over 1: it should glow
                default: color = Color.Lerp(baseColor, ghostColor, ghostColorMix); break;
            }
            color.a = 1f;   // the ghost shader takes its opacity from its own settings

            _block ??= new MaterialPropertyBlock();
            part.Renderer.GetPropertyBlock(_block, material);
            _block.SetColor(BaseColorId, color);
            part.Renderer.SetPropertyBlock(_block, material);
        }

        /// <summary>Swap a part between its own materials and the translucent ghost, and take it out
        /// of (or back into) the set of things a click can hit.</summary>
        private void SetGhosted(Part part, bool ghosted)
        {
            if (part.Renderer == null || part.Ghosted == ghosted) return;
            var ghost = GhostMaterial();
            if (ghosted && ghost == null) return;   // no ghost shader available: the part just keeps its colour

            part.Ghosted = ghosted;
            part.Renderer.sharedMaterials = ghosted ? GhostArray(part.Original.Length) : part.Original;
            // Layer toggles keep a part's collider in step with its renderer, so that is the state to restore.
            if (part.Collider != null) part.Collider.enabled = !ghosted && part.Renderer.enabled;
        }

        private Material GhostMaterial()
        {
            if (_ghost != null) return _ghost;
            var shader = Shader.Find("HumanBodyExplorer/SkinShell");
            if (shader == null) return null;
            _ghost = new Material(shader) { name = "HighlightGhost", hideFlags = HideFlags.HideAndDontSave };
            _ghost.SetFloat("_CenterAlpha", 0.035f);
            _ghost.SetFloat("_EdgeAlpha", 0.42f);
            _ghost.SetFloat("_FresnelPower", 2.0f);
            _ghost.SetFloat("_Ambient", 0.85f);
            return _ghost;
        }

        private Material[] GhostArray(int length)
        {
            if (!_ghostArrays.TryGetValue(length, out var array))
            {
                array = new Material[length];
                for (int i = 0; i < length; i++) array[i] = _ghost;
                _ghostArrays[length] = array;
            }
            return array;
        }

        private void RestoreAll()
        {
            foreach (var part in _parts)
            {
                if (part.Renderer == null) continue;
                SetGhosted(part, false);
                for (int m = 0; m < part.Base.Length; m++) part.Renderer.SetPropertyBlock(null, m);
                for (int m = 0; m < part.Applied.Length; m++) part.Applied[m] = Look.Normal;
            }
        }

        // ------------------------------------------------------------------ tooltip

        private void OnGUI()
        {
            if (!Active || string.IsNullOrEmpty(_hoverName) || Mouse.current == null) return;

            if (_nameStyle == null)
            {
                _nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
                _nameStyle.normal.textColor = Color.white;
                _latinStyle = new GUIStyle(_nameStyle) { fontSize = 12, fontStyle = FontStyle.Italic };
                _latinStyle.normal.textColor = new Color(0.72f, 0.86f, 0.95f);
                _panel = Texture2D.whiteTexture;
            }

            Vector2 mouse = Mouse.current.position.ReadValue();
            float x = mouse.x + 18f, y = Screen.height - mouse.y + 14f;
            bool hasLatin = !string.IsNullOrEmpty(_hoverLatin) && _hoverLatin != _hoverName;
            float width = Mathf.Max(_nameStyle.CalcSize(new GUIContent(_hoverName)).x,
                                    hasLatin ? _latinStyle.CalcSize(new GUIContent(_hoverLatin)).x : 0f) + 20f;
            float height = hasLatin ? 46f : 28f;
            x = Mathf.Min(x, Screen.width - width - 8f);
            y = Mathf.Min(y, Screen.height - height - 8f);

            var previous = GUI.color;
            GUI.color = new Color(0.05f, 0.07f, 0.10f, 0.92f);
            GUI.DrawTexture(new Rect(x, y, width, height), _panel);
            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, 1f);
            GUI.DrawTexture(new Rect(x, y, 3f, height), _panel);
            GUI.color = previous;

            GUI.Label(new Rect(x + 10f, y + 3f, width, 24f), _hoverName, _nameStyle);
            if (hasLatin) GUI.Label(new Rect(x + 10f, y + 24f, width, 20f), _hoverLatin, _latinStyle);
        }
    }
}
