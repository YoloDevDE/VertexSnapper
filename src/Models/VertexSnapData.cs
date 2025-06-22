using System.Collections.Generic;
using UnityEngine;

public class VertexSnapData
{
    // Core components
    public Camera Camera { get; set; }
    public LEV_LevelEditorCentral Central { get; set; }

    // Current state
    public List<BlockProperties> SelectedItems { get; } = new List<BlockProperties>();
    public BlockProperties CurrentTarget { get; set; }
    public Transform Cursor { get; set; }
    public bool IsDragging { get; set; }
    public bool IsInEditor { get; set; }

    // Snapping mode stored data
    public BlockProperties StoredPrimaryTarget { get; set; }
    public Vector3 StoredVertexPosition { get; set; }
    public Vector3 StoredVertOffset { get; set; }
    public List<BlockProperties> StoredSelectedItems { get; } = new List<BlockProperties>();
    public List<Vector3> StoredRelativePositions { get; } = new List<Vector3>();

    // Rendering and visual data
    public List<GameObject> Holograms { get; } = new List<GameObject>();
    public HashSet<Renderer> HiddenRenderers { get; } = new HashSet<Renderer>();
    public Dictionary<Renderer, Material[]> OriginalMaterials { get; } = new Dictionary<Renderer, Material[]>();
    public List<Renderer> OriginRenderers { get; } = new List<Renderer>();

    // Timing and calculations
    public float LastVisibilityUpdateTime { get; set; }
    public float PulseTime { get; set; }
    public Vector3 VertOffset { get; set; }
    public MeshFilter[] MeshFilters { get; set; }

    // Input tracking
    public bool WasKeyDownLastFrame { get; set; }

    // Undo/Redo data
    public List<string> BeforeData { get; } = new List<string>();
    public List<string> BeforeSelection { get; } = new List<string>();
    public List<string> AfterData { get; } = new List<string>();
    public List<string> AfterSelection { get; } = new List<string>();
}