using System.Collections.Generic;
using UnityEngine;

namespace VertexSnapper.Managers;

public class EditorSelectionManager : MonoBehaviour
{
    public List<BlockProperties> BlockPropertiesList { get; private set; }

    private void Start()
    {
        BlockPropertiesList = FindObjectOfType<LEV_LevelEditorCentral>().selection.list;
    }

    private void Update()
    {
    }
}